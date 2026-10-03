using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCosH : BehaviourEngineNode
    {
        public MathCosH(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(math.cosh(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(math.cosh(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(math.cosh(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(math.cosh(a.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}