using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathTranspose : BehaviourEngineNode
    {
        public MathTranspose(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float2x2 => Variant.FromFloat2x2(math.transpose(a.Float2x2)),
                VariantType.Float3x3 => Variant.FromFloat3x3(math.transpose(a.Float3x3)),
                VariantType.Float4x4 => Variant.FromFloat4x4(math.transpose(a.Float4x4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}