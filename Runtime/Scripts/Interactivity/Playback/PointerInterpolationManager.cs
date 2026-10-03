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
        /// <summary><see cref="pointerKey"/> as an id from <see cref="PointerInterpolationManager"/>, or -1 for a null key.</summary>
        internal int keyId;
        internal PointerInterpolationManager.PointerKind kind;
    }

    public class InterpolatorException : Exception
    {
        public InterpolatorException(){}
        public InterpolatorException(string message) : base(message){}
        public InterpolatorException(string message, Exception innerException) : base(message, innerException) {}
    }

    public class PointerInterpolationManager
    {
        /// <summary>The concrete pointer type of an entry, found once when it starts so ticks need no type tests.</summary>
        internal enum PointerKind : byte
        {
            None,
            Float,
            Float2,
            Float3,
            Color3,
            Float4,
            Color,
            Quaternion,
            Float2x2,
            Float3x3,
            Float4x4,
        }

        // Insertion-ordered entries; a pointer has at most one. Ids increase with insertion,
        // so the entries are also sorted by id. An array rather than a List so entries are updated in place.
        private PointerInterpolateData[] _active = new PointerInterpolateData[8];
        private int _count;
        private readonly List<int> _snapshot = new();
        private int _nextId;

        // Effective pointer strings mapped to small ids, so entries are matched without string comparisons.
        private readonly Dictionary<string, int> _keyIds = new(StringComparer.Ordinal);

        public int activeInterpolationCount => _count;

        public void OnTick(double now)
        {
            // Snapshot ids first: done flows may start or stop other interpolations.
            _snapshot.Clear();

            for (int i = 0; i < _count; i++)
                _snapshot.Add(_active[i].id);

            for (int i = 0; i < _snapshot.Count; i++)
            {
                var index = IndexOfId(_snapshot[i]);

                if (index >= 0)
                    DoInterpolate(index, now);
            }

            _snapshot.Clear();
        }

        private void DoInterpolate(int index, double now)
        {
            ref var data = ref _active[index];
            var t = (now - data.startTime) / data.duration;

            if (t <= 0)
                return;

            if (double.IsNaN(t) || t >= 1)
            {
                PointerHelpers.TryWrite(data.pointer, data.endValue);
                var done = data.done;
                RemoveAt(index);
                done?.Invoke();
                return;
            }

            Apply(data.kind, data.pointer, data.from, data.endValue, Helpers.Ease((float)t, data.cp1, data.cp2));
        }

        /// <summary>
        /// Adds an entry for the pointer, replacing any entry with the same effective pointer.
        /// Throws <see cref="InterpolatorException"/> when the value type does not match the pointer.
        /// </summary>
        public void StartInterpolation(ref PointerInterpolateData data)
        {
            data.kind = GetKind(data.pointer, data.endValue.type);

            if (data.kind == PointerKind.None)
                throw new InterpolatorException($"Type {data.endValue.GetTypeSignature()} cannot be interpolated on a pointer of type {data.pointer.GetSystemType()}.");

            data.from = PointerHelpers.Read(data.pointer);
            data.id = _nextId++;
            data.keyId = GetKeyId(data.pointerKey);

            RemoveAt(IndexOfKey(data.keyId));

            if (_count == _active.Length)
                Array.Resize(ref _active, _count * 2);

            _active[_count++] = data;

            Util.Log($"Starting Interpolation: Start Time {data.startTime}, Duration: {data.duration}");
        }

        public bool StopInterpolation(string pointerKey)
        {
            // pointer/set calls this on every activation; most of the time nothing is interpolating.
            if (_count == 0 || pointerKey == null || !_keyIds.TryGetValue(pointerKey, out var keyId))
                return false;

            return RemoveAt(IndexOfKey(keyId));
        }

        private int GetKeyId(string pointerKey)
        {
            if (pointerKey == null)
                return -1;

            if (!_keyIds.TryGetValue(pointerKey, out var keyId))
            {
                keyId = _keyIds.Count;
                _keyIds.Add(pointerKey, keyId);
            }

            return keyId;
        }

        private int IndexOfKey(int keyId)
        {
            if (keyId < 0)
                return -1;

            for (int i = 0; i < _count; i++)
            {
                if (_active[i].keyId == keyId)
                    return i;
            }

            return -1;
        }

        /// <summary>Binary search: entries stay sorted by id because new ones are appended and removals keep order.</summary>
        private int IndexOfId(int id)
        {
            int lo = 0, hi = _count - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                var midId = _active[mid].id;

                if (midId == id)
                    return mid;

                if (midId < id)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            return -1;
        }

        private bool RemoveAt(int index)
        {
            if (index < 0)
                return false;

            _count--;

            if (index < _count)
                Array.Copy(_active, index + 1, _active, index, _count - index);

            // Release the references held by the vacated slot.
            _active[_count] = default;
            return true;
        }

        private static PointerKind GetKind(IPointer pointer, VariantType type)
        {
            return type switch
            {
                VariantType.Float when pointer is Pointer<float> p && p.evaluator != null => PointerKind.Float,
                VariantType.Float2 when pointer is Pointer<float2> p && p.evaluator != null => PointerKind.Float2,
                VariantType.Float3 when pointer is Pointer<float3> p && p.evaluator != null => PointerKind.Float3,
                VariantType.Float3 when pointer is Pointer<Color3> p && p.evaluator != null => PointerKind.Color3,
                VariantType.Float4 when pointer is Pointer<float4> p && p.evaluator != null => PointerKind.Float4,
                VariantType.Float4 when pointer is Pointer<Color> p && p.evaluator != null => PointerKind.Color,
                VariantType.Float4 when pointer is Pointer<quaternion> p && p.evaluator != null => PointerKind.Quaternion,
                VariantType.Float2x2 when pointer is Pointer<float2x2> p && p.evaluator != null => PointerKind.Float2x2,
                VariantType.Float3x3 when pointer is Pointer<float3x3> p && p.evaluator != null => PointerKind.Float3x3,
                VariantType.Float4x4 when pointer is Pointer<float4x4> p && p.evaluator != null => PointerKind.Float4x4,
                _ => PointerKind.None,
            };
        }

        /// <summary>Writes the pointer's own evaluator result (e.g. slerp for rotations) at eased progress <paramref name="q"/>.</summary>
        private static void Apply(PointerKind kind, IPointer pointer, in Variant from, in Variant to, float q)
        {
            switch (kind)
            {
                case PointerKind.Float: { var p = (Pointer<float>)pointer; p.setter(p.evaluator(from.Float, to.Float, q)); break; }
                case PointerKind.Float2: { var p = (Pointer<float2>)pointer; p.setter(p.evaluator(from.Float2, to.Float2, q)); break; }
                case PointerKind.Float3: { var p = (Pointer<float3>)pointer; p.setter(p.evaluator(from.Float3, to.Float3, q)); break; }
                case PointerKind.Color3: { var p = (Pointer<Color3>)pointer; p.setter(p.evaluator(from.Float3.ToColor(), to.Float3.ToColor(), q)); break; }
                case PointerKind.Float4: { var p = (Pointer<float4>)pointer; p.setter(p.evaluator(from.Float4, to.Float4, q)); break; }
                case PointerKind.Color: { var p = (Pointer<Color>)pointer; p.setter(p.evaluator(from.Float4.ToColor(), to.Float4.ToColor(), q)); break; }
                case PointerKind.Quaternion: { var p = (Pointer<quaternion>)pointer; p.setter(p.evaluator(from.Float4.ToQuaternion(), to.Float4.ToQuaternion(), q)); break; }
                case PointerKind.Float2x2: { var p = (Pointer<float2x2>)pointer; p.setter(p.evaluator(from.Float2x2, to.Float2x2, q)); break; }
                case PointerKind.Float3x3: { var p = (Pointer<float3x3>)pointer; p.setter(p.evaluator(from.Float3x3, to.Float3x3, q)); break; }
                case PointerKind.Float4x4: { var p = (Pointer<float4x4>)pointer; p.setter(p.evaluator(from.Float4x4, to.Float4x4, q)); break; }
            }
        }
    }
}
