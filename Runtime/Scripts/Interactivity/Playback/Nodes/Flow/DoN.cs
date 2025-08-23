using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowDoN : BehaviourEngineNode
    {
        private int _nTimes;
        private int _currentCount = 0;

        public FlowDoN(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            switch (socket)
            {
                case ConstStrings.RESET:
                    _currentCount = 0;
                    break;
                case ConstStrings.IN:
                    if (_currentCount < _nTimes)
                    {
                        _currentCount++;
                        Util.Log($"Incrementing currentCount to {_currentCount} for an output that can be run {_nTimes} times");
                        TryExecuteFlow(ConstStrings.OUT);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Socket {socket} is not a valid input on this DoN node!");
            }
        }

        public override bool ValidateValues(string socket)
        {
            return TryEvaluateValue(ConstStrings.N, out _nTimes);
        }

        public override IProperty GetOutputValue(string socket)
        {
            return new Property<int>(_currentCount);
        }
    }
}