using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSelect : BehaviourEngineNode
    {
        public MathSelect(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            var typeA = a.GetSystemType();
            var typeB = b.GetSystemType();

            if (typeA != typeB)
                throw new InvalidOperationException($"Select only accepts arguments of the same type. Type A: {typeA}, Type B: {typeB}");

            TryEvaluateValue(ConstStrings.CONDITION, out bool condition);

            return condition ? a : b;
        }
    }
}