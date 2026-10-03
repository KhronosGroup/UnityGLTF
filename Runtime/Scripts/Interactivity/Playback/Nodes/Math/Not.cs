using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathNot : BehaviourEngineNode
    {
        public MathNot(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Int => Variant.FromInt(~a.Int),
                VariantType.Bool => Variant.FromBool(!a.Bool),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()}."),
            };
        }
    }
}