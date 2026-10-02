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
                Property<float> prop => new Property<float>(Sign(prop.value)),
                Property<float2> prop => new Property<float2>(Sign(prop.value)),
                Property<float3> prop => new Property<float3>(Sign(prop.value)),
                Property<float4> prop => new Property<float4>(Sign(prop.value)),
                Property<float2x2> prop => new Property<float2x2>(sign(prop.value)),
                Property<float3x3> prop => new Property<float3x3>(sign(prop.value)),
                Property<float4x4> prop => new Property<float4x4>(sign(prop.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        // Spec: -1 if a < 0, a if a is +-0, +1 if a > 0; NaN propagates (math.sign returns 0 and drops -0).
        public static float Sign(float a) => a > 0f ? 1f : a < 0f ? -1f : a;
        public static float2 Sign(float2 a) => new float2(Sign(a.x), Sign(a.y));
        public static float3 Sign(float3 a) => new float3(Sign(a.x), Sign(a.y), Sign(a.z));
        public static float4 Sign(float4 a) => new float4(Sign(a.x), Sign(a.y), Sign(a.z), Sign(a.w));
        private static float2x2 sign(float2x2 a) => new float2x2(Sign(a.c0), Sign(a.c1));
        private static float3x3 sign(float3x3 a) => new float3x3(Sign(a.c0), Sign(a.c1), Sign(a.c2));
        private static float4x4 sign(float4x4 a) => new float4x4(Sign(a.c0), Sign(a.c1), Sign(a.c2), Sign(a.c3));
    }
}