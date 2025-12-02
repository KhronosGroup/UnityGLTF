using System;
using System.Threading;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowBranch : BehaviourEngineNode
    {
        private bool _condition;

        public FlowBranch(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (socket != ConstStrings.IN)
                throw new ArgumentException($"Only valid input socket for this node is \"{ConstStrings.IN}\"");

            Util.Log($"Branch condition is {_condition}");

            var outSocket = _condition ? ConstStrings.TRUE : ConstStrings.FALSE;

            TryExecuteFlow(outSocket);
        }

        public override bool ValidateValues(string socket)
        {
            if (!TryEvaluateValue(ConstStrings.CONDITION, out bool condition))
                return false;

            _condition = condition;
            return true;
        }
    }
}