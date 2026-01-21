using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowSetDelay : BehaviourEngineNode
    {
        private float _duration;
        private int _lastDelayIndex = -1;

        public FlowSetDelay(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            switch (socket)
            {
                case ConstStrings.CANCEL:
                    engine.nodeDelayManager.CancelDelaysFromNode(this);
                    _lastDelayIndex = -1;
                    break;

                case ConstStrings.IN:
                    if (validationResult != ValidationResult.Valid)
                    { 
                        TryExecuteFlow(ConstStrings.ERR);
                        return;
                    }

                    Util.Log($"Executing delay with duration of {_duration}s");
                    _lastDelayIndex = engine.nodeDelayManager.AddDelayNode(this, _duration, () => TryExecuteFlow(ConstStrings.DONE));

                    TryExecuteFlow(ConstStrings.OUT);
                    break;

                default:
                    throw new InvalidOperationException($"Socket {socket} is not a valid input on this SetDelay node!");
            }
        }

        public override bool ValidateValues(string socket)
        {
            if (!TryEvaluateValue(ConstStrings.DURATION, out _duration))
                return false;

            if (double.IsNaN(_duration) || double.IsInfinity(_duration) || _duration < 0)
                return false;

            return true;
        }

        public override IProperty GetOutputValue(string socket)
        {
            return new Property<int>(_lastDelayIndex);
        }
    }
}