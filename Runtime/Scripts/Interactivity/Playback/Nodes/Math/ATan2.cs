using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathATan2 : BehaviourEngineNode
    {
        public MathATan2(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Float when b.type == VariantType.Float => Variant.FromFloat(math.atan2(a.Float, b.Float)),
                VariantType.Float2 when b.type == VariantType.Float2 => Variant.FromFloat2(math.atan2(a.Float2, b.Float2)),
                VariantType.Float3 when b.type == VariantType.Float3 => Variant.FromFloat3(math.atan2(a.Float3, b.Float3)),
                VariantType.Float4 when b.type == VariantType.Float4 => Variant.FromFloat4(math.atan2(a.Float4, b.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}