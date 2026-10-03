using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeFloatToBool : BehaviourEngineNode
    {
        public TypeFloatToBool(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Float)
                throw new InvalidOperationException("Value provided is not a float! Will not cast to bool.");

            return Variant.FromBool(a.Float != float.NaN && a.Float != 0);
        }
    }
}