using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>One entry of the spec's "pointer interpolation state dynamic array".</summary>
    public struct PointerInterpolateData
    {
        /// <summary>The effective JSON pointer; entries are unique per pointer.</summary>
        public string pointerKey;
        public IPointer pointer;
        public double startTime;
        public double duration;
        public IProperty endValue;
        public float2 cp1;
        public float2 cp2;
        public Action done;
        public IInterpolator interpolator;
        internal int id;
    }

    public interface IInterpolator
    {
        /// <summary>Applies the value at eased progress <paramref name="q"/>, which may be outside [0, 1].</summary>
        public void Interpolate(float q);
        /// <summary>Sets the exact target value.</summary>
        public void Finish();
    }

    public class InterpolatorException : Exception
    {
        public InterpolatorException(){}
        public InterpolatorException(string message) : base(message){}
        public InterpolatorException(string message, Exception innerException) : base(message, innerException) {}
    }

    public class PointerInterpolationManager
    {
        public struct Interpolator<T> : IInterpolator
        {
            public Action<T> setter;
            public Func<T, T, float, T> evaluator;
            public T from;
            public T to;

            public void Interpolate(float q) => setter(evaluator(from, to, q));
            public void Finish() => setter(to);
        }

        private readonly Dictionary<string, PointerInterpolateData> _interpolationsInProgress = new();
        private int _nextId;

        public int activeInterpolationCount => _interpolationsInProgress.Count;

        public void OnTick(double now)
        {
            // Snapshot first: done flows may start or stop other interpolations.
            var temp = ListPool<PointerInterpolateData>.Get();
            try
            {
                foreach (var interp in _interpolationsInProgress)
                {
                    temp.Add(interp.Value);
                }

                foreach (var data in temp)
                {
                    if (!_interpolationsInProgress.TryGetValue(data.pointerKey, out var current) || current.id != data.id)
                        continue;

                    DoInterpolate(current, now);
                }
            }
            finally
            {
                ListPool<PointerInterpolateData>.Release(temp);
            }
        }

        private void DoInterpolate(PointerInterpolateData data, double now)
        {
            var t = (now - data.startTime) / data.duration;

            if (t <= 0)
                return;

            if (double.IsNaN(t) || t >= 1)
            {
                data.interpolator.Finish();
                _interpolationsInProgress.Remove(data.pointerKey);
                data.done?.Invoke();
                return;
            }

            data.interpolator.Interpolate(Helpers.Ease((float)t, data.cp1, data.cp2));
        }

        /// <summary>
        /// Adds an entry for the pointer, replacing any entry with the same effective pointer.
        /// Throws <see cref="InterpolatorException"/> when the value type does not match the pointer.
        /// </summary>
        public void StartInterpolation(ref PointerInterpolateData data)
        {
            var interpolator = data.endValue switch
            {
                Property<float> property => GetInterpolator(property, data),
                Property<float2> property => GetInterpolator(property, data),
                Property<float3> property => Processfloat3(property, data),
                Property<float4> property => Processfloat4(property, data),
                Property<float2x2> property => GetInterpolator(property, data),
                Property<float3x3> property => GetInterpolator(property, data),
                Property<float4x4> property => GetInterpolator(property, data),

                _ => throw new InterpolatorException($"Type {data.endValue.GetTypeSignature()} is not supported for interpolation."),
            };

            data.interpolator = interpolator;
            data.id = _nextId++;

            _interpolationsInProgress.Remove(data.pointerKey);
            _interpolationsInProgress.Add(data.pointerKey, data);

            Util.Log($"Starting Interpolation: Start Time {data.startTime}, Duration: {data.duration}");
        }

        public bool StopInterpolation(string pointerKey)
        {
            return pointerKey != null && _interpolationsInProgress.Remove(pointerKey);
        }

        private IInterpolator Processfloat3(Property<float3> property, PointerInterpolateData data)
        {
            return data.pointer switch
            {
                Pointer<float3> => GetInterpolator(property, data),
                Pointer<Color3> => GetInterpolator(new Property<Color3>(property.value.ToColor()), data),

                _ => throw new InterpolatorException($"Pointer type {data.pointer.GetSystemType()} is not supported for this float3 property."),
            };
        }

        private IInterpolator Processfloat4(Property<float4> property, PointerInterpolateData data)
        {
            return data.pointer switch
            {
                Pointer<float4> => GetInterpolator(property, data),
                Pointer<Color> => GetInterpolator(new Property<Color>(property.value.ToColor()), data),
                // Quaternion pointers use their slerp evaluator, as the spec requires for rotations.
                Pointer<quaternion> => GetInterpolator(new Property<quaternion>(property.value.ToQuaternion()), data),

                _ => throw new InterpolatorException($"Pointer type {data.pointer.GetSystemType()} is not supported for this float4 property."),
            };
        }

        private IInterpolator GetInterpolator<T>(Property<T> property, in PointerInterpolateData data)
        {
            if (data.pointer is not Pointer<T> p || p.evaluator == null)
                throw new InterpolatorException($"Pointer type {data.pointer.GetSystemType()} cannot be interpolated to {typeof(T)}.");

            return new Interpolator<T>()
            {
                setter = p.setter,
                evaluator = p.evaluator,
                from = p.getter(),
                to = property.value
            };
        }
    }
}
