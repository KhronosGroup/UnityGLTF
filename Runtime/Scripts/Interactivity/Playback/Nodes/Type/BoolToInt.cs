using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeBoolToInt : BehaviourEngineNode
    {
        public TypeBoolToInt(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Bool)
                throw new InvalidOperationException("Value provided is not a bool! Will not cast to int.");

            return Variant.FromInt(a.Bool ? 1 : 0);
        }
    }
}