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

        // Insertion-ordered entries; a variable has at most one.
        private readonly List<VariableInterpolateData> _active = new();
        private readonly List<VariableInterpolateData> _snapshot = new();
        private int _nextId;

        public int activeInterpolationCount => _active.Count;

        public VariableInterpolationManager(BehaviourEngine engine)
        {
            _engine = engine;
        }

        public void OnTick(double now)
        {
            // Snapshot first: done flows may start or stop other interpolations.
            _snapshot.Clear();
            _snapshot.AddRange(_active);

            for (int i = 0; i < _snapshot.Count; i++)
            {
                var index = IndexOf(_snapshot[i].variableIndex);

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
                _engine.SetVariable(data.variableIndex, data.endValue);
                _active.RemoveAt(index);
                data.done?.Invoke();
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
            _active.Add(data);

            Util.Log($"Starting Variable Interpolation: Start Time {data.startTime}, Duration: {data.duration}");
        }

        public bool StopInterpolation(int variableIndex)
        {
            var index = IndexOf(variableIndex);

            if (index < 0)
                return false;

            _active.RemoveAt(index);
            return true;
        }

        private int IndexOf(int variableIndex)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].variableIndex == variableIndex)
                    return i;
            }

            return -1;
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
