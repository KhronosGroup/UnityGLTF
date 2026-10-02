namespace UnityGLTF.Interactivity.Playback
{
    public class AnimationStop : BehaviourEngineNode
    {
        public AnimationStop(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (!TryEvaluateValue(ConstStrings.ANIMATION, out Ref animation) || !engine.TryGetAnimationIndex(animation, out var index))
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            Util.Log($"Stopping animation index {index}.");

            engine.StopAnimation(index);

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
