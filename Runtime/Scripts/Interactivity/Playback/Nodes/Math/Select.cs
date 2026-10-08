using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSelect : BehaviourEngineNode
    {
        public MathSelect(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            var typeA = a.GetSystemType();
            var typeB = b.GetSystemType();

            if (typeA != typeB)
                throw new InvalidOperationException($"Select only accepts arguments of the same type. Type A: {typeA}, Type B: {typeB}");

            TryEvaluateValue(ConstStrings.CONDITION, out bool condition);

            return condition ? a : b;
        }
    }
}