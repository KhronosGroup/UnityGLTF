using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowSwitch : BehaviourEngineNode
    {
        private Dictionary<int, Flow> _cases;
        private int _selection;

        public FlowSwitch(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (!_cases.TryGetValue(_selection, out Flow caseFlow))
            {
                Util.Log($"Executing case {ConstStrings.DEFAULT}");
                TryExecuteFlow(ConstStrings.DEFAULT);
                return;
            }

            Util.Log($"Executing case {_selection}");
            engine.ExecuteFlow(caseFlow);
        }

        public override bool ValidateValues(string socket)
        {
            if (!TryEvaluateValue(ConstStrings.SELECTION, out _selection))
                return false;

            return true;
        }

        public override bool ValidateConfiguration(string socket)
        {
            try
            {
                if (_cases == null)
                {
                    _cases = new();

                    for (int i = 0; i < node.flows.Count; i++)
                    {
                        if (node.flows[i].fromSocket.Equals(ConstStrings.DEFAULT))
                            continue;

                        _cases.Add(int.Parse(node.flows[i].fromSocket), node.flows[i]);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
    }
}