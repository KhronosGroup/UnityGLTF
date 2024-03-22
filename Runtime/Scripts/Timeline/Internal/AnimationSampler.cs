#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityGLTF.Timeline.Samplers;
using Object = UnityEngine.Object;

namespace UnityGLTF.Timeline
{
    
    internal sealed class AnimationSamplers
    {
        /// GLTF internally does not seem to support animated visibility state, but recording this explicitly makes a lot of things easier.
        /// The resulting animation track will be merged with the "scale" track of any animation while exporting, forcing the scale
        /// to (0,0,0) when the object is invisible.
        public VisibilitySampler? VisibilitySampler { get; private set; }

        /// all animation samplers that do not require special treatment - so currently all others, except visibility 
        private readonly Dictionary<Type, AnimationSampler> registeredAnimationSamplers =
            new Dictionary<Type, AnimationSampler>();

        public static AnimationSamplers From(
            Func<Transform, bool> useWorldSpaceForTransform,
            bool sampleVisibility,
            bool recordBlendShapes,
            bool recordAnimationPointer,
            IEnumerable<AnimationSampler>? additionalSamplers = null
        ) {
            var otherSamplers = new List<AnimationSampler>{
                new TranslationSampler(useWorldSpaceForTransform),
                new RotationSampler(useWorldSpaceForTransform),
                new ScaleSampler(useWorldSpaceForTransform)
            };
            if (recordBlendShapes) {
                otherSamplers.Add(new BlendWeightSampler());
            }
            if (recordAnimationPointer) {
                // TODO add other animation pointer export plans
                otherSamplers.Add(new BaseColorSampler());
            }
            if (additionalSamplers != null) {
                otherSamplers.AddRange(additionalSamplers);
            }
            return new AnimationSamplers(sampleVisibility, otherSamplers);
        }
        
        public AnimationSamplers(bool sampleVisibility, IEnumerable<AnimationSampler> otherSamplers) {
            VisibilitySampler = sampleVisibility ? new VisibilitySampler() : null;

            foreach (var sampler in otherSamplers) {
                registeredAnimationSamplers.TryAdd(sampler.GetType(), sampler);
            }
        }

        public IEnumerable<AnimationSampler> GetAdditionalAnimationSamplers() => registeredAnimationSamplers.Values;
    }
    
    internal interface AnimationSampler
    {
        public string PropertyName { get; }
        
        object? Sample(AnimationData data);

        public Object? GetTarget(Transform transform);
        
        public AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time);
    }
    
    internal abstract class AnimationSampler<TObject, TData> : AnimationSampler
        where TObject : UnityEngine.Object
    {
        public abstract string PropertyName { get; }
        public Type dataType => typeof(TData);

        public object? Sample(AnimationData data) => sample(data);

        public Object? GetTarget(Transform transform) => getTarget(transform);
        protected abstract TObject? getTarget(Transform transform);
        protected abstract TData? getValue(Transform transform, TObject target, AnimationData data);

        internal TData? sample(AnimationData data) {
            var target = getTarget(data.transform);
            return target != null ? getValue(data.transform, target, data) : default;
        }
        
        public AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time) =>
            new AnimationTrack<TObject, TData>(data, this, time);
    }

    internal sealed class CustomAnimationSamplerWrapper : AnimationSampler<Component, object?>
    {

        public override string PropertyName => customSampler.PropertyName;

        protected override Component? getTarget(Transform transform) => customSampler.GetTarget(transform);

        protected override object? getValue(Transform transform, Component target, AnimationData data) =>
            customSampler.GetValue(transform, target);

        private readonly CustomComponentAnimationSampler customSampler;
        public CustomAnimationSamplerWrapper(CustomComponentAnimationSampler customSampler) => this.customSampler = customSampler;

    }
}