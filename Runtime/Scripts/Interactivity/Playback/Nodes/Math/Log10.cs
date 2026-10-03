using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathLog10 : BehaviourEngineNode
    {
        public MathLog10(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(math.log10(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(math.log10(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(math.log10(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(math.log10(a.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}