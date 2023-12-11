using UnityEngine;

namespace UnityGLTF.Timeline.Samplers
{
    internal sealed class TranslationSampler : AnimationSampler<Transform, Vector3>, SimpleAnimationSampler
    {
        public override string PropertyName => "translation";
        public AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time) => new AnimationTrack<Transform, Vector3>(data, this, time);
        protected override Transform getTarget(Transform transform) => transform;

        protected override Vector3 getValue(Transform transform, Transform target, AnimationData data) =>
            data.inWorldSpace ? transform.position : transform.localPosition;
    }
    
    internal sealed class RotationSampler : AnimationSampler<Transform, Quaternion>, SimpleAnimationSampler
    {
        public override string PropertyName => "rotation";
        public AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time) => new AnimationTrack<Transform, Quaternion>(data, this, time);
        protected override Transform getTarget(Transform transform) => transform;
        protected override Quaternion getValue(Transform transform, Transform target, AnimationData data) =>
            data.inWorldSpace ? transform.rotation : transform.localRotation;
    }
    
    internal sealed class ScaleSampler : AnimationSampler<Transform, Vector3>, SimpleAnimationSampler
    {
        public override string PropertyName => "scale";
        public AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time) => new AnimationTrack<Transform, Vector3>(data, this, time);
        protected override Transform getTarget(Transform transform) => transform;
        protected override Vector3 getValue(Transform transform, Transform target, AnimationData data) =>
            data.inWorldSpace ? transform.lossyScale : transform.localScale;
    }
}