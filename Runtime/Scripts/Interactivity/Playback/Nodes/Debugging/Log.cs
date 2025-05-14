using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class DebugLog : BehaviourEngineNode
    {
        private readonly int _severity;
        private readonly string _message;
        private static readonly Regex _variableRegex = new("{(.*?)}");

        public DebugLog(BehaviourEngine engine, Node node) : base(engine, node)
        {
            if (!TryGetConfig(ConstStrings.MESSAGE, out _message))
                _message = "";

            if (!TryGetConfig(ConstStrings.SEVERITY, out _severity))
                _severity = 0;
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            var formatted = FormatString(_message);

            switch (_severity)
            {
                case 0:
                    Debug.LogWarning(formatted);
                    break;

                case 1:
                    Debug.LogError(formatted);
                    break;

                default:
                    Debug.Log(formatted);
                    break;
            }

            TryExecuteFlow(ConstStrings.OUT);
        }

        private string FormatString(string str)
        {
            var matches = _variableRegex.Matches(str);

            foreach (Match match in matches)
            {
                if (!TryEvaluateValue(match.Groups[1].Value, out IProperty value))
                    continue;

                str = str.Replace(match.Value, value.ToString());
            }

            return str;
        }
    }
}