using UnityEngine;

namespace UnityGLTF.Timeline.Samplers
{
    internal class VisibilitySampler : AnimationSampler<GameObject, bool>
    {
        public override string propertyName => "visibility";
        public override AnimationTrack StartNewAnimationTrackFor(AnimationData data, double time) => new VisibilityTrack(data, this, time);
        protected override GameObject getTarget(Transform transform) => transform.gameObject;

        protected override bool getValue(Transform transform, GameObject target, AnimationData data) =>
            target.activeSelf;
    }

    // TODO: this feels kinda redundant now
    internal class VisibilityTrack : AnimationTrack<GameObject, bool>
    {
        public VisibilityTrack(AnimationData tr, AnimationSampler<GameObject, bool> plan, double time) : base(tr, plan, time) { }
    }
}