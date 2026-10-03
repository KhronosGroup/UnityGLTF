using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeIntToFloat : BehaviourEngineNode
    {
        public TypeIntToFloat(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Int)
                throw new InvalidOperationException("Value provided is not an int! Will not cast to float.");

            return Variant.FromFloat(a.Int);
        }
    }
}