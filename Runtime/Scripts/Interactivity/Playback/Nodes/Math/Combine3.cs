using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCombine3 : BehaviourEngineNode
    {
        public MathCombine3(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);
            TryEvaluateValue(ConstStrings.C, out IProperty c);

            if (a is not Property<float> aFloat)
                throw new InvalidOperationException("Input A is not a float!");

            if (b is not Property<float> bFloat)
                throw new InvalidOperationException("Input B is not a float!");

            if (c is not Property<float> cFloat)
                throw new InvalidOperationException("Input C is not a float!");

            return new Property<float3>(new float3(aFloat.value, bFloat.value, cFloat.value));
        }
    }
}