using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeBoolToFloat : BehaviourEngineNode
    {
        public TypeBoolToFloat(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Bool)
                throw new InvalidOperationException("Value provided is not a bool! Will not cast to float.");

            return Variant.FromFloat(a.Bool ? 1f : 0f);
        }
    }
}