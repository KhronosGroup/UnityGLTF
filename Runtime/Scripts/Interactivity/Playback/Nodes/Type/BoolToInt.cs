using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeBoolToInt : BehaviourEngineNode
    {
        public TypeBoolToInt(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<bool> aProp)
                throw new InvalidOperationException("Value provided is not a bool! Will not cast to int.");

            return new Property<int>(aProp.value ? 1 : 0);
        }
    }
}