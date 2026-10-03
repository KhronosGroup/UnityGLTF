using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class DebugLog : BehaviourEngineNode
    {
        public readonly struct MessageParameter
        {
            public readonly string id;
            /// <summary>Inclusive range of the parameter, including its braces, in the message template.</summary>
            public readonly int start;
            public readonly int end;

            public MessageParameter(string id, int start, int end)
            {
                this.id = id;
                this.start = start;
                this.end = end;
            }
        }

        private readonly int _severity;
        private readonly string _message;
        private readonly List<MessageParameter> _parameters;

        public DebugLog(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // Invalid or missing configuration falls back to the default configuration: severity 0, empty message.
            if (!TryGetConfig(ConstStrings.SEVERITY, out _severity))
                _severity = 0;

            if (!TryGetConfig(ConstStrings.MESSAGE, out _message) || !TryParseMessageTemplate(_message, out _parameters))
            {
                _message = string.Empty;
                _parameters = new List<MessageParameter>();
            }
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            var formatted = FormatMessage();

            switch (_severity)
            {
                case 0:
                    Debug.Log(formatted);
                    break;

                case 1:
                    Debug.LogWarning(formatted);
                    break;

                default:
                    Debug.LogError(formatted);
                    break;
            }

            TryExecuteFlow(ConstStrings.OUT);
        }

        private string FormatMessage()
        {
            // Evaluate all input values first, then substitute in descending order of location.
            var values = new string[_parameters.Count];

            for (int i = 0; i < _parameters.Count; i++)
            {
                values[i] = TryEvaluateValue(_parameters[i].id, out Variant value)
                    ? (value.ToString() ?? string.Empty).Replace("{", "{{").Replace("}", "}}")
                    : string.Empty;
            }

            var sb = new StringBuilder(_message);

            for (int i = _parameters.Count - 1; i >= 0; i--)
            {
                var p = _parameters[i];
                sb.Remove(p.start, p.end - p.start + 1);
                sb.Insert(p.start, values[i]);
            }

            return sb.Replace("{{", "{").Replace("}}", "}").ToString();
        }

        /// <summary>
        /// The message template procedure from the debug/log specification. Returns false for invalid templates.
        /// Parameters are returned in ascending order of location; the same id may appear more than once.
        /// </summary>
        public static bool TryParseMessageTemplate(string message, out List<MessageParameter> parameters)
        {
            parameters = new List<MessageParameter>();

            if (message == null)
                return false;

            var state = 0;
            var paramStart = 0;

            for (int i = 0; i < message.Length; i++)
            {
                var c = message[i];

                if (c == '{')
                {
                    if (state == 0) state = 1;
                    else if (state == 1) state = 0;
                    else return false;
                }
                else if (c == '}')
                {
                    if (state == 0) state = 3;
                    else if (state == 3) state = 0;
                    else if (state == 2)
                    {
                        parameters.Add(new MessageParameter(message.Substring(paramStart + 1, i - paramStart - 1), paramStart, i));
                        state = 0;
                    }
                    else return false;
                }
                else
                {
                    if (state == 1)
                    {
                        paramStart = i - 1;
                        state = 2;
                    }
                    else if (state == 3)
                    {
                        return false;
                    }
                }
            }

            return state == 0;
        }
    }
}
