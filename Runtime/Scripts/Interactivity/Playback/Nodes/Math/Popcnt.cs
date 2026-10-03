using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathPopcnt : BehaviourEngineNode
    {
        public MathPopcnt(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Int => Variant.FromInt(math.countbits(a.Int)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}