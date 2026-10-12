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

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            if (a.type != VariantType.Float)
                throw new InvalidOperationException("Input A is not a float!");

            if (b.type != VariantType.Float)
                throw new InvalidOperationException("Input B is not a float!");

            return Variant.FromFloat2(new float2(a.Float, b.Float));
        }
    }
}