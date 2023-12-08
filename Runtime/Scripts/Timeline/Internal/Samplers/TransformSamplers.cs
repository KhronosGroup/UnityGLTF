using UnityEngine;

namespace UnityGLTF.Timeline.Samplers
{
    internal class TranslationSampler : AnimationSampler<Transform, Vector3>
    {
        public override string propertyName => "translation";
        public override AnimationTrack StartNewAnimationTrackFor(AnimationData data, double time) => new AnimationTrack<Transform, Vector3>(data, this, time);
        protected override Transform getTarget(Transform transform) => transform;

        protected override Vector3 getValue(Transform transform, Transform target, AnimationData data) =>
            data.inWorldSpace ? transform.position : transform.localPosition;
    }
    
    internal class RotationSampler : AnimationSampler<Transform, Quaternion>
    {
        public override string propertyName => "rotation";
        public override AnimationTrack StartNewAnimationTrackFor(AnimationData data, double time) => new AnimationTrack<Transform, Quaternion>(data, this, time);
        protected override Transform getTarget(Transform transform) => transform;
        protected override Quaternion getValue(Transform transform, Transform target, AnimationData data) =>
            data.inWorldSpace ? transform.rotation : transform.localRotation;
    }
    
    internal class ScaleSampler : AnimationSampler<Transform, Vector3>
    {
        public override string propertyName => "scale";
        public override AnimationTrack StartNewAnimationTrackFor(AnimationData data, double time) => new AnimationTrack<Transform, Vector3>(data, this, time);
        protected override Transform getTarget(Transform transform) => transform;
        protected override Vector3 getValue(Transform transform, Transform target, AnimationData data) =>
            data.inWorldSpace ? transform.lossyScale : transform.localScale;
    }
}