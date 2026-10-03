using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventOnTick : BehaviourEngineNode
    {
        public EventOnTick(BehaviourEngine engine, Node node) : base(engine, node)
        {
            engine.onTick += OnTick;
        }

        private void OnTick()
        {
            if (engine.IsImmediatelyStopped(engine.tickEvent))
                return;

            TryExecuteFlow(ConstStrings.OUT);
        }

        public override Variant GetOutputValue(string id)
        {
            return id switch
            {
                // All onTick nodes share the engine's per-tick values, so every node reports the current tick
                // even when read by an earlier onTick node's flow before this node's own handler has run.
                ConstStrings.TIME_SINCE_START => Variant.FromFloat(engine.timeSinceStart),
                ConstStrings.TIME_SINCE_LAST_TICK => Variant.FromFloat(engine.timeSinceLastTick),
                ConstStrings.EVENT => Variant.FromRef(engine.tickEvent),
                _ => throw new InvalidOperationException($"No valid output with name {id}"),
            };
        }
    }
}
