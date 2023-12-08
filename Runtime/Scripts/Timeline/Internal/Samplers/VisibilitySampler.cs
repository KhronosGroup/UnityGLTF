using UnityEngine;

namespace UnityGLTF.Timeline.Samplers
{
    internal class VisibilitySampler : AnimationSampler<GameObject, bool>
    {
        public override string propertyName => "visibility";
        public override AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time) => 
            new AnimationTrack<GameObject, bool>(data, this, time);
        internal VisibilityTrack startNewAnimationTrackAtStartOfTime(AnimationData data, bool initialVisibility) => 
            new VisibilityTrack(data, this,  initialVisibility);
        
        
        protected override GameObject getTarget(Transform transform) => transform.gameObject;

        protected override bool getValue(Transform transform, GameObject target, AnimationData data) =>
            target.activeSelf;
    }
    
    internal sealed class VisibilityTrack : BaseAnimationTrack<GameObject, bool>
    {
        public VisibilityTrack(AnimationData tr, AnimationSampler<GameObject, bool> plan, bool initialVisibility) : base(tr, plan, 0, initialVisibility) { }
    }
}