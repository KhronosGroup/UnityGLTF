using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCombine4 : BehaviourEngineNode
    {
        public MathCombine4(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);
            TryEvaluateValue(ConstStrings.C, out IProperty c);
            TryEvaluateValue(ConstStrings.D, out IProperty d);

            if (a is not Property<float> aFloat)
                throw new InvalidOperationException("Input A is not a float!");

            if (b is not Property<float> bFloat)
                throw new InvalidOperationException("Input B is not a float!");

            if (c is not Property<float> cFloat)
                throw new InvalidOperationException("Input C is not a float!");

            if (d is not Property<float> dFloat)
                throw new InvalidOperationException("Input D is not a float!");

            return new Property<float4>(new float4(aFloat.value, bFloat.value, cFloat.value, dFloat.value));
        }
    }
}