using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathTransform : BehaviourEngineNode
    {
        public MathTransform(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Float2 when b.type == VariantType.Float2x2 => Variant.FromFloat2(math.mul(b.Float2x2, a.Float2)),
                VariantType.Float3 when b.type == VariantType.Float3x3 => Variant.FromFloat3(math.mul(b.Float3x3, a.Float3)),
                VariantType.Float4 when b.type == VariantType.Float4x4 => Variant.FromFloat4(math.mul(b.Float4x4, a.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}