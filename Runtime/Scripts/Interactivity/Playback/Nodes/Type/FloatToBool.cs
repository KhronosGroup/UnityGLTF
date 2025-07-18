using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeFloatToBool : BehaviourEngineNode
    {
        public TypeFloatToBool(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<float> aProp)
                throw new InvalidOperationException("Value provided is not a float! Will not cast to bool.");

            return new Property<bool>(aProp.value != float.NaN && aProp.value != 0);
        }
    }
}