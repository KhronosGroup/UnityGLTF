using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCombine2 : BehaviourEngineNode
    {
        public MathCombine2(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            if (a is not Property<float> aFloat)
                throw new InvalidOperationException("Input A is not a float!");

            if (b is not Property<float> bFloat)
                throw new InvalidOperationException("Input B is not a float!");

            return new Property<float2>(new float2(aFloat.value, bFloat.value));
        }
    }
}