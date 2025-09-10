using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathClz : BehaviourEngineNode
    {
        public MathClz(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<int> aProp => new Property<int>(CountLeadingZeros(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static int CountLeadingZeros(int x)
        {
            const int numIntBits = sizeof(int) * 8;

            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;

            x -= x >> 1 & 0x55555555;
            x = (x >> 2 & 0x33333333) + (x & 0x33333333);
            x = (x >> 4) + x & 0x0f0f0f0f;
            x += x >> 8;
            x += x >> 16;

            return numIntBits - (x & 0x0000003f);
        }
    }
}