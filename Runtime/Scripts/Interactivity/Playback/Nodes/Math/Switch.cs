using System.Collections.Generic;
using System.Globalization;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSwitch : BehaviourEngineNode
    {
        private readonly HashSet<int> _cases;

        public MathSwitch(BehaviourEngine engine, Node node) : base(engine, node)
        {
            _cases = new HashSet<int>(GraphValidator.GetSwitchCases(node));
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.SELECTION, out int selection);

            // Only selections present in the "cases" configuration use their socket, even if
            // the node JSON has a socket for another number.
            if (_cases.Contains(selection) && TryEvaluateValue(selection.ToString(CultureInfo.InvariantCulture), out IProperty value))
                return value;

            TryEvaluateValue(ConstStrings.DEFAULT, out IProperty fallback);
            return fallback;
        }
    }
}
