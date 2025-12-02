using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathMax : BehaviourEngineNode
    {
        public MathMax(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<int> aProp when b is Property<int> bProp => new Property<int>(math.max(aProp.value, bProp.value)),
                Property<float> aProp when b is Property<float> bProp => new Property<float>(math.max(aProp.value, bProp.value)),
                Property<float2> aProp when b is Property<float2> bProp => new Property<float2>(math.max(aProp.value, bProp.value)),
                Property<float3> aProp when b is Property<float3> bProp => new Property<float3>(math.max(aProp.value, bProp.value)),
                Property<float4> aProp when b is Property<float4> bProp => new Property<float4>(math.max(aProp.value, bProp.value)),
                Property<float2x2> aProp when b is Property<float2x2> bProp => new Property<float2x2>(max(aProp.value, bProp.value)),
                Property<float3x3> aProp when b is Property<float3x3> bProp => new Property<float3x3>(max(aProp.value, bProp.value)),
                Property<float4x4> aProp when b is Property<float4x4> bProp => new Property<float4x4>(max(aProp.value, bProp.value)),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }

        private static float2x2 max(float2x2 a, float2x2 b)
        {
            return new float2x2(math.max(a.c0, b.c0), math.max(a.c1, b.c1));
        }

        private static float3x3 max(float3x3 a, float3x3 b)
        {
            return new float3x3(math.max(a.c0, b.c0), math.max(a.c1, b.c1), math.max(a.c2, b.c2));
        }

        private static float4x4 max(float4x4 a, float4x4 b)
        {
            return new float4x4(math.max(a.c0, b.c0), math.max(a.c1, b.c1), math.max(a.c2, b.c2), math.max(a.c3, b.c3));
        }
    }
}