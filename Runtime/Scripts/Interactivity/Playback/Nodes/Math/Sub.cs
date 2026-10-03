using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSub : BehaviourEngineNode
    {
        public MathSub(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Int when b.type == VariantType.Int => Variant.FromInt(a.Int - b.Int),
                VariantType.Float when b.type == VariantType.Float => Variant.FromFloat(a.Float - b.Float),
                VariantType.Float2 when b.type == VariantType.Float2 => Variant.FromFloat2(a.Float2 - b.Float2),
                VariantType.Float3 when b.type == VariantType.Float3 => Variant.FromFloat3(a.Float3 - b.Float3),
                VariantType.Float4 when b.type == VariantType.Float4 => Variant.FromFloat4(a.Float4 - b.Float4),
                VariantType.Float2x2 when b.type == VariantType.Float2x2 => Variant.FromFloat2x2(a.Float2x2 - b.Float2x2),
                VariantType.Float3x3 when b.type == VariantType.Float3x3 => Variant.FromFloat3x3(a.Float3x3 - b.Float3x3),
                VariantType.Float4x4 when b.type == VariantType.Float4x4 => Variant.FromFloat4x4(a.Float4x4 - b.Float4x4),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }
    }
}