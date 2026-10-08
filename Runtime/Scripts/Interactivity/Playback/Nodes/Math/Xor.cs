using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathXor : BehaviourEngineNode
    {
        public MathXor(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Int when b.type == VariantType.Int => Variant.FromInt(a.Int ^ b.Int),
                VariantType.Bool when b.type == VariantType.Bool => Variant.FromBool(a.Bool ^ b.Bool),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }
    }
}