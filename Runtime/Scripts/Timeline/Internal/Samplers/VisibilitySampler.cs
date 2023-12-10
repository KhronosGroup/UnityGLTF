using UnityEngine;

namespace UnityGLTF.Timeline.Samplers
{
    internal class VisibilitySampler : AnimationSampler<GameObject, bool>
    {
        public override string propertyName => "visibility";

        public override AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time) =>
            new AnimationTrack<GameObject, bool>(data, this, time);

        internal VisibilityTrack startNewAnimationTrackAtStartOfTime(AnimationData data, double time) =>
            new VisibilityTrack(data, this, time);

        protected override GameObject getTarget(Transform transform) => transform.gameObject;

        protected override bool getValue(Transform transform, GameObject target, AnimationData data) =>
            target.activeSelf;
    }

    internal sealed class VisibilityTrack : BaseAnimationTrack<GameObject, bool>
    {
        public VisibilityTrack(AnimationData tr, AnimationSampler<GameObject, bool> plan, double time) :
            base(tr, plan, time, actualVisibility => {
                var res = time <= 0 && actualVisibility;
                return res;
            }) { }
    }
}