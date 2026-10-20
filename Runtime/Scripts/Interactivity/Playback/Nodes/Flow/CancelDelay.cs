namespace UnityGLTF.Interactivity.Playback
{
    public class FlowCancelDelay : BehaviourEngineNode
    {
        public FlowCancelDelay(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            // Null or invalid delay references must not cause runtime errors.
            if (TryEvaluateValue(ConstStrings.DELAY, out Ref delay))
                engine.nodeDelayManager.CancelDelay(delay);

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
