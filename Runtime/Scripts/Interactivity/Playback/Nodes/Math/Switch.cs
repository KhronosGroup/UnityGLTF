using System.Collections.Generic;
using System.Globalization;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSwitch : BehaviourEngineNode
    {
        private readonly HashSet<int> _cases;
        private readonly Dictionary<int, string> _caseSockets = new();

        public MathSwitch(BehaviourEngine engine, Node node) : base(engine, node)
        {
            _cases = new HashSet<int>(GraphValidator.GetSwitchCases(node));

            foreach (var c in _cases)
                _caseSockets[c] = c.ToString(CultureInfo.InvariantCulture);
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.SELECTION, out int selection);

            // Only selections present in the "cases" configuration use their socket, even if
            // the node JSON has a socket for another number.
            if (_cases.Contains(selection) && TryEvaluateValue(_caseSockets[selection], out Variant value))
                return value;

            TryEvaluateValue(ConstStrings.DEFAULT, out Variant fallback);
            return fallback;
        }
    }
}
