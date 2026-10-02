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

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<float> aFloat when b is Property<float> bFloat => new Property<float>(Pow(aFloat.value, bFloat.value)),
                Property<float2> aVec2 when b is Property<float2> bVec2 => new Property<float2>(Pow(aVec2.value, bVec2.value)),
                Property<float3> aVec3 when b is Property<float3> bVec3 => new Property<float3>(Pow(aVec3.value, bVec3.value)),
                Property<float4> aVec4 when b is Property<float4> bVec4 => new Property<float4>(Pow(aVec4.value, bVec4.value)),
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