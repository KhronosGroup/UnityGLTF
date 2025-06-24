using System;
using System.Threading;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowFor : BehaviourEngineNode
    {
        private int _startIndex;
        private int _endIndex;
        private int _index;

        public FlowFor(BehaviourEngine engine, Node node) : base(engine, node)
        {
            if (!configuration.TryGetValue(ConstStrings.INITIAL_INDEX, out Configuration config))
                return;

            _index = ((Property<int>)config.property).value;
        }

        public override IProperty GetOutputValue(string socket)
        {
            if (socket == ConstStrings.INDEX)
                return new Property<int>(_index);

            throw new ArgumentException($"Socket {socket} is not valid on this node!");
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            Util.Log($"Starting a loop with start index {_startIndex} and end index {_endIndex} from initial value {_index}");

            _index = _startIndex;

            while(_index < _endIndex)
            {
                TryExecuteFlow(ConstStrings.LOOP_BODY);
                _index++;
            }

            TryExecuteFlow(ConstStrings.COMPLETED);
        }

        public override bool ValidateValues(string socket)
        {
            if (!TryEvaluateValue(ConstStrings.START_INDEX, out _startIndex) ||
            !TryEvaluateValue(ConstStrings.END_INDEX, out _endIndex))
                return false;

            return true;
        }
    }
}