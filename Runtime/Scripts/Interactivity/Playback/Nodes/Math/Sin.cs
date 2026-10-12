using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSin : BehaviourEngineNode
    {
        public MathSin(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(math.sin(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(math.sin(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(math.sin(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(math.sin(a.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}