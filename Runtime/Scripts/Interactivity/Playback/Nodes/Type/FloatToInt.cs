using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeFloatToInt : BehaviourEngineNode
    {
        public TypeFloatToInt(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Float)
                throw new InvalidOperationException("Value provided is not a float! Will not cast to int.");

            return Variant.FromInt((int)a.Float);
        }
    }
}