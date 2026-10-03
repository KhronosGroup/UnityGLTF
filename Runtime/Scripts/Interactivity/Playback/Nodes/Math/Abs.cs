using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathAbs : BehaviourEngineNode
    {
        public MathAbs(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Int => Variant.FromInt(math.abs(a.Int)),
                VariantType.Float => Variant.FromFloat(math.abs(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(math.abs(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(math.abs(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(math.abs(a.Float4)),
                VariantType.Float2x2 => Variant.FromFloat2x2(abs(a.Float2x2)),
                VariantType.Float3x3 => Variant.FromFloat3x3(abs(a.Float3x3)),
                VariantType.Float4x4 => Variant.FromFloat4x4(abs(a.Float4x4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 abs(float2x2 a)
        {
            return new float2x2(math.abs(a.c0), math.abs(a.c1));
        }

        private static float3x3 abs(float3x3 a)
        {
            return new float3x3(math.abs(a.c0), math.abs(a.c1), math.abs(a.c2));
        }

        private static float4x4 abs(float4x4 a)
        {
            return new float4x4(math.abs(a.c0), math.abs(a.c1), math.abs(a.c2), math.abs(a.c3));
        }
    }
}