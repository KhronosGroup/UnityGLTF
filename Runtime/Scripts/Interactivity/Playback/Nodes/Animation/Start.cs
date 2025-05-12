using System.Threading;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class AnimationStart : BehaviourEngineNode
    {
        private int _animationIndex;
        private float _speed;
        private float _startTime;
        private float _endTime;

        public AnimationStart(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (validationResult != ValidationResult.Valid)
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            Util.Log($"Playing animation index {_animationIndex} with speed {_speed} and start/end times of {_startTime}/{_endTime}");

            var data = new AnimationPlayData()
            {
                index = _animationIndex,
                startTime = _startTime,
                endTime = _endTime,
                stopTime = _endTime,
                speed = _speed,
                unityStartTime = Time.time,
                endDone = () => TryExecuteFlow(ConstStrings.DONE)
            };

            engine.PlayAnimation(data);

            TryExecuteFlow(ConstStrings.OUT);
        }

        public override bool ValidateValues(string socket)
        {
            return TryEvaluateValue(ConstStrings.ANIMATION, out _animationIndex) &&
                TryEvaluateValue(ConstStrings.SPEED, out _speed) &&
                TryEvaluateValue(ConstStrings.START_TIME, out _startTime) &&
                TryEvaluateValue(ConstStrings.END_TIME, out _endTime) &&
                ValidateAnimationIndex(_animationIndex) &&
                ValidateStartAndEndTimes(_startTime, _endTime) &&
                ValidateSpeed(_speed);
        }

        private static bool ValidateSpeed(float speed)
        {
            return speed > 0 && !float.IsNaN(speed) && !float.IsInfinity(speed);
        }

        private bool ValidateAnimationIndex(int animationIndex)
        {
            if (!TryGetReadOnlyPointer($"/{Pointers.ANIMATIONS_LENGTH}", out ReadOnlyPointer<int> animPointer))
                return false;

            var animationCount = animPointer.GetValue();

            if (animationIndex < 0 || animationIndex >= animationCount)
                return false;

            return true;
        }

        private static bool ValidateStartAndEndTimes(float startTime, float endTime)
        {
            if (float.IsNaN(startTime) || float.IsNaN(endTime))
                return false;

            if (float.IsInfinity(startTime))
                return false;

            return true;
        }
    }
}