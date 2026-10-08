using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeIntToBool : BehaviourEngineNode
    {
        public TypeIntToBool(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Int)
                throw new InvalidOperationException("Value provided is not an int! Will not cast to bool.");

            return Variant.FromBool(a.Int != 0);
        }
    }
}