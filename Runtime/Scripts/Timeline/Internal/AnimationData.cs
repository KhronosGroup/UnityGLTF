#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF.Timeline.Samplers;

namespace UnityGLTF.Timeline
{
    internal sealed class AnimationData
    {
        internal readonly Transform transform;
        
        /// GLTF natively does not support animated visibility - as a result it has to be merged with the scale track later on
        /// in the export process.
        /// At the same time visibility has a higher priority than the other tracks, since
        /// there is no point in animating properties of an invisible object.
        /// These requirements / constraints are easier to fulfill when we store the visibility track explicitly
        /// instead of putting it in the <see cref="tracks"/> field alongside the other tracks. 
        internal readonly VisibilityTrack? visibilityTrack;
        
        internal readonly List<AnimationTrack> tracks = new List<AnimationTrack>();
        
        public AnimationData(
            AnimationSamplers animationSamplers,
            Transform transform,
            double time
        ) {
            this.transform = transform;
            
            // the visibility track always starts at time = 0, inserting additional invisible samples at the start of the time if required
            visibilityTrack = animationSamplers.VisibilitySampler?.startNewAnimationTrackAtStartOfTime(this, time);
            if (visibilityTrack != null && time > 0) {
                // make sure to insert another sample right before the change so that the linear interpolation is very short, not from the start of time
                visibilityTrack.recordVisibilityAt(time-Double.Epsilon, visibilityTrack.lastValue);
                // if we are not at the start of time, add another visibility sample to the current time, where the object started to exist
                visibilityTrack.SampleIfChanged(time);
            }

            foreach (var plan in animationSamplers.GetAdditionalAnimationSamplers()) {
                if (plan.GetTarget(transform)) {
                    tracks.Add(plan.StartNewAnimationTrackAt(this, time));
                }
            }
        }

        public void Update(double time) {
            visibilityTrack?.SampleIfChanged(time);
            // if visibility is not being sampled, or the object is currently visible, sample the other tracks
            if (visibilityTrack == null || visibilityTrack.lastValue) {
                foreach (var track in tracks) {
                    track.SampleIfChanged(time);
                }    
            }
        }
    }
}