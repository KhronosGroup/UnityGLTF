using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathRound : BehaviourEngineNode
    {
        public MathRound(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> aProp => new Property<float>(math.round(aProp.value)),
                Property<float2> aProp => new Property<float2>(math.round(aProp.value)),
                Property<float3> aProp => new Property<float3>(math.round(aProp.value)),
                Property<float4> aProp => new Property<float4>(math.round(aProp.value)),
                Property<float2x2> aProp => new Property<float2x2>(round(aProp.value)),
                Property<float3x3> aProp => new Property<float3x3>(round(aProp.value)),
                Property<float4x4> aProp => new Property<float4x4>(round(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 round(float2x2 a)
        {
            return new float2x2(math.round(a.c0), math.round(a.c1));
        }

        private static float3x3 round(float3x3 a)
        {
            return new float3x3(math.round(a.c0), math.round(a.c1), math.round(a.c2));
        }

        private static float4x4 round(float4x4 a)
        {
            return new float4x4(math.round(a.c0), math.round(a.c1), math.round(a.c2), math.round(a.c3));
        }
    }

}