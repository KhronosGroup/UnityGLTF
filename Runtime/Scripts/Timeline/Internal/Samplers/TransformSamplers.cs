using System;
using UnityEngine;

namespace UnityGLTF.Timeline.Samplers
{
    internal sealed class TranslationSampler : AnimationSampler<Transform, Vector3>
    {
        private readonly Func<Transform, bool> sampleInWorldSpace;
        public TranslationSampler(Func<Transform, bool> sampleInWorldSpace) => this.sampleInWorldSpace = sampleInWorldSpace;

        public override string PropertyName => "translation";
        protected override Transform getTarget(Transform transform) => transform;

        protected override Vector3 getValue(Transform transform, Transform target, AnimationData data) =>
            sampleInWorldSpace(transform) ? transform.position : transform.localPosition;
    }
    
    internal sealed class RotationSampler : AnimationSampler<Transform, Quaternion>
    {
        private readonly Func<Transform, bool> sampleInWorldSpace;
        public RotationSampler(Func<Transform, bool> sampleInWorldSpace) => this.sampleInWorldSpace = sampleInWorldSpace;
        public override string PropertyName => "rotation";
        protected override Transform getTarget(Transform transform) => transform;
        protected override Quaternion getValue(Transform transform, Transform target, AnimationData data) =>
            sampleInWorldSpace(transform) ? transform.rotation : transform.localRotation;
    }
    
    internal sealed class ScaleSampler : AnimationSampler<Transform, Vector3>
    {
        private readonly Func<Transform, bool> sampleInWorldSpace;
        public ScaleSampler(Func<Transform, bool> sampleInWorldSpace) => this.sampleInWorldSpace = sampleInWorldSpace;
        
        public override string PropertyName => "scale";
        protected override Transform getTarget(Transform transform) => transform;
        protected override Vector3 getValue(Transform transform, Transform target, AnimationData data) =>
            sampleInWorldSpace(transform) ? transform.lossyScale : transform.localScale;
    }
}