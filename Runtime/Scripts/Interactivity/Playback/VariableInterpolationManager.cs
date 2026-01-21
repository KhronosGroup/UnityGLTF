using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public struct VariableInterpolateData
    {
        public Variable variable;
        public float startTime;
        public float duration;
        public IProperty endValue;
        public float2 cp1;
        public float2 cp2;
        public Action done;
        public IInterpolator interpolator;
        public bool slerp;
    }

    public class VariableInterpolationManager
    {
        private struct Interpolator<T> : IInterpolator
        {
            public Variable variable;
            public Func<T, T, float, Property<T>> evaluator;
            public T from;
            public T to;

            public bool Interpolate(float t)
            {
                var end = t >= 1f;

                t = end ? 1f : t;

                variable.property = evaluator(from, to, t);

                return end;
            }
        }

        private readonly Dictionary<Variable, VariableInterpolateData> _interpolationsInProgress = new();

        public void OnTick()
        {
            // Avoiding iterating over a changing collection by grabbing a pooled dictionary.
            var temp = DictionaryPool<Variable, VariableInterpolateData>.Get();
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
                DictionaryPool<Variable, VariableInterpolateData>.Release(temp);
            }
        }

        private void DoInterpolate(VariableInterpolateData data)
        {
            var t = (Time.time - data.startTime) / data.duration;

            var finished = data.interpolator.Interpolate(t);

            if (finished)
            {
                Util.Log($"Finished Variable interpolate.");

                _interpolationsInProgress.Remove(data.variable);
                data.done();
            }
        }

        public void StartInterpolation(ref VariableInterpolateData data)
        {
            _interpolationsInProgress.Remove(data.variable); // Stop any in-progress interpolations for this variable.

            var interpolator = GetInterpolator(data);

            data.interpolator = interpolator;

            _interpolationsInProgress.Add(data.variable, data);

            Util.Log($"Starting Variable Interpolation: Start Time {data.startTime}, Duration: {data.duration}");
        }

        public bool StopInterpolation(Variable variable)
        {
            return _interpolationsInProgress.Remove(variable);
        }

        private IInterpolator GetInterpolator(in VariableInterpolateData data)
        {
            var cp1 = data.cp1;
            var cp2 = data.cp2;

            var interpolator = data.variable.property switch
            {
                Property<float> => GetInterpolator(GetFloatEvaluator(cp1,cp2),  data),
                Property<float2> => GetInterpolator(Getfloat2Evaluator(cp1, cp2), data),
                Property<float3> => GetInterpolator(Getfloat3Evaluator(cp1, cp2), data),
                Property<float4> when data.slerp => GetInterpolator(GetquaternionEvaluator(cp1, cp2), data),
                Property<float4> when !data.slerp=> GetInterpolator(Getfloat4Evaluator(cp1, cp2), data),
                Property<float2x2> => GetInterpolator(Getfloat2x2Evaluator(cp1, cp2), data),
                Property<float3x3> => GetInterpolator(Getfloat3x3Evaluator(cp1, cp2), data),
                Property<float4x4> => GetInterpolator(Getfloat4x4Evaluator(cp1, cp2), data),

                _ => throw new NotImplementedException($"Interpolation has not been defined for type {data.variable.property.GetTypeSignature()}!"),
            };

            return interpolator;
        }

        private IInterpolator GetInterpolator<T>(Func<T, T, float, Property<T>> evaluator, in VariableInterpolateData data)
        {
            var variable = data.variable;
            var endValue = (Property<T>)data.endValue;

            return new Interpolator<T>()
            {
                variable = data.variable,
                evaluator = evaluator,
                from = ((Property<T>)variable.property).value,
                to = endValue.value
            };
        }

        private Func<float,float,float, Property<float>> GetFloatEvaluator(float2 cp1, float2 cp2)
        {
            return (a, b, t) => new Property<float>(math.lerp(a, b, Helpers.CubicBezier(t, cp1, cp2).y));
        }

        private Func<float2, float2, float, Property<float2>> Getfloat2Evaluator(float2 cp1, float2 cp2)
        {
            return (a, b, t) => new Property<float2>(math.lerp(a, b, Helpers.CubicBezier(t, cp1, cp2).y));
        }

        private Func<float3, float3, float, Property<float3>> Getfloat3Evaluator(float2 cp1, float2 cp2)
        {
            return (a, b, t) => new Property<float3>(math.lerp(a, b, Helpers.CubicBezier(t, cp1, cp2).y));
        }

        private Func<float4, float4, float, Property<float4>> Getfloat4Evaluator(float2 cp1, float2 cp2)
        {
            return (a, b, t) => new Property<float4>(math.lerp(a, b, Helpers.CubicBezier(t, cp1, cp2).y));
        }

        private Func<float4, float4, float, Property<float4>> GetquaternionEvaluator(float2 cp1, float2 cp2)
        {
            // Just a copy from unity mathematics library to avoid a bunch of type conversions.
            return (a, b, t) => new Property<float4>(Helpers.Slerpfloat4(a, b, Helpers.CubicBezier(t, cp1, cp2).y));
        }

        private Func<float2x2, float2x2, float, Property<float2x2>> Getfloat2x2Evaluator(float2 cp1, float2 cp2)
        {
            return (a, b, t) => new Property<float2x2>(Helpers.LerpComponentwise(a, b, Helpers.CubicBezier(t, cp1, cp2).y));
        }

        private Func<float3x3, float3x3, float, Property<float3x3>> Getfloat3x3Evaluator(float2 cp1, float2 cp2)
        {
            return (a, b, t) => new Property<float3x3>(Helpers.LerpComponentwise(a, b, Helpers.CubicBezier(t, cp1, cp2).y));
        }

        private Func<float4x4, float4x4, float, Property<float4x4>> Getfloat4x4Evaluator(float2 cp1, float2 cp2)
        {
            return (a, b, t) => new Property<float4x4>(Helpers.LerpComponentwise(a, b, Helpers.CubicBezier(t, cp1, cp2).y));
        }
    }
}
