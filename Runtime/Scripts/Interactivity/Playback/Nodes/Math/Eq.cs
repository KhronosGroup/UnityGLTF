using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathEq : BehaviourEngineNode
    {
        public MathEq(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Bool when b.type == VariantType.Bool => Variant.FromBool(a.Bool == b.Bool),
                VariantType.Int when b.type == VariantType.Int => Variant.FromBool(a.Int == b.Int),
                VariantType.Float when b.type == VariantType.Float => Variant.FromBool(eq(a.Float,b.Float)),
                VariantType.Float2 when b.type == VariantType.Float2 => Variant.FromBool(AllEqual(a.Float2, b.Float2)),
                VariantType.Float3 when b.type == VariantType.Float3 => Variant.FromBool(AllEqual(a.Float3, b.Float3)),
                VariantType.Float4 when b.type == VariantType.Float4 => Variant.FromBool(AllEqual(a.Float4, b.Float4)),
                VariantType.Float2x2 when b.type == VariantType.Float2x2 => Variant.FromBool(AllEqual(a.Float2x2, b.Float2x2)),
                VariantType.Float3x3 when b.type == VariantType.Float3x3 => Variant.FromBool(AllEqual(a.Float3x3, b.Float3x3)),
                VariantType.Float4x4 when b.type == VariantType.Float4x4 => Variant.FromBool(AllEqual(a.Float4x4, b.Float4x4)),
                _ => throw new InvalidOperationException($"No supported type found or input types did not match. Types were A: {a.GetTypeSignature()}, B: {b.GetTypeSignature()}"),
            };
        }

        private static bool AllEqual(float2 a, float2 b)
        {
            return eq(a.x, b.x) && eq(a.y, b.y);
        }

        private static bool AllEqual(float3 a, float3 b)
        {
            return eq(a.x, b.x) && eq(a.y, b.y) && eq(a.z, b.z);
        }

        private static bool AllEqual(float4 a, float4 b)
        {
            return eq(a.x, b.x) && eq(a.y, b.y) && eq(a.z, b.z) && eq(a.w, b.w);
        }

        private static bool AllEqual(float2x2 a, float2x2 b)
        {
            return eq(a.c0.x, b.c0.x) && eq(a.c0.y, b.c0.y) &&
                   eq(a.c1.x, b.c1.x) && eq(a.c1.y, b.c1.y);
        }

        private static bool AllEqual(float3x3 a, float3x3 b)
        {
            return eq(a.c0.x, b.c0.x) && eq(a.c0.y, b.c0.y) && eq(a.c0.z, b.c0.z) &&
                   eq(a.c1.x, b.c1.x) && eq(a.c1.y, b.c1.y) && eq(a.c1.z, b.c1.z) &&
                   eq(a.c2.x, b.c2.x) && eq(a.c2.y, b.c2.y) && eq(a.c2.z, b.c2.z);
        }

        private static bool AllEqual(float4x4 a, float4x4 b)
        {
            return eq(a.c0.x, b.c0.x) && eq(a.c0.y, b.c0.y) && eq(a.c0.z, b.c0.z) && eq(a.c0.w, b.c0.w) &&
                   eq(a.c1.x, b.c1.x) && eq(a.c1.y, b.c1.y) && eq(a.c1.z, b.c1.z) && eq(a.c1.w, b.c1.w) &&
                   eq(a.c2.x, b.c2.x) && eq(a.c2.y, b.c2.y) && eq(a.c2.z, b.c2.z) && eq(a.c2.w, b.c2.w) &&
                   eq(a.c3.x, b.c3.x) && eq(a.c3.y, b.c3.y) && eq(a.c3.z, b.c3.z) && eq(a.c3.w, b.c3.w);
        }

        private static bool eq(float a, float b)
        {
            // Exact IEEE-754 comparison: NaN is never equal, +0 equals -0, +Inf equals only +Inf.
            return a == b;
        }
    }
}