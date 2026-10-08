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

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);
            TryEvaluateValue(ConstStrings.C, out Variant c);
            TryEvaluateValue(ConstStrings.D, out Variant d);

            if (a.type != VariantType.Float)
                throw new InvalidOperationException("Input A is not a float!");

            if (b.type != VariantType.Float)
                throw new InvalidOperationException("Input B is not a float!");

            if (c.type != VariantType.Float)
                throw new InvalidOperationException("Input C is not a float!");

            if (d.type != VariantType.Float)
                throw new InvalidOperationException("Input D is not a float!");

            return Variant.FromFloat4(new float4(a.Float, b.Float, c.Float, d.Float));
        }
    }
}