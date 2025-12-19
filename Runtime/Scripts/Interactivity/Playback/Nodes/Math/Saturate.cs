using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSaturate : BehaviourEngineNode
    {
        public MathSaturate(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> aProp => new Property<float>(math.saturate(aProp.value)),
                Property<float2> aProp => new Property<float2>(math.saturate(aProp.value)),
                Property<float3> aProp => new Property<float3>(math.saturate(aProp.value)),
                Property<float4> aProp => new Property<float4>(math.saturate(aProp.value)),
                Property<float2x2> aProp => new Property<float2x2>(saturate(aProp.value)),
                Property<float3x3> aProp => new Property<float3x3>(saturate(aProp.value)),
                Property<float4x4> aProp => new Property<float4x4>(saturate(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 saturate(float2x2 a)
        {
            return new float2x2(math.saturate(a.c0), math.saturate(a.c1));
        }

        private static float3x3 saturate(float3x3 a)
        {
            return new float3x3(math.saturate(a.c0), math.saturate(a.c1), math.saturate(a.c2));
        }

        private static float4x4 saturate(float4x4 a)
        {
            return new float4x4(math.saturate(a.c0), math.saturate(a.c1), math.saturate(a.c2), math.saturate(a.c3));
        }
    }
}