using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventReceive : BehaviourEngineNode
    {
        private readonly int _eventToListenFor = -1;
        private readonly Dictionary<string, int> _valueIndices = new();
        private Variant[] _initialValues = Array.Empty<Variant>();
        private Variant[] _values = Array.Empty<Variant>();
        private Ref _event = Ref.Null;

        public EventReceive(BehaviourEngine engine, Node node) : base(engine, node)
        {
            engine.customEventFired += OnEventFired;

            if (!TryGetConfig(ConstStrings.EVENT, out _eventToListenFor))
                throw new InvalidOperationException("No event provided in the config to listen for.");

            var definition = engine.graph.customEvents[_eventToListenFor].values;
            var count = definition?.Count ?? 0;

            for (int i = 0; i < count; i++)
                _valueIndices[definition[i].id] = i;

            _initialValues = new Variant[count];
            _values = new Variant[count];
            ReadInitialValues();
        }

        public override void OnEngineReady()
        {
            // Static references in initial values are resolved when playback starts.
            ReadInitialValues();
        }

        private void ReadInitialValues()
        {
            var definition = engine.graph.customEvents[_eventToListenFor].values;

            for (int i = 0; i < _initialValues.Length; i++)
                _initialValues[i] = Variant.FromProperty(definition[i].property);

            Array.Copy(_initialValues, _values, _values.Length);
        }

        public override Variant GetOutputValue(string socket)
        {
            if (socket == ConstStrings.EVENT)
                return Variant.FromRef(_event);

            if (!_valueIndices.TryGetValue(socket, out var index))
                throw new ArgumentException($"No output value found for socket {socket}");

            return _values[index];
        }

        private void OnEventFired(int eventIndex, Variant[] values, bool[] provided)
        {
            if (eventIndex != _eventToListenFor || engine.IsImmediatelyStopped(engine.lastCustomEvent))
                return;

            Util.Log($"Received event {engine.graph.customEvents[eventIndex].id} with index {eventIndex}.");

            // Values not provided by this occurrence, or of the wrong type, are reset to their initial or type-default values.
            for (int i = 0; i < _values.Length; i++)
            {
                _values[i] = provided[i] && values[i].type == _initialValues[i].type ? values[i] : _initialValues[i];
            }

            _event = engine.lastCustomEvent;

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
