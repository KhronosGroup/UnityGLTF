using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathNeg : BehaviourEngineNode
    {
        public MathNeg(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Int => Variant.FromInt(-a.Int),
                VariantType.Float => Variant.FromFloat(-a.Float),
                VariantType.Float2 => Variant.FromFloat2(-a.Float2),
                VariantType.Float3 => Variant.FromFloat3(-a.Float3),
                VariantType.Float4 => Variant.FromFloat4(-a.Float4),
                VariantType.Float2x2 => Variant.FromFloat2x2(-a.Float2x2),
                VariantType.Float3x3 => Variant.FromFloat3x3(-a.Float3x3),
                VariantType.Float4x4 => Variant.FromFloat4x4(-a.Float4x4),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}