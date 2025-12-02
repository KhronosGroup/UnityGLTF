using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCtz : BehaviourEngineNode
    {
        public MathCtz(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<int> aProp => new Property<int>(CountTrailingZeros(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static int CountTrailingZeros(int n)
        {
            int mask = 1;
            for (int i = 0; i < 32; i++, mask <<= 1)
                if ((n & mask) != 0)
                    return i;

            return 32;
        }
    }
}