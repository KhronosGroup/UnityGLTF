using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathAnd : BehaviourEngineNode
    {
        public MathAnd(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<int> aProp when b is Property<int> bProp => new Property<int>(aProp.value & bProp.value),
                Property<bool> aProp when b is Property<bool> bProp => new Property<bool>(aProp.value && bProp.value),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }
    }
}