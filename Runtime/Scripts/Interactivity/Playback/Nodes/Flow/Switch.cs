using System.Collections.Generic;
using System.Globalization;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowSwitch : BehaviourEngineNode
    {
        /// <summary>Output flow socket ids generated from the "cases" configuration.</summary>
        private readonly HashSet<int> _cases;
        private readonly Dictionary<int, string> _caseSockets = new();

        public FlowSwitch(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // An absent or invalid "cases" configuration means the default configuration: only "default".
            _cases = new HashSet<int>(GraphValidator.GetSwitchCases(node));

            foreach (var c in _cases)
                _caseSockets[c] = c.ToString(CultureInfo.InvariantCulture);
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (!TryEvaluateValue(ConstStrings.SELECTION, out int selection) || !_cases.Contains(selection))
            {
                TryExecuteFlow(ConstStrings.DEFAULT);
                return;
            }

            // A case present in the configuration activates its socket, if connected, and never "default".
            TryExecuteFlow(_caseSockets[selection]);
        }
    }
}
