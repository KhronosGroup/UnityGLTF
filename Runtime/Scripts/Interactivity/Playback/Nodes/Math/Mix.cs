using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathMix : BehaviourEngineNode
    {
        public MathMix(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);
            TryEvaluateValue(ConstStrings.C, out Variant c);

            return a.type switch
            {
                VariantType.Float when b.type == VariantType.Float && c.type == VariantType.Float => Variant.FromFloat(math.lerp(a.Float, b.Float, c.Float)),
                VariantType.Float2 when b.type == VariantType.Float2 && c.type == VariantType.Float2 => Variant.FromFloat2(math.lerp(a.Float2, b.Float2, c.Float2)),
                VariantType.Float3 when b.type == VariantType.Float3 && c.type == VariantType.Float3 => Variant.FromFloat3(math.lerp(a.Float3, b.Float3, c.Float3)),
                VariantType.Float4 when b.type == VariantType.Float4 && c.type == VariantType.Float4 => Variant.FromFloat4(math.lerp(a.Float4, b.Float4, c.Float4)),
                VariantType.Float2x2 when b.type == VariantType.Float2x2 && c.type == VariantType.Float2x2 =>Variant.FromFloat2x2(lerp(a.Float2x2, b.Float2x2, c.Float2x2)),
                VariantType.Float3x3 when b.type == VariantType.Float3x3 && c.type == VariantType.Float3x3 =>Variant.FromFloat3x3(lerp(a.Float3x3, b.Float3x3, c.Float3x3)),
                VariantType.Float4x4 when b.type == VariantType.Float4x4 && c.type == VariantType.Float4x4 =>Variant.FromFloat4x4(lerp(a.Float4x4, b.Float4x4, c.Float4x4)),

                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }

        private static float2x2 lerp(float2x2 a, float2x2 b, float2x2 t)
        {
            return new float2x2(math.lerp(a.c0, b.c0, t.c0), math.lerp(a.c1, b.c1, t.c1));
        }

        private static float3x3 lerp(float3x3 a, float3x3 b, float3x3 t)
        {
            return new float3x3(
                math.lerp(a.c0, b.c0, t.c0),
                math.lerp(a.c1, b.c1, t.c1),
                math.lerp(a.c2, b.c2, t.c2)
            );
        }

        private static float4x4 lerp(float4x4 a, float4x4 b, float4x4 t)
        {
            return new float4x4(
                math.lerp(a.c0, b.c0, t.c0),
                math.lerp(a.c1, b.c1, t.c1),
                math.lerp(a.c2, b.c2, t.c2),
                math.lerp(a.c3, b.c3, t.c3)
            );
        }
    }
}