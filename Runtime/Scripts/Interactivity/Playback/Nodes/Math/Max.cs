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

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Int when b.type == VariantType.Int => Variant.FromInt(math.max(a.Int, b.Int)),
                VariantType.Float when b.type == VariantType.Float => Variant.FromFloat(Max(a.Float, b.Float)),
                VariantType.Float2 when b.type == VariantType.Float2 => Variant.FromFloat2(Max(a.Float2, b.Float2)),
                VariantType.Float3 when b.type == VariantType.Float3 => Variant.FromFloat3(Max(a.Float3, b.Float3)),
                VariantType.Float4 when b.type == VariantType.Float4 => Variant.FromFloat4(Max(a.Float4, b.Float4)),
                VariantType.Float2x2 when b.type == VariantType.Float2x2 => Variant.FromFloat2x2(Max(a.Float2x2, b.Float2x2)),
                VariantType.Float3x3 when b.type == VariantType.Float3x3 => Variant.FromFloat3x3(Max(a.Float3x3, b.Float3x3)),
                VariantType.Float4x4 when b.type == VariantType.Float4x4 => Variant.FromFloat4x4(Max(a.Float4x4, b.Float4x4)),
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
