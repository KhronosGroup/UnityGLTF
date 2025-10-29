using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathTrunc : BehaviourEngineNode
    {
        public MathTrunc(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> aProp => new Property<float>(math.trunc(aProp.value)),
                Property<float2> aProp => new Property<float2>(math.trunc(aProp.value)),
                Property<float3> aProp => new Property<float3>(math.trunc(aProp.value)),
                Property<float4> aProp => new Property<float4>(math.trunc(aProp.value)),
                Property<float2x2> aProp => new Property<float2x2>(trunc(aProp.value)),
                Property<float3x3> aProp => new Property<float3x3>(trunc(aProp.value)),
                Property<float4x4> aProp => new Property<float4x4>(trunc(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 trunc(float2x2 a)
        {
            return new float2x2(math.trunc(a.c0), math.trunc(a.c1));
        }

        private static float3x3 trunc(float3x3 a)
        {
            return new float3x3(math.trunc(a.c0), math.trunc(a.c1), math.trunc(a.c2));
        }

        private static float4x4 trunc(float4x4 a)
        {
            return new float4x4(math.trunc(a.c0), math.trunc(a.c1), math.trunc(a.c2), math.trunc(a.c3));
        }
    }

}