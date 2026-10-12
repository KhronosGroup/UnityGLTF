namespace UnityGLTF.Interactivity.Playback
{
    public class EventSend : BehaviourEngineNode
    {
        private readonly int _eventIndex = -1;
        /// <summary>Input index of each value the custom event defines, or -1 when the node has no such socket.</summary>
        private readonly int[] _valueInputs = System.Array.Empty<int>();

        public EventSend(BehaviourEngine engine, Node node) : base(engine, node)
        {
            TryGetConfig(ConstStrings.EVENT, out _eventIndex);

            if (_eventIndex < 0 || _eventIndex >= engine.graph.customEvents.Count)
                return;

            var definition = engine.graph.customEvents[_eventIndex].values;
            _valueInputs = new int[definition?.Count ?? 0];

            for (int i = 0; i < _valueInputs.Length; i++)
                _valueInputs[i] = GetInputIndex(definition[i].id);
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (_eventIndex < 0 || _eventIndex >= engine.graph.customEvents.Count)
            {
                TryExecuteFlow(ConstStrings.OUT);
                return;
            }

            // Only the sockets defined by the custom event are sent; extra input sockets are ignored.
            var count = _valueInputs.Length;

            engine.RentEventPayload(count, out var values, out var provided);

            try
            {
                for (int i = 0; i < count; i++)
                {
                    if (TryEvaluateValue(_valueInputs[i], out Variant value))
                    {
                        values[i] = value;
                        provided[i] = true;
                    }
                }

                Util.Log($"Sending event index {_eventIndex}");

                engine.FireCustomEvent(_eventIndex, values, provided);
            }
            finally
            {
                engine.ReturnEventPayload();
            }

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
