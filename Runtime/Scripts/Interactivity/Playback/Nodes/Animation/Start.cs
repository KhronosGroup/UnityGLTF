namespace UnityGLTF.Interactivity.Playback
{
    public class AnimationStart : BehaviourEngineNode
    {
        private readonly System.Action _done;

        public AnimationStart(BehaviourEngine engine, Node node) : base(engine, node)
        {
            _done = () => TryExecuteFlow(ConstStrings.DONE);
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            // 1. Evaluate all input values.
            if (!TryEvaluateValue(ConstStrings.ANIMATION, out Ref animation) ||
                !TryEvaluateValue(ConstStrings.START_TIME, out float startTime) ||
                !TryEvaluateValue(ConstStrings.END_TIME, out float endTime) ||
                !TryEvaluateValue(ConstStrings.SPEED, out float speed))
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            // 2-5. Reference, times, speed.
            if (!engine.TryGetAnimationIndex(animation, out var index) ||
                float.IsNaN(startTime) || float.IsNaN(endTime) || float.IsInfinity(startTime) ||
                float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0)
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            Util.Log($"Playing animation index {index} with speed {speed} and start/end times of {startTime}/{endTime}");

            // 6-7. Replace any existing entry; stop time starts equal to end time with no stop completion.
            engine.PlayAnimation(new AnimationPlayData()
            {
                index = index,
                startTime = startTime,
                endTime = endTime,
                stopTime = endTime,
                speed = speed,
                unityStartTime = engine.time,
                endDone = _done,
                stopDone = null
            });

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
