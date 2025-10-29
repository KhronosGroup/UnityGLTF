using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSign : BehaviourEngineNode
    {
        public MathSign(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<int> prop => new Property<int>((int)math.sign(prop.value)),
                Property<float> prop => new Property<float>(math.sign(prop.value)),
                Property<float2> prop => new Property<float2>(math.sign(prop.value)),
                Property<float3> prop => new Property<float3>(math.sign(prop.value)),
                Property<float4> prop => new Property<float4>(math.sign(prop.value)),
                Property<float2x2> prop => new Property<float2x2>(sign(prop.value)),
                Property<float3x3> prop => new Property<float3x3>(sign(prop.value)),
                Property<float4x4> prop => new Property<float4x4>(sign(prop.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 sign(float2x2 a)
        {
            return new float2x2(math.sign(a.c0), math.sign(a.c1));
        }

        private static float3x3 sign(float3x3 a)
        {
            return new float3x3(math.sign(a.c0), math.sign(a.c1), math.sign(a.c2));
        }

        private static float4x4 sign(float4x4 a)
        {
            return new float4x4(math.sign(a.c0), math.sign(a.c1), math.sign(a.c2), math.sign(a.c3));
        }
    }
}