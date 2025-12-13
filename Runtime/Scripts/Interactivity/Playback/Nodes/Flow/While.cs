using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowWhile : BehaviourEngineNode
    {
        private bool _condition;

        public FlowWhile(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (socket != ConstStrings.IN)
                throw new ArgumentException($"Only condition input socket for this node is \"{ConstStrings.IN}\"");

            while (_condition)
            {
                TryExecuteFlow(ConstStrings.LOOP_BODY);
                TryEvaluateValue(ConstStrings.CONDITION, out _condition);
            }

            TryExecuteFlow(ConstStrings.COMPLETED);
        }

        public override bool ValidateValues(string socket)
        {
            return TryEvaluateValue(ConstStrings.CONDITION, out _condition);
        }
    }
}