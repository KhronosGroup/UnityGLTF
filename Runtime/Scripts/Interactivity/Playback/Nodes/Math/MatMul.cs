using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathMatMul : BehaviourEngineNode
    {
        public MathMatMul(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Float2x2 when b.type == VariantType.Float2x2 => Variant.FromFloat2x2(math.mul(a.Float2x2, b.Float2x2)),
                VariantType.Float3x3 when b.type == VariantType.Float3x3 => Variant.FromFloat3x3(math.mul(a.Float3x3, b.Float3x3)),
                VariantType.Float4x4 when b.type == VariantType.Float4x4 => Variant.FromFloat4x4(math.mul(a.Float4x4, b.Float4x4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}