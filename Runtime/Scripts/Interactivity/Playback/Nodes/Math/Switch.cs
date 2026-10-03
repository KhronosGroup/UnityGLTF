using System.Collections.Generic;
using System.Globalization;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSwitch : BehaviourEngineNode
    {
        private readonly HashSet<int> _cases;
        /// <summary>Input index of each case socket, or -1 when the node has no such socket.</summary>
        private readonly Dictionary<int, int> _caseInputs = new();

        public MathSwitch(BehaviourEngine engine, Node node) : base(engine, node)
        {
            _cases = new HashSet<int>(GraphValidator.GetSwitchCases(node));

            foreach (var c in _cases)
                _caseInputs[c] = GetInputIndex(c.ToString(CultureInfo.InvariantCulture));
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.SELECTION, out int selection);

            // Only selections present in the "cases" configuration use their socket, even if
            // the node JSON has a socket for another number.
            if (_cases.Contains(selection) && TryEvaluateValue(_caseInputs[selection], out Variant value))
                return value;

            TryEvaluateValue(ConstStrings.DEFAULT, out Variant fallback);
            return fallback;
        }
    }
}
