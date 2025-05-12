using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeBoolToFloat : BehaviourEngineNode
    {
        public TypeBoolToFloat(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<bool> aProp)
                throw new InvalidOperationException("Value provided is not a bool! Will not cast to float.");

            return new Property<float>(aProp.value ? 1f : 0f);
        }
    }
}