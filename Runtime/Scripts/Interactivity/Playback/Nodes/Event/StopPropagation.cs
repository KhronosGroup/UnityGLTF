namespace UnityGLTF.Interactivity.Playback
{
    public class EventStopPropagation : BehaviourEngineNode
    {
        public EventStopPropagation(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            // Invalid event references simply activate "out".
            if (TryEvaluateValue(ConstStrings.EVENT, out Ref eventRef) &&
                TryEvaluateValue(ConstStrings.STOP_IMMEDIATE, out bool stopImmediate))
            {
                engine.StopPropagation(eventRef, stopImmediate);
            }

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
