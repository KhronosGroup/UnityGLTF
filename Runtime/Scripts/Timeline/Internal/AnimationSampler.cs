using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF.Timeline.Samplers;
using Object = UnityEngine.Object;

namespace UnityGLTF.Timeline
{
    internal interface AnimationSampler
    {
        internal static readonly VisibilitySampler visibilitySampler = new VisibilitySampler();

        private static List<AnimationSampler> animationSamplers;

        public string propertyName { get; }
        
        object Sample(AnimationData data);

        public abstract AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time);

        public abstract Object GetTarget(Transform transform);
        
        internal static IReadOnlyList<AnimationSampler> getAllAnimationSamplers(bool recordBlendShapes, bool recordAnimationPointer) {
            if (animationSamplers == null) {
                animationSamplers = new List<AnimationSampler> {
                    new TranslationSampler(),
                    new RotationSampler(),
                    new ScaleSampler()
                };
                if (recordBlendShapes) {
                    animationSamplers.Add(new BlendWeightSampler());
                }

                if (recordAnimationPointer) {
                    // TODO add other animation pointer export plans
                    animationSamplers.Add(new BaseColorSampler());
                }
            }
            return animationSamplers;
        }
    }

    internal abstract class AnimationSampler<TObject, TData> : AnimationSampler
        where TObject : UnityEngine.Object
    {
        public abstract string propertyName { get; }
        public Type dataType => typeof(TData);

        public object Sample(AnimationData data) => sample(data);
        public abstract AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time);

        public Object GetTarget(Transform transform) => getTarget(transform);
        protected abstract TObject getTarget(Transform transform);
        protected abstract TData getValue(Transform transform, TObject target, AnimationData data);

        internal TData sample(AnimationData data) {
            var target = getTarget(data.transform);
            return getValue(data.transform, target, data);
        }
    }
}