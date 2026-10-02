using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Pool;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>One entry of the spec's "variable interpolation state dynamic array".</summary>
    public struct VariableInterpolateData
    {
        public Variable variable;
        public double startTime;
        public double duration;
        public IProperty endValue;
        public float2 cp1;
        public float2 cp2;
        public Action done;
        public IInterpolator interpolator;
        public bool slerp;
        internal int id;
    }

    public class VariableInterpolationManager
    {
        private struct Interpolator<T> : IInterpolator
        {
            public Variable variable;
            public Func<T, T, float, T> evaluator;
            public T from;
            public T to;

            public void Interpolate(float q) => variable.property = new Property<T>(evaluator(from, to, q));
            public void Finish() => variable.property = new Property<T>(to);
        }

        private readonly Dictionary<Variable, VariableInterpolateData> _interpolationsInProgress = new();
        private int _nextId;

        public int activeInterpolationCount => _interpolationsInProgress.Count;

        public void OnTick(double now)
        {
            // Snapshot first: done flows may start or stop other interpolations.
            var temp = ListPool<VariableInterpolateData>.Get();
            try
            {
                foreach (var interp in _interpolationsInProgress)
                {
                    temp.Add(interp.Value);
                }

                foreach (var data in temp)
                {
                    if (!_interpolationsInProgress.TryGetValue(data.variable, out var current) || current.id != data.id)
                        continue;

                    DoInterpolate(current, now);
                }
            }
            finally
            {
                ListPool<VariableInterpolateData>.Release(temp);
            }
        }

        private void DoInterpolate(VariableInterpolateData data, double now)
        {
            var t = (now - data.startTime) / data.duration;

            if (t <= 0)
                return;

            if (double.IsNaN(t) || t >= 1)
            {
                data.interpolator.Finish();
                _interpolationsInProgress.Remove(data.variable);
                data.done?.Invoke();
                return;
            }

            data.interpolator.Interpolate(Helpers.Ease((float)t, data.cp1, data.cp2));
        }

        public void StartInterpolation(ref VariableInterpolateData data)
        {
            data.interpolator = GetInterpolator(data);
            data.id = _nextId++;

            _interpolationsInProgress.Remove(data.variable);
            _interpolationsInProgress.Add(data.variable, data);

            Util.Log($"Starting Variable Interpolation: Start Time {data.startTime}, Duration: {data.duration}");
        }

        public bool StopInterpolation(Variable variable)
        {
            return _interpolationsInProgress.Remove(variable);
        }

        private static IInterpolator GetInterpolator(in VariableInterpolateData data)
        {
            return data.variable.property switch
            {
                Property<float> => Create<float>(math.lerp, data),
                Property<float2> => Create<float2>(math.lerp, data),
                Property<float3> => Create<float3>(math.lerp, data),
                Property<float4> when data.slerp => Create<float4>(Helpers.Slerpfloat4, data),
                Property<float4> => Create<float4>(math.lerp, data),
                Property<float2x2> => Create<float2x2>(Helpers.LerpComponentwise, data),
                Property<float3x3> => Create<float3x3>(Helpers.LerpComponentwise, data),
                Property<float4x4> => Create<float4x4>(Helpers.LerpComponentwise, data),

                _ => throw new InterpolatorException($"Interpolation has not been defined for type {data.variable.property.GetTypeSignature()}!"),
            };
        }

        private static IInterpolator Create<T>(Func<T, T, float, T> evaluator, in VariableInterpolateData data)
        {
            if (data.endValue is not Property<T> end || data.variable.property is not Property<T> from)
                throw new InterpolatorException($"Interpolation target type does not match variable type {typeof(T)}.");

            return new Interpolator<T>()
            {
                variable = data.variable,
                evaluator = evaluator,
                from = from.value,
                to = end.value
            };
        }
    }
}
