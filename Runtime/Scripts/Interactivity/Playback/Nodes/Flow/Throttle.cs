using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowThrottle : BehaviourEngineNode
    {
        private float _duration;
        private float _timestamp;
        private float _elapsed;
        private float _lastRemainingTime = float.NaN;

        public FlowThrottle(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (socket.Equals(ConstStrings.RESET))
            {
                _lastRemainingTime = float.NaN;
                return;
            }

            TryEvaluateValue(ConstStrings.DURATION, out _duration);

            if (_duration < 0 || float.IsNaN(_duration) || float.IsInfinity(_duration))
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            if (float.IsNaN(_lastRemainingTime))
            {
                ExecuteOutFlow();
                return;
            }

            _elapsed = Time.time - _timestamp;

            if (_duration <= _elapsed)
            {
                ExecuteOutFlow();
                return;
            }
            
            _lastRemainingTime = _duration - _elapsed;
        }

        private void ExecuteOutFlow()
        {
            _timestamp = Time.time;
            _lastRemainingTime = 0;
            TryExecuteFlow(ConstStrings.OUT);
        }

        public override IProperty GetOutputValue(string socket)
        {
            return new Property<float>(_lastRemainingTime);
        }
    }
}