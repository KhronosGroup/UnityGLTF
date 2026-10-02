using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventReceive : BehaviourEngineNode
    {
        private readonly Dictionary<string, IProperty> _outValues = new();
        private readonly int _eventToListenFor = -1;
        private Ref _event = Ref.Null;

        public EventReceive(BehaviourEngine engine, Node node) : base(engine, node)
        {
            engine.onCustomEventFired += OnEventFired;

            if (!TryGetConfig(ConstStrings.EVENT, out _eventToListenFor))
                throw new InvalidOperationException("No event provided in the config to listen for.");

            ResetToInitialValues();
        }

        public override IProperty GetOutputValue(string socket)
        {
            if (socket == ConstStrings.EVENT)
                return new Property<Ref>(_event);

            if (!_outValues.TryGetValue(socket, out IProperty outValue))
                throw new ArgumentException($"No output value found for socket {socket}");

            return outValue;
        }

        private void OnEventFired(int eventIndex, Dictionary<string, IProperty> values)
        {
            if (eventIndex != _eventToListenFor || engine.IsImmediatelyStopped(engine.lastCustomEvent))
                return;

            Util.Log($"Received event {engine.graph.customEvents[eventIndex].id} with index {eventIndex}.");

            // Values not provided by this occurrence are reset to their initial or type-default values.
            ResetToInitialValues();

            if (values != null)
            {
                foreach (var kvp in values)
                {
                    if (_outValues.TryGetValue(kvp.Key, out var current) && current.GetSystemType() == kvp.Value?.GetSystemType())
                        _outValues[kvp.Key] = kvp.Value;
                }
            }

            _event = engine.lastCustomEvent;

            TryExecuteFlow(ConstStrings.OUT);
        }

        private void ResetToInitialValues()
        {
            _outValues.Clear();

            var eventData = engine.graph.customEvents[_eventToListenFor];

            if (eventData.values == null)
                return;

            for (int i = 0; i < eventData.values.Count; i++)
            {
                _outValues[eventData.values[i].id] = eventData.values[i].property;
            }
        }
    }
}
