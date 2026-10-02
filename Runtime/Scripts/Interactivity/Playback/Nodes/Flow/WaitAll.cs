using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowWaitAll : BehaviourEngineNode
    {
        private readonly int _inputFlows;
        private readonly bool[] _activated;

        private int _remainingInputs;

        public FlowWaitAll(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // Invalid, negative or >64 values use the default configuration (zero input flows).
            if (!TryGetConfig(ConstStrings.INPUT_FLOWS, out _inputFlows) || _inputFlows < 0 || _inputFlows > 64)
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

            // Ids are canonical decimal numbers below inputFlows; anything else is not an input flow of this node.
            if (!Ref.TryParseCanonicalIndex(socket.AsSpan(), out var index) || index >= _inputFlows)
                return;

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