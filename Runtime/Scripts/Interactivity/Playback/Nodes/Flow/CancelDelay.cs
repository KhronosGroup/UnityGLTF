namespace UnityGLTF.Interactivity.Playback
{
    public class FlowCancelDelay : BehaviourEngineNode
    {
        public FlowCancelDelay(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            TryEvaluateValue(ConstStrings.DELAY_INDEX, out int index);

            engine.nodeDelayManager.CancelDelayByIndex(index);

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}