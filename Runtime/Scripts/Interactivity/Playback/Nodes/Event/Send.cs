namespace UnityGLTF.Interactivity.Playback
{
    public class EventSend : BehaviourEngineNode
    {
        private readonly int _eventIndex = -1;

        public EventSend(BehaviourEngine engine, Node node) : base(engine, node)
        {
            TryGetConfig(ConstStrings.EVENT, out _eventIndex);
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (_eventIndex < 0 || _eventIndex >= engine.graph.customEvents.Count)
            {
                TryExecuteFlow(ConstStrings.OUT);
                return;
            }

            // Only the sockets defined by the custom event are sent; extra input sockets are ignored.
            var definition = engine.graph.customEvents[_eventIndex].values;
            var count = definition?.Count ?? 0;

            engine.RentEventPayload(count, out var values, out var provided);

            try
            {
                for (int i = 0; i < count; i++)
                {
                    if (TryEvaluateValue(definition[i].id, out Variant value))
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
