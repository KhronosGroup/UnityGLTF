using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
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
        public Variant endValue;
        public float2 cp1;
        public float2 cp2;
        /// <summary>Invoked when the interpolation completes. Nodes create this delegate once, not per activation.</summary>
        public Action done;
        internal Variant from;
        internal int id;
    }

    public class InterpolatorException : Exception
    {
        public InterpolatorException(){}
        public InterpolatorException(string message) : base(message){}
        public InterpolatorException(string message, Exception innerException) : base(message, innerException) {}
    }

    public class PointerInterpolationManager
    {
        // Insertion-ordered entries; a pointer has at most one.
        private readonly List<PointerInterpolateData> _active = new();
        private readonly List<PointerInterpolateData> _snapshot = new();
        private int _nextId;

        public int activeInterpolationCount => _active.Count;

        public void OnTick(double now)
        {
            // Snapshot first: done flows may start or stop other interpolations.
            _snapshot.Clear();
            _snapshot.AddRange(_active);

            for (int i = 0; i < _snapshot.Count; i++)
            {
                var index = IndexOf(_snapshot[i].pointerKey);

                if (index < 0 || _active[index].id != _snapshot[i].id)
                    continue;

                DoInterpolate(index, now);
            }

            _snapshot.Clear();
        }

        private void DoInterpolate(int index, double now)
        {
            var data = _active[index];
            var t = (now - data.startTime) / data.duration;

            if (t <= 0)
                return;

            if (double.IsNaN(t) || t >= 1)
            {
                PointerHelpers.TryWrite(data.pointer, data.endValue);
                _active.RemoveAt(index);
                data.done?.Invoke();
                return;
            }

            Apply(data.pointer, data.from, data.endValue, Helpers.Ease((float)t, data.cp1, data.cp2));
        }

        /// <summary>
        /// Adds an entry for the pointer, replacing any entry with the same effective pointer.
        /// Throws <see cref="InterpolatorException"/> when the value type does not match the pointer.
        /// </summary>
        public void StartInterpolation(ref PointerInterpolateData data)
        {
            if (!CanInterpolate(data.pointer, data.endValue.type))
                throw new InterpolatorException($"Type {data.endValue.GetTypeSignature()} cannot be interpolated on a pointer of type {data.pointer.GetSystemType()}.");

            data.from = PointerHelpers.Read(data.pointer);
            data.id = _nextId++;

            StopInterpolation(data.pointerKey);
            _active.Add(data);

            Util.Log($"Starting Interpolation: Start Time {data.startTime}, Duration: {data.duration}");
        }

        public bool StopInterpolation(string pointerKey)
        {
            var index = IndexOf(pointerKey);

            if (index < 0)
                return false;

            _active.RemoveAt(index);
            return true;
        }

        private int IndexOf(string pointerKey)
        {
            if (pointerKey == null)
                return -1;

            for (int i = 0; i < _active.Count; i++)
            {
                if (string.Equals(_active[i].pointerKey, pointerKey, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private static bool CanInterpolate(IPointer pointer, VariantType type)
        {
            return type switch
            {
                VariantType.Float => pointer is Pointer<float> p && p.evaluator != null,
                VariantType.Float2 => pointer is Pointer<float2> p && p.evaluator != null,
                VariantType.Float3 => (pointer is Pointer<float3> a && a.evaluator != null) || (pointer is Pointer<Color3> b && b.evaluator != null),
                VariantType.Float4 => (pointer is Pointer<float4> a && a.evaluator != null) || (pointer is Pointer<Color> b && b.evaluator != null) || (pointer is Pointer<quaternion> c && c.evaluator != null),
                VariantType.Float2x2 => pointer is Pointer<float2x2> p && p.evaluator != null,
                VariantType.Float3x3 => pointer is Pointer<float3x3> p && p.evaluator != null,
                VariantType.Float4x4 => pointer is Pointer<float4x4> p && p.evaluator != null,
                _ => false,
            };
        }

        /// <summary>Writes the pointer's own evaluator result (e.g. slerp for rotations) at eased progress <paramref name="q"/>.</summary>
        private static void Apply(IPointer pointer, in Variant from, in Variant to, float q)
        {
            switch (pointer)
            {
                case Pointer<float> p: p.setter(p.evaluator(from.Float, to.Float, q)); break;
                case Pointer<float2> p: p.setter(p.evaluator(from.Float2, to.Float2, q)); break;
                case Pointer<float3> p: p.setter(p.evaluator(from.Float3, to.Float3, q)); break;
                case Pointer<Color3> p: p.setter(p.evaluator(from.Float3.ToColor(), to.Float3.ToColor(), q)); break;
                case Pointer<float4> p: p.setter(p.evaluator(from.Float4, to.Float4, q)); break;
                case Pointer<Color> p: p.setter(p.evaluator(from.Float4.ToColor(), to.Float4.ToColor(), q)); break;
                case Pointer<quaternion> p: p.setter(p.evaluator(from.Float4.ToQuaternion(), to.Float4.ToQuaternion(), q)); break;
                case Pointer<float2x2> p: p.setter(p.evaluator(from.Float2x2, to.Float2x2, q)); break;
                case Pointer<float3x3> p: p.setter(p.evaluator(from.Float3x3, to.Float3x3, q)); break;
                case Pointer<float4x4> p: p.setter(p.evaluator(from.Float4x4, to.Float4x4, q)); break;
            }
        }
    }
}
