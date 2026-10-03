using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSqrt : BehaviourEngineNode
    {
        public MathSqrt(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(math.sqrt(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(math.sqrt(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(math.sqrt(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(math.sqrt(a.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}