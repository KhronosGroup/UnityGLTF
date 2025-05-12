namespace UnityGLTF.Interactivity.Playback
{
    public class FlowWaitAll : BehaviourEngineNode
    {
        private readonly int _inputFlows;
        private readonly bool[] _activated;

        private int _remainingInputs;

        public FlowWaitAll(BehaviourEngine engine, Node node) : base(engine, node)
        {
            if (!TryGetConfig(ConstStrings.INPUT_FLOWS, out _inputFlows))
                _inputFlows = 0;

            _remainingInputs = _inputFlows;
            _activated = new bool[_inputFlows];
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (socket.Equals(ConstStrings.RESET))
            {
                _remainingInputs = _inputFlows;
                ResetBooleanArray(_activated);
                return;
            }

            var index = int.Parse(socket); // Throws if an unexpected socket id is passed in that isn't in the spec.

            if(!_activated[index])
                _remainingInputs--;
            _activated[index] = true;

            if (_remainingInputs == 0)
                TryExecuteFlow(ConstStrings.COMPLETED);
            else
                TryExecuteFlow(ConstStrings.OUT);
        }

        public override IProperty GetOutputValue(string socket)
        {
            return new Property<int>(_remainingInputs);
        }

        private static void ResetBooleanArray(bool[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = false;
            }
        }
    }
}