using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public struct PointerInterpolateData
    {
        public IPointer pointer;
        public float startTime;
        public float duration;
        public IProperty endValue;
        public float2 cp1;
        public float2 cp2;
        public Action done;
        public IInterpolator interpolator;
    }

    public interface IInterpolator 
    {
        public bool Interpolate(float t);
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

            public bool Interpolate(float t)
            {
                var end = t >= 1f;

                t = end ? 1f : t;

                setter(evaluator(from, to, t));

                return end;
            }
        }

        private Dictionary<IPointer, PointerInterpolateData> _interpolationsInProgress = new();

        public void OnTick()
        {
            // Avoiding iterating over a changing collection by grabbing a pooled dictionary.
            var temp = DictionaryPool<IPointer, PointerInterpolateData>.Get();
            try
            {
                foreach (var interp in _interpolationsInProgress)
                {
                    temp.Add(interp.Key, interp.Value);
                }

                foreach (var anim in temp)
                {
                    DoInterpolate(anim.Value);
                }
            }
            finally
            {
                DictionaryPool<IPointer, PointerInterpolateData>.Release(temp);
            }
        }

        private void DoInterpolate(PointerInterpolateData data)
        {
            var t = (Time.time - data.startTime) / data.duration;

            var finished = data.interpolator.Interpolate(t);

            if (finished)
            {
                Util.Log($"Finished interpolating.");

                _interpolationsInProgress.Remove(data.pointer);
                data.done();
            }
        }

        public void StartInterpolation(ref PointerInterpolateData data)
        {
            _interpolationsInProgress.Remove(data.pointer); // Stop any in-progress interpolations for this pointer.

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

            _interpolationsInProgress.Add(data.pointer, data);

            Util.Log($"Starting Interpolation: Start Time {data.startTime}, Duration: {data.duration}");
        }

        public bool StopInterpolation(IPointer pointer)
        {
            return _interpolationsInProgress.Remove(pointer); // Stop any in-progress interpolations for this pointer.
        }

        private IInterpolator Processfloat3(Property<float3> property, PointerInterpolateData data)
        {
            return data.pointer switch
            {
                Pointer<float3> => GetInterpolator(property, data),
                Pointer<Color> => GetInterpolator(new Property<Color>(property.value.ToColor()), data),
                Pointer<Color3> => GetInterpolator(new Property<Color3>(property.value.ToColor()), data),
                Pointer<quaternion> => GetInterpolator(new Property<quaternion>(quaternion.Euler(property.value)), data),

                _ => throw new InterpolatorException($"Pointer type {data.pointer.GetSystemType()} is not supported for this float3 property."),
            };
        }

        private IInterpolator Processfloat4(Property<float4> property, PointerInterpolateData data)
        {
            return data.pointer switch
            {
                Pointer<float4> => GetInterpolator(property, data),
                Pointer<Color> => GetInterpolator(new Property<Color>(property.value.ToColor()), data),
                Pointer<quaternion> => GetInterpolator(new Property<quaternion>(property.value.ToQuaternion()), data),

                _ => throw new InterpolatorException($"Pointer type {data.pointer.GetSystemType()} is not supported for this float4 property."),
            };
        }

        private IInterpolator GetInterpolator<T>(Property<T> property, in PointerInterpolateData data)
        {
            var p = (Pointer<T>)data.pointer;
            var cp1 = data.cp1;
            var cp2 = data.cp2;
            var evaluator = new Func<T, T, float, T>((a, b, t) => p.evaluator(a, b, Helpers.CubicBezier(t, cp1, cp2).y));

            var interpolator = new Interpolator<T>()
            {
                setter = p.setter,
                evaluator = evaluator,
                from = p.getter(),
                to = property.value
            };

            return interpolator;
        }
    }
}
