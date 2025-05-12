using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeFloatToInt : BehaviourEngineNode
    {
        public TypeFloatToInt(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<float> intProperty)
                throw new InvalidOperationException("Value provided is not a float! Will not cast to int.");

            return new Property<int>((int)intProperty.value);
        }
    }
}