using System.Threading;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class AnimationStopAt : BehaviourEngineNode
    {
        private int _animationIndex;
        private float _stopTime;

        public AnimationStopAt(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (validationResult != ValidationResult.Valid)
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            Util.Log($"Stopping animation index {_animationIndex} at {_stopTime}.");

            engine.StopAnimationAt(_animationIndex, _stopTime, () => TryExecuteFlow(ConstStrings.DONE));

            TryExecuteFlow(ConstStrings.OUT);
        }

        public override bool ValidateValues(string socket)
        {
            return TryEvaluateValue(ConstStrings.ANIMATION, out _animationIndex) &&
                TryEvaluateValue(ConstStrings.STOP_TIME, out _stopTime) &&
                ValidateAnimationIndex(_animationIndex) &&
                ValidateStopTime(_stopTime);
        }

        private bool ValidateStopTime(float stopTime)
        {
            return !float.IsNaN(stopTime);
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
    }
}