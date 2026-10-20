using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowWhile : BehaviourEngineNode
    {
        public FlowWhile(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (socket != ConstStrings.IN)
                throw new ArgumentException($"Only condition input socket for this node is \"{ConstStrings.IN}\"");

            while (TryEvaluateValue(ConstStrings.CONDITION, out bool condition) && condition)
            {
                TryExecuteFlow(ConstStrings.LOOP_BODY);
                // Self-activation of "in": retained output values must be recomputed.
                engine.NotifySelfActivation();
            }

            TryExecuteFlow(ConstStrings.COMPLETED);
        }
    }
}
