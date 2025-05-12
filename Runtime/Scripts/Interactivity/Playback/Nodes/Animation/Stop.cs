using System.Threading;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class AnimationStop : BehaviourEngineNode
    {
        private int _animationIndex;

        public AnimationStop(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (validationResult != ValidationResult.Valid)
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            Util.Log($"Stopping animation index {_animationIndex}.");

            engine.StopAnimation(_animationIndex);

            TryExecuteFlow(ConstStrings.OUT);
        }

        public override bool ValidateValues(string socket)
        {
            return TryEvaluateValue(ConstStrings.ANIMATION, out _animationIndex) &&
                ValidateAnimationIndex(_animationIndex);
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