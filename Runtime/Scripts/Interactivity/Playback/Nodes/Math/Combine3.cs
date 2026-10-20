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

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);
            TryEvaluateValue(ConstStrings.C, out Variant c);

            if (a.type != VariantType.Float)
                throw new InvalidOperationException("Input A is not a float!");

            if (b.type != VariantType.Float)
                throw new InvalidOperationException("Input B is not a float!");

            if (c.type != VariantType.Float)
                throw new InvalidOperationException("Input C is not a float!");

            return Variant.FromFloat3(new float3(a.Float, b.Float, c.Float));
        }
    }
}