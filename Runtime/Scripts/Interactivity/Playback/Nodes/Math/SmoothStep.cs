using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>math/smoothStep: t = saturate((c - min(a, b)) / |b - a|), value = t * t * (3 - 2t), per component.</summary>
    public class MathSmoothStep : BehaviourEngineNode
    {
        public MathSmoothStep(BehaviourEngine engine, Node node) : base(engine, node) { }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);
            TryEvaluateValue(ConstStrings.C, out IProperty c);

            return a switch
            {
                Property<float> pa when b is Property<float> pb && c is Property<float> pc => new Property<float>(Step(pa.value, pb.value, pc.value)),
                Property<float2> pa when b is Property<float2> pb && c is Property<float2> pc => new Property<float2>(Step(pa.value, pb.value, pc.value)),
                Property<float3> pa when b is Property<float3> pb && c is Property<float3> pc => new Property<float3>(Step(pa.value, pb.value, pc.value)),
                Property<float4> pa when b is Property<float4> pb && c is Property<float4> pc => new Property<float4>(Step(pa.value, pb.value, pc.value)),
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
