using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCos : BehaviourEngineNode
    {
        public MathCos(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(math.cos(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(math.cos(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(math.cos(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(math.cos(a.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}