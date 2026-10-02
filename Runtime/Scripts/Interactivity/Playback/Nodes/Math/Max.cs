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
                Property<float> aProp when b is Property<float> bProp => new Property<float>(Max(aProp.value, bProp.value)),
                Property<float2> aProp when b is Property<float2> bProp => new Property<float2>(Max(aProp.value, bProp.value)),
                Property<float3> aProp when b is Property<float3> bProp => new Property<float3>(Max(aProp.value, bProp.value)),
                Property<float4> aProp when b is Property<float4> bProp => new Property<float4>(Max(aProp.value, bProp.value)),
                Property<float2x2> aProp when b is Property<float2x2> bProp => new Property<float2x2>(Max(aProp.value, bProp.value)),
                Property<float3x3> aProp when b is Property<float3x3> bProp => new Property<float3x3>(Max(aProp.value, bProp.value)),
                Property<float4x4> aProp when b is Property<float4x4> bProp => new Property<float4x4>(Max(aProp.value, bProp.value)),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }

        // ECMAScript Math.max semantics: NaN propagates and +0 is greater than -0 (math.max does neither).
        public static float Max(float a, float b)
        {
            if (float.IsNaN(a) || float.IsNaN(b))
                return float.NaN;
            if (a == b)
                return float.IsNegative(a) ? b : a;
            return a > b ? a : b;
        }

        public static float2 Max(float2 a, float2 b) => new float2(Max(a.x, b.x), Max(a.y, b.y));
        public static float3 Max(float3 a, float3 b) => new float3(Max(a.x, b.x), Max(a.y, b.y), Max(a.z, b.z));
        public static float4 Max(float4 a, float4 b) => new float4(Max(a.x, b.x), Max(a.y, b.y), Max(a.z, b.z), Max(a.w, b.w));
        public static float2x2 Max(float2x2 a, float2x2 b) => new float2x2(Max(a.c0, b.c0), Max(a.c1, b.c1));
        public static float3x3 Max(float3x3 a, float3x3 b) => new float3x3(Max(a.c0, b.c0), Max(a.c1, b.c1), Max(a.c2, b.c2));
        public static float4x4 Max(float4x4 a, float4x4 b) => new float4x4(Max(a.c0, b.c0), Max(a.c1, b.c1), Max(a.c2, b.c2), Max(a.c3, b.c3));
    }
}
