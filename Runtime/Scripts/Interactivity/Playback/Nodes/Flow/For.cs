using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowFor : BehaviourEngineNode
    {
        private int _index;

        public FlowFor(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // Default configuration: initialIndex is zero.
            if (!TryGetConfig(ConstStrings.INITIAL_INDEX, out _index))
                _index = 0;
        }

        public override IProperty GetOutputValue(string socket)
        {
            if (socket == ConstStrings.INDEX)
                return new Property<int>(_index);

            throw new ArgumentException($"Socket {socket} is not valid on this node!");
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (!TryEvaluateValue(ConstStrings.START_INDEX, out int startIndex))
                return;

            _index = startIndex;

            // endIndex is re-evaluated before every iteration.
            while (TryEvaluateValue(ConstStrings.END_INDEX, out int endIndex) && _index < endIndex)
            {
                TryExecuteFlow(ConstStrings.LOOP_BODY);
                _index++;
                engine.NotifySelfActivation();
            }

            TryExecuteFlow(ConstStrings.COMPLETED);
        }
    }
}
