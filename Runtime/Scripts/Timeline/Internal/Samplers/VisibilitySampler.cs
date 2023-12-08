using UnityEngine;

namespace UnityGLTF.Timeline.Samplers
{
    internal class VisibilitySampler : AnimationSampler<GameObject, bool>
    {
        public override string propertyName => "visibility";
        public override AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time) => 
            new AnimationTrack<GameObject, bool>(data, this, time);
        internal AnimationTrack<GameObject, bool> startNewAnimationTrackAt(AnimationData data, double time) => 
            new AnimationTrack<GameObject, bool>(data, this, time);
        
        
        protected override GameObject getTarget(Transform transform) => transform.gameObject;

        protected override bool getValue(Transform transform, GameObject target, AnimationData data) =>
            target.activeSelf;
    }
}