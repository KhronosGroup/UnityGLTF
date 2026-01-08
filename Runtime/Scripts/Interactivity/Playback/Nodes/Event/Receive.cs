using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventReceive : BehaviourEngineNode
    {
        private Dictionary<string, IProperty> _outValues = new();

        private int _eventToListenFor;

        public EventReceive(BehaviourEngine engine, Node node) : base(engine, node)
        {
            engine.onCustomEventFired += OnEventFired;

            // TODO: Putting things here is helpful for performance as we're caching them but does make runtime editing fail.
            // Figure out if we want to support runtime editing or if we just want to rebuild the whole graph when the user plays back the interaction.
            if (!TryGetConfig(ConstStrings.EVENT, out _eventToListenFor))
                throw new InvalidOperationException("No event provided in the config to listen for.");

            AddDefaultValues();
        }

        public override IProperty GetOutputValue(string socket)
        {
            if (!_outValues.TryGetValue(socket, out IProperty outValue))
                throw new ArgumentException($"No output value found for socket {socket}");

            return outValue;
        }

        private void OnEventFired(int eventIndex, Dictionary<string, IProperty> outValues)
        {
            if (eventIndex != _eventToListenFor)
                return;

            Util.Log($"Received event {engine.graph.customEvents[eventIndex].id} with id {eventIndex}.");
            _outValues = outValues;

            TryExecuteFlow(ConstStrings.OUT);
        }

        private void AddDefaultValues()
        {
            var eventData = engine.graph.customEvents[_eventToListenFor];

            if (eventData.values == null)
                return;

            EventValue value;

            for (int i = 0; i < eventData.values.Count; i++)
            {
                value = eventData.values[i];

                _outValues.Add(value.id, value.property);
            }
        }
    }
}