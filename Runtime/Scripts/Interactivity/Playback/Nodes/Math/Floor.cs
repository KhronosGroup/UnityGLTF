using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathFloor : BehaviourEngineNode
    {
        public MathFloor(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(math.floor(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(math.floor(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(math.floor(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(math.floor(a.Float4)),
                VariantType.Float2x2 => Variant.FromFloat2x2(floor(a.Float2x2)),
                VariantType.Float3x3 => Variant.FromFloat3x3(floor(a.Float3x3)),
                VariantType.Float4x4 => Variant.FromFloat4x4(floor(a.Float4x4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 floor(float2x2 a)
        {
            return new float2x2(math.floor(a.c0), math.floor(a.c1));
        }

        private static float3x3 floor(float3x3 a)
        {
            return new float3x3(math.floor(a.c0), math.floor(a.c1), math.floor(a.c2));
        }

        private static float4x4 floor(float4x4 a)
        {
            return new float4x4(math.floor(a.c0), math.floor(a.c1), math.floor(a.c2), math.floor(a.c3));
        }
    }
}