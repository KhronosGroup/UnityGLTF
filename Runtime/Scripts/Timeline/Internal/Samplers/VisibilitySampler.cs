using UnityEngine;

namespace UnityGLTF.Timeline.Samplers
{
    internal sealed class VisibilitySampler : AnimationSampler<GameObject, bool>
    {
        public override string PropertyName => "visibility";

        internal VisibilityTrack startNewAnimationTrackAtStartOfTime(AnimationData data, double time) =>
            new VisibilityTrack(data, this, time);

        protected override GameObject getTarget(Transform transform) => transform.gameObject;

        protected override bool getValue(Transform transform, GameObject target, AnimationData data) =>
            target.activeSelf;
    }

    internal sealed class VisibilityTrack : BaseAnimationTrack<GameObject, bool>
    {
        public VisibilityTrack(AnimationData tr, VisibilitySampler plan, double time) :
            base(tr, plan, time, objectVisibility => {
                var overridenVisibility = time <= 0 && objectVisibility;
                return overridenVisibility;
            }) { }

        internal void recordVisibilityAt(double time, bool visible) => recordSampleIfChanged(time, visible);
    }
}