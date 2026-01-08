using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathFract : BehaviourEngineNode
    {
        public MathFract(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> aProp => new Property<float>(math.frac(aProp.value)),
                Property<float2> aProp => new Property<float2>(math.frac(aProp.value)),
                Property<float3> aProp => new Property<float3>(math.frac(aProp.value)),
                Property<float4> aProp => new Property<float4>(math.frac(aProp.value)),
                Property<float2x2> aProp => new Property<float2x2>(fract(aProp.value)),
                Property<float3x3> aProp => new Property<float3x3>(fract(aProp.value)),
                Property<float4x4> aProp => new Property<float4x4>(fract(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 fract(float2x2 a)
        {
            return new float2x2(math.frac(a.c0), math.frac(a.c1));
        }

        private static float3x3 fract(float3x3 a)
        {
            return new float3x3(math.frac(a.c0), math.frac(a.c1), math.frac(a.c2));
        }

        private static float4x4 fract(float4x4 a)
        {
            return new float4x4(math.frac(a.c0), math.frac(a.c1), math.frac(a.c2), math.frac(a.c3));
        }
    }

}