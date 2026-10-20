using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>math/smoothStep: t = saturate((c - min(a, b)) / |b - a|), value = t * t * (3 - 2t), per component.</summary>
    public class MathSmoothStep : BehaviourEngineNode
    {
        public MathSmoothStep(BehaviourEngine engine, Node node) : base(engine, node) { }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);
            TryEvaluateValue(ConstStrings.C, out Variant c);

            return a.type switch
            {
                VariantType.Float when b.type == VariantType.Float && c.type == VariantType.Float => Variant.FromFloat(Step(a.Float, b.Float, c.Float)),
                VariantType.Float2 when b.type == VariantType.Float2 && c.type == VariantType.Float2 => Variant.FromFloat2(Step(a.Float2, b.Float2, c.Float2)),
                VariantType.Float3 when b.type == VariantType.Float3 && c.type == VariantType.Float3 => Variant.FromFloat3(Step(a.Float3, b.Float3, c.Float3)),
                VariantType.Float4 when b.type == VariantType.Float4 && c.type == VariantType.Float4 => Variant.FromFloat4(Step(a.Float4, b.Float4, c.Float4)),
                _ => throw new InvalidOperationException("math/smoothStep requires a, b and c of the same floatN type."),
            };
        }

        private static float Step(float a, float b, float c) => Step(new float4(a), new float4(b), new float4(c)).x;
        private static float2 Step(float2 a, float2 b, float2 c) => Step(new float4(a, 0, 0), new float4(b, 0, 0), new float4(c, 0, 0)).xy;
        private static float3 Step(float3 a, float3 b, float3 c) => Step(new float4(a, 0), new float4(b, 0), new float4(c, 0)).xyz;

        private static float4 Step(float4 a, float4 b, float4 c)
        {
            // saturate(x) = min(max(x, 0), 1), written out so NaN propagates like the spec's arithmetic.
            var t = (c - math.min(a, b)) / math.abs(b - a);
            t = Saturate(t);
            return t * t * (3f - 2f * t);
        }

        private static float4 Saturate(float4 v)
        {
            return new float4(S(v.x), S(v.y), S(v.z), S(v.w));
            static float S(float x) => float.IsNaN(x) ? float.NaN : math.min(math.max(x, 0f), 1f);
        }
    }
}
