using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathNot : BehaviourEngineNode
    {
        public MathNot(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<int> aProp => new Property<int>(~aProp.value),
                Property<bool> aProp => new Property<bool>(!aProp.value),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()}."),
            };
        }
    }
}