using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathDeterminant : BehaviourEngineNode
    {
        public MathDeterminant(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float2x2 => Variant.FromFloat(math.determinant(a.Float2x2)),
                VariantType.Float3x3 => Variant.FromFloat(math.determinant(a.Float3x3)),
                VariantType.Float4x4 => Variant.FromFloat(math.determinant(a.Float4x4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}