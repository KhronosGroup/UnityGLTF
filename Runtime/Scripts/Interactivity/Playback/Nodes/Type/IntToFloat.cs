using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeIntToFloat : BehaviourEngineNode
    {
        public TypeIntToFloat(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<int> intProperty)
                throw new InvalidOperationException("Value provided is not an int! Will not cast to float.");

            return new Property<float>(intProperty.value);
        }
    }
}