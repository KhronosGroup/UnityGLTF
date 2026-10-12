using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCross : BehaviourEngineNode
    {
        public MathCross(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Float3 when b.type == VariantType.Float3 => Variant.FromFloat3(math.cross(a.Float3, b.Float3)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}