using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventSend : BehaviourEngineNode
    {
        private int _eventNum;
        private Dictionary<string, IProperty> _outValues;

        public EventSend(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            Util.Log($"Sending event index {_eventNum}");

            engine.FireCustomEvent(_eventNum, _outValues);

            TryExecuteFlow(ConstStrings.OUT);
        }

        public override bool ValidateConfiguration(string socket)
        {
            if (!TryGetConfig(ConstStrings.EVENT, out int eventNum))
                return false;

            _eventNum = eventNum;

            return true;
        }

        public override bool ValidateValues(string socket)
        {
            var outValues = new Dictionary<string, IProperty>();

            try
            {
                foreach (var v in values)
                {
                    outValues.Add(v.Key, engine.ParseValue(v.Value));
                }

                _outValues = outValues;
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