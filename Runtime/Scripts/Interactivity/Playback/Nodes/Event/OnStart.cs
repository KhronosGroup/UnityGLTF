using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventOnStart : BehaviourEngineNode
    {
        private Ref _event = Ref.Null;

        public EventOnStart(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // Subscribed in node order, so multiple onStart nodes activate in JSON order.
            engine.onStart += OnStart;
        }

        private void OnStart()
        {
            if (engine.IsImmediatelyStopped(engine.startEvent))
                return;

            _event = engine.startEvent;
            TryExecuteFlow(ConstStrings.OUT);
        }

        public override Variant GetOutputValue(string id)
        {
            return id switch
            {
                ConstStrings.EVENT => Variant.FromRef(_event),
                _ => throw new InvalidOperationException($"No valid output with name {id}"),
            };
        }
    }
}
