using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathDot : BehaviourEngineNode
    {
        public MathDot(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Float2 when b.type == VariantType.Float2 => Variant.FromFloat(math.dot(a.Float2, b.Float2)),
                VariantType.Float3 when b.type == VariantType.Float3 => Variant.FromFloat(math.dot(a.Float3, b.Float3)),
                VariantType.Float4 when b.type == VariantType.Float4 => Variant.FromFloat(math.dot(a.Float4, b.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}