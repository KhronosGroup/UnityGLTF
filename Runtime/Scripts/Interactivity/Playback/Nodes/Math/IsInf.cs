using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathIsInf : BehaviourEngineNode
    {
        public MathIsInf(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Float)
                throw new InvalidOperationException("Property must be of type float for IsNan!");

            return Variant.FromBool(float.IsInfinity(a.Float));
        }
    }
}