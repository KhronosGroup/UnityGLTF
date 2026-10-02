using System.Collections.Generic;
using System.Globalization;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowSwitch : BehaviourEngineNode
    {
        /// <summary>Output flow socket ids generated from the "cases" configuration.</summary>
        private readonly HashSet<int> _cases;

        public FlowSwitch(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // An absent or invalid "cases" configuration means the default configuration: only "default".
            _cases = new HashSet<int>(GraphValidator.GetSwitchCases(node));
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (!TryEvaluateValue(ConstStrings.SELECTION, out int selection) || !_cases.Contains(selection))
            {
                TryExecuteFlow(ConstStrings.DEFAULT);
                return;
            }

            // A case present in the configuration activates its socket, if connected, and never "default".
            TryExecuteFlow(selection.ToString(CultureInfo.InvariantCulture));
        }
    }
}
