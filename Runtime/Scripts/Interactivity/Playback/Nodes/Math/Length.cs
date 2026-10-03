using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathLength : BehaviourEngineNode
    {
        public MathLength(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float2 => Variant.FromFloat(math.length(a.Float2)),
                VariantType.Float3 => Variant.FromFloat(math.length(a.Float3)),
                VariantType.Float4 => Variant.FromFloat(math.length(a.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}