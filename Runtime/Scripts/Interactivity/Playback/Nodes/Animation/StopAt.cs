namespace UnityGLTF.Interactivity.Playback
{
    public class AnimationStopAt : BehaviourEngineNode
    {
        public AnimationStopAt(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (!TryEvaluateValue(ConstStrings.ANIMATION, out Ref animation) ||
                !TryEvaluateValue(ConstStrings.STOP_TIME, out float stopTime) ||
                !engine.TryGetAnimationIndex(animation, out var index) ||
                float.IsNaN(stopTime))
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            Util.Log($"Stopping animation index {index} at {stopTime}.");

            // Does nothing if the animation isn't playing; "out" still activates.
            engine.StopAnimationAt(index, stopTime, () => TryExecuteFlow(ConstStrings.DONE));

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
