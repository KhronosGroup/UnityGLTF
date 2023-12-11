using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF.Timeline.Samplers;
using Object = UnityEngine.Object;

namespace UnityGLTF.Timeline
{
    
    internal interface AnimationSampler
    {
        public string PropertyName { get; }
        
        object Sample(AnimationData data);

        public Object GetTarget(Transform transform);
    }
    
    internal interface SimpleAnimationSampler : AnimationSampler
    {
        public AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time);
    }

    internal static class AnimationSamplers
    {
        /// GLTF internally does not support animated visibility state, but recording this explicitly makes a lot of things easier.
        /// The resulting animation track will be merged with the "scale" track of any animation, forcing the scale to (0,0,0) when
        /// object is invisible.
        internal static readonly VisibilitySampler visibilitySampler = new VisibilitySampler();
        
        /// all animation samplers that do not require special treatment - so currently all others, except visibility 
        private static List<SimpleAnimationSampler> _animationSamplers;
        
        internal static IReadOnlyList<SimpleAnimationSampler> getAllAnimationSamplers(bool recordBlendShapes, bool recordAnimationPointer) {
            if (_animationSamplers == null) {
                _animationSamplers = new List<SimpleAnimationSampler> {
                    new TranslationSampler(),
                    new RotationSampler(),
                    new ScaleSampler()
                };
                if (recordBlendShapes) {
                    _animationSamplers.Add(new BlendWeightSampler());
                }

                if (recordAnimationPointer) {
                    // TODO add other animation pointer export plans
                    _animationSamplers.Add(new BaseColorSampler());
                }
            }
            return _animationSamplers;
        }
    }
    
    internal abstract class AnimationSampler<TObject, TData> : AnimationSampler
        where TObject : UnityEngine.Object
    {
        public abstract string PropertyName { get; }
        public Type dataType => typeof(TData);

        public object Sample(AnimationData data) => sample(data);

        public Object GetTarget(Transform transform) => getTarget(transform);
        protected abstract TObject getTarget(Transform transform);
        protected abstract TData getValue(Transform transform, TObject target, AnimationData data);

        internal TData sample(AnimationData data) {
            var target = getTarget(data.transform);
            return getValue(data.transform, target, data);
        }
    }
}