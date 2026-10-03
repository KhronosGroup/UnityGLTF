using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCeil : BehaviourEngineNode
    {
        public MathCeil(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(math.ceil(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(math.ceil(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(math.ceil(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(math.ceil(a.Float4)),
                VariantType.Float2x2 => Variant.FromFloat2x2(ceil(a.Float2x2)),
                VariantType.Float3x3 => Variant.FromFloat3x3(ceil(a.Float3x3)),
                VariantType.Float4x4 => Variant.FromFloat4x4(ceil(a.Float4x4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 ceil(float2x2 a)
        {
            return new float2x2(math.ceil(a.c0), math.ceil(a.c1));
        }

        private static float3x3 ceil(float3x3 a)
        {
            return new float3x3(math.ceil(a.c0), math.ceil(a.c1), math.ceil(a.c2));
        }

        private static float4x4 ceil(float4x4 a)
        {
            return new float4x4(math.ceil(a.c0), math.ceil(a.c1), math.ceil(a.c2), math.ceil(a.c3));
        }
    }

}