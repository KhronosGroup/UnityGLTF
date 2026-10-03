using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>One entry of the spec's "variable interpolation state dynamic array".</summary>
    public struct VariableInterpolateData
    {
        public int variableIndex;
        public double startTime;
        public double duration;
        public Variant endValue;
        public float2 cp1;
        public float2 cp2;
        /// <summary>Invoked when the interpolation completes. Nodes create this delegate once, not per activation.</summary>
        public Action done;
        public bool slerp;
        internal Variant from;
        internal int id;
    }

    public class VariableInterpolationManager
    {
        private readonly BehaviourEngine _engine;

        // Insertion-ordered entries; a variable has at most one. Ids increase with insertion,
        // so the entries are also sorted by id. An array rather than a List so entries are updated in place.
        private VariableInterpolateData[] _active = new VariableInterpolateData[8];
        private int _count;
        private readonly List<int> _snapshot = new();
        private int _nextId;

        public int activeInterpolationCount => _count;

        public VariableInterpolationManager(BehaviourEngine engine)
        {
            _engine = engine;
        }

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
                _engine.SetVariable(data.variableIndex, data.endValue);
                var done = data.done;
                RemoveAt(index);
                done?.Invoke();
                return;
            }

            _engine.SetVariable(data.variableIndex, Lerp(data.from, data.endValue, Helpers.Ease((float)t, data.cp1, data.cp2), data.slerp));
        }

        /// <summary>
        /// Adds an entry for the variable, replacing any existing one.
        /// Throws <see cref="InterpolatorException"/> when the target type does not match the variable or cannot be interpolated.
        /// </summary>
        public void StartInterpolation(ref VariableInterpolateData data)
        {
            var current = _engine.GetVariable(data.variableIndex);

            if (!current.isFloatBased)
                throw new InterpolatorException($"Interpolation has not been defined for type {current.GetTypeSignature()}!");

            if (data.endValue.type != current.type)
                throw new InterpolatorException($"Interpolation target type {data.endValue.GetTypeSignature()} does not match variable type {current.GetTypeSignature()}.");

            data.from = current;
            data.id = _nextId++;

            StopInterpolation(data.variableIndex);

            if (_count == _active.Length)
                Array.Resize(ref _active, _count * 2);

            _active[_count++] = data;

            Util.Log($"Starting Variable Interpolation: Start Time {data.startTime}, Duration: {data.duration}");
        }

        public bool StopInterpolation(int variableIndex)
        {
            return RemoveAt(IndexOf(variableIndex));
        }

        private int IndexOf(int variableIndex)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_active[i].variableIndex == variableIndex)
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

            // Release the delegate held by the vacated slot.
            _active[_count] = default;
            return true;
        }

        /// <summary>Component-wise linear interpolation of any float-based value; float4 uses slerp when requested.</summary>
        private static Variant Lerp(in Variant a, in Variant b, float t, bool slerp)
        {
            if (slerp && a.type == VariantType.Float4)
                return Variant.FromFloat4(Helpers.Slerpfloat4(a.Float4, b.Float4, t));

            var ca = a.columns;
            var cb = b.columns;

            return Variant.FromColumns(a.type, new float4x4(
                math.lerp(ca.c0, cb.c0, t),
                math.lerp(ca.c1, cb.c1, t),
                math.lerp(ca.c2, cb.c2, t),
                math.lerp(ca.c3, cb.c3, t)));
        }
    }
}
