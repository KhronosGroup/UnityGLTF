using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathIsNaN : BehaviourEngineNode
    {
        public MathIsNaN(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<float> fProp)
                throw new InvalidOperationException("Property must be of type float for IsNan!");

            return new Property<bool>(float.IsNaN(fProp.value));
        }
    }
}