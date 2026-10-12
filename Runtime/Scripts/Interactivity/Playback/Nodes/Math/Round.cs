using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathRound : BehaviourEngineNode
    {
        public MathRound(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(round(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(round(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(round(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(round(a.Float4)),
                VariantType.Float2x2 => Variant.FromFloat2x2(round(a.Float2x2)),
                VariantType.Float3x3 => Variant.FromFloat3x3(round(a.Float3x3)),
                VariantType.Float4x4 => Variant.FromFloat4x4(round(a.Float4x4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 round(float2x2 a)
        {
            return new float2x2(round(a.c0), round(a.c1));
        }

        private static float3x3 round(float3x3 a)
        {
            return new float3x3(round(a.c0), round(a.c1), round(a.c2));
        }

        private static float4x4 round(float4x4 a)
        {
            return new float4x4(round(a.c0), round(a.c1), round(a.c2), round(a.c3));
        }

        private static float2 round(float2 a)
        {
            return new float2(round(a.x), round(a.y));
        }

        private static float3 round(float3 a)
        {
            return new float3(round(a.x), round(a.y), round(a.z));
        }

        private static float4 round(float4 a)
        {
            return new float4(round(a.x), round(a.y), round(a.z), round(a.w));
        }

        private static float round(float f)
        {
            return (float)System.Math.Round(f, MidpointRounding.AwayFromZero);
        }
    }

}