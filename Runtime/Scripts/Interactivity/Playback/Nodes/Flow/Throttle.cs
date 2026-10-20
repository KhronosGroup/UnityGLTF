using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowThrottle : BehaviourEngineNode
    {
        private float _duration;
        private double _timestamp;
        private double _elapsed;
        private float _lastRemainingTime = float.NaN;

        public FlowThrottle(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override bool HasInputFlow(string socket)
        {
            return socket == ConstStrings.IN || socket == ConstStrings.RESET;
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

            _elapsed = engine.time - _timestamp;

            if (_duration <= _elapsed)
            {
                ExecuteOutFlow();
                return;
            }
            
            _lastRemainingTime = (float)(_duration - _elapsed);
        }

        private void ExecuteOutFlow()
        {
            _timestamp = engine.time;
            _lastRemainingTime = 0;
            TryExecuteFlow(ConstStrings.OUT);
        }

        public override Variant GetOutputValue(string socket)
        {
            return Variant.FromFloat(_lastRemainingTime);
        }
    }
}