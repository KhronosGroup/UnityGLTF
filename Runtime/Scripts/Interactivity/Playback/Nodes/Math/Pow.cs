using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathPow : BehaviourEngineNode
    {
        public MathPow(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Float when b.type == VariantType.Float => Variant.FromFloat(Pow(a.Float, b.Float)),
                VariantType.Float2 when b.type == VariantType.Float2 => Variant.FromFloat2(Pow(a.Float2, b.Float2)),
                VariantType.Float3 when b.type == VariantType.Float3 => Variant.FromFloat3(Pow(a.Float3, b.Float3)),
                VariantType.Float4 when b.type == VariantType.Float4 => Variant.FromFloat4(Pow(a.Float4, b.Float4)),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }

        // Spec (ECMAScript a ** b): +-1 ^ +-Infinity and +-1 ^ NaN are NaN, where IEEE pow gives 1.
        public static float Pow(float a, float b) => math.abs(a) == 1f && !math.isfinite(b) ? float.NaN : math.pow(a, b);
        public static float2 Pow(float2 a, float2 b) => new float2(Pow(a.x, b.x), Pow(a.y, b.y));
        public static float3 Pow(float3 a, float3 b) => new float3(Pow(a.x, b.x), Pow(a.y, b.y), Pow(a.z, b.z));
        public static float4 Pow(float4 a, float4 b) => new float4(Pow(a.x, b.x), Pow(a.y, b.y), Pow(a.z, b.z), Pow(a.w, b.w));
    }
}