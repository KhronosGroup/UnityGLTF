using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventOnStart : BehaviourEngineNode
    {
        public EventOnStart(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // Subscribed in node order, so multiple onStart nodes activate in JSON order.
            engine.onStart += OnStart;
        }

        private void OnStart()
        {
            if (engine.IsImmediatelyStopped(engine.startEvent))
                return;

            TryExecuteFlow(ConstStrings.OUT);
        }

        public override Variant GetOutputValue(string id)
        {
            return id switch
            {
                // Read from the engine rather than copied per node: every onStart node outputs the current occurrence,
                // even when read by an earlier onStart node's flow before this node's own handler has run.
                ConstStrings.EVENT => Variant.FromRef(engine.startEvent),
                _ => throw new InvalidOperationException($"No valid output with name {id}"),
            };
        }
    }
}
