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

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<int> aProp => new Property<int>(math.countbits(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}