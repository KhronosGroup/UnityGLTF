using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventOnTick : BehaviourEngineNode
    {
        private float _timeSinceStart = float.NaN;
        private float _timeSinceLastTick = float.NaN;
        private Ref _event = Ref.Null;

        public EventOnTick(BehaviourEngine engine, Node node) : base(engine, node)
        {
            engine.onTick += OnTick;
        }

        private void OnTick()
        {
            if (engine.IsImmediatelyStopped(engine.tickEvent))
                return;

            // All onTick nodes share the engine's per-tick values.
            _timeSinceStart = engine.timeSinceStart;
            _timeSinceLastTick = engine.timeSinceLastTick;
            _event = engine.tickEvent;

            TryExecuteFlow(ConstStrings.OUT);
        }

        public override IProperty GetOutputValue(string id)
        {
            return id switch
            {
                ConstStrings.TIME_SINCE_START => new Property<float>(_timeSinceStart),
                ConstStrings.TIME_SINCE_LAST_TICK => new Property<float>(_timeSinceLastTick),
                ConstStrings.EVENT => new Property<Ref>(_event),
                _ => throw new InvalidOperationException($"No valid output with name {id}"),
            };
        }
    }
}
