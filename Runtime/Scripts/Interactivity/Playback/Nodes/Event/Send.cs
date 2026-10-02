using System.Collections.Generic;

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
            var definition = engine.graph.customEvents[_eventIndex];
            var outValues = new Dictionary<string, IProperty>(definition.values.Count);

            foreach (var v in definition.values)
            {
                if (TryEvaluateValue(v.id, out IProperty value))
                    outValues[v.id] = value;
            }

            Util.Log($"Sending event index {_eventIndex}");

            engine.FireCustomEvent(_eventIndex, outValues);

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
