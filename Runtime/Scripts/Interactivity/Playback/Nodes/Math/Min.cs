using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathMin : BehaviourEngineNode
    {
        public MathMin(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<int> aProp when b is Property<int> bProp => new Property<int>(math.min(aProp.value, bProp.value)),
                Property<float> aProp when b is Property<float> bProp => new Property<float>(Min(aProp.value, bProp.value)),
                Property<float2> aProp when b is Property<float2> bProp => new Property<float2>(Min(aProp.value, bProp.value)),
                Property<float3> aProp when b is Property<float3> bProp => new Property<float3>(Min(aProp.value, bProp.value)),
                Property<float4> aProp when b is Property<float4> bProp => new Property<float4>(Min(aProp.value, bProp.value)),
                Property<float2x2> aProp when b is Property<float2x2> bProp => new Property<float2x2>(Min(aProp.value, bProp.value)),
                Property<float3x3> aProp when b is Property<float3x3> bProp => new Property<float3x3>(Min(aProp.value, bProp.value)),
                Property<float4x4> aProp when b is Property<float4x4> bProp => new Property<float4x4>(Min(aProp.value, bProp.value)),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }

        // ECMAScript Math.min semantics: NaN propagates and -0 is less than +0 (math.min does neither).
        public static float Min(float a, float b)
        {
            if (float.IsNaN(a) || float.IsNaN(b))
                return float.NaN;
            if (a == b)
                return float.IsNegative(a) ? a : b;
            return a < b ? a : b;
        }

        public static float2 Min(float2 a, float2 b) => new float2(Min(a.x, b.x), Min(a.y, b.y));
        public static float3 Min(float3 a, float3 b) => new float3(Min(a.x, b.x), Min(a.y, b.y), Min(a.z, b.z));
        public static float4 Min(float4 a, float4 b) => new float4(Min(a.x, b.x), Min(a.y, b.y), Min(a.z, b.z), Min(a.w, b.w));
        public static float2x2 Min(float2x2 a, float2x2 b) => new float2x2(Min(a.c0, b.c0), Min(a.c1, b.c1));
        public static float3x3 Min(float3x3 a, float3x3 b) => new float3x3(Min(a.c0, b.c0), Min(a.c1, b.c1), Min(a.c2, b.c2));
        public static float4x4 Min(float4x4 a, float4x4 b) => new float4x4(Min(a.c0, b.c0), Min(a.c1, b.c1), Min(a.c2, b.c2), Min(a.c3, b.c3));
    }
}
