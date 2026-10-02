using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>One entry of the spec's "animation state dynamic array".</summary>
    public struct AnimationPlayData
    {
        public int index;
        public float startTime;
        public float endTime;
        public float stopTime;
        public float speed;
        /// <summary>Entry creation timestamp, in engine time.</summary>
        public double unityStartTime;
        public Action endDone;
        public Action stopDone;
    }

    public class AnimationData
    {
        public float playhead;
        public float virtualPlayhead;
        public AnimationState anim;

        public AnimationData(AnimationState anim)
        {
            this.anim = anim;
        }
    }

    public class GLTFInteractivityAnimationWrapper : MonoBehaviour
    {
        public Animation animationComponent { get; private set; }

        private readonly Dictionary<int, AnimationPlayData> _animationsInProgress = new();
        private AnimationData[] _animations;

        private BehaviourEngine _engine;

        public int animationCount => _animations?.Length ?? 0;

        public void SetData(BehaviourEngine behaviourEngine, Animation animationComponent)
        {
            if (_engine != null)
                _engine.onAnimationUpdate -= OnTick;

            _engine = behaviourEngine;
            _engine.onAnimationUpdate += OnTick;
            this.animationComponent = animationComponent;

            // Graph-controlled animations must not play automatically.
            animationComponent.playAutomatically = false;
            animationComponent.Stop();

            var clipCount = animationComponent.GetClipCount();
            _animations = new AnimationData[clipCount];

            var j = 0;

            foreach (AnimationState state in animationComponent)
            {
                state.speed = 0f;
                state.enabled = false;
                _animations[j++] = new AnimationData(state);
            }
        }

        public bool IsValidAnimationIndex(int index)
        {
            return _animations != null && index >= 0 && index < _animations.Length;
        }

        private void OnTick()
        {
            // Avoiding iterating over a changing collection by grabbing a pooled list.
            var temp = ListPool<AnimationPlayData>.Get();
            try
            {
                foreach (var anim in _animationsInProgress)
                {
                    temp.Add(anim.Value);
                }

                foreach (var anim in temp)
                {
                    // An earlier "done" flow may have restarted or stopped this animation.
                    if (!_animationsInProgress.TryGetValue(anim.index, out var current) || current.unityStartTime != anim.unityStartTime)
                        continue;

                    SampleAnimation(current);
                }
            }
            finally
            {
                ListPool<AnimationPlayData>.Release(temp);
            }
        }

        // Follows the "On each asset animation update" steps of animation/start.
        private void SampleAnimation(AnimationPlayData a)
        {
            float r;
            var T = _animations[a.index].anim.length;

            if (a.startTime == a.endTime)
            {
                CompleteAnimation(a.startTime, a.endDone);
                return;
            }

            var scaledElapsedTime = (float)((_engine.time - a.unityStartTime) * a.speed);

            if (a.startTime > a.endTime)
                scaledElapsedTime *= -1;

            r = scaledElapsedTime + a.startTime;

            var c1 = a.startTime < a.endTime && r >= a.stopTime && a.stopTime >= a.startTime && a.stopTime < a.endTime;
            var c2 = a.startTime > a.endTime && r <= a.stopTime && a.stopTime <= a.startTime && a.stopTime > a.endTime;

            if (c1 || c2)
            {
                Util.Log($"Stopping Animation {a.index}.");
                CompleteAnimation(a.stopTime, a.stopDone);
                return;
            }

            var c3 = a.startTime < a.endTime && r >= a.endTime;
            var c4 = a.startTime > a.endTime && r <= a.endTime;

            if (c3 || c4)
            {
                Util.Log($"Done Animation {a.index}.");
                CompleteAnimation(a.endTime, a.endDone);
                return;
            }

            SampleAnimationAtTime(r);

            float GetTimeStamp(float requested)
            {
                if (T == 0)
                    return 0;

                var s = requested > 0 ? Mathf.Ceil((requested - T) / T) : Mathf.Floor(requested / T);
                return requested - s * T;
            }

            void SampleAnimationAtTime(float requested)
            {
                var t = GetTimeStamp(requested);
                var data = _animations[a.index];
                data.playhead = t;
                data.virtualPlayhead = requested;
                data.anim.enabled = true;
                data.anim.weight = 1f;
                data.anim.time = t;
                animationComponent.Sample();
            }

            void CompleteAnimation(float requested, Action callback)
            {
                SampleAnimationAtTime(requested);
                // Remove the entry before activating "done" so the flow can restart the animation.
                StopAnimation(a.index);
                callback?.Invoke();
            }
        }

        public void PlayAnimation(in AnimationPlayData data)
        {
            // Replacing an entry must not activate the previous entry's done flows.
            _animationsInProgress.Remove(data.index);
            _animationsInProgress.Add(data.index, data);
        }

        /// <summary>Schedules stopping; does nothing if the animation is not playing.</summary>
        internal void StopAnimationAt(int animationIndex, float stopTime, Action callback)
        {
            if (!_animationsInProgress.TryGetValue(animationIndex, out var anim))
                return;

            anim.stopTime = stopTime;
            anim.stopDone = callback;

            _animationsInProgress[animationIndex] = anim;
        }

        /// <summary>Stops immediately; animated properties keep their current values.</summary>
        internal void StopAnimation(int index)
        {
            _animationsInProgress.Remove(index);

            if (IsValidAnimationIndex(index))
                _animations[index].anim.enabled = false;
        }

        public bool IsAnimationPlaying(int index)
        {
            return _animationsInProgress.ContainsKey(index);
        }

        public float GetAnimationMaxTime(int index)
        {
            return _animations[index].anim.length;
        }

        public float GetPlayhead(int index)
        {
            return _animations[index].playhead;
        }

        public float GetVirtualPlayhead(int index)
        {
            return _animations[index].virtualPlayhead;
        }
    }
}
