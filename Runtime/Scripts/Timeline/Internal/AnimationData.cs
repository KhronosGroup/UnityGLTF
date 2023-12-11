using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF.Timeline.Samplers;

namespace UnityGLTF.Timeline
{
    internal sealed class AnimationData
    {
        internal readonly Transform transform;
        
        internal readonly bool recordBlendShapes;
        internal readonly bool inWorldSpace = false;
        internal readonly bool recordAnimationPointer;

        internal readonly VisibilityTrack visibilityTrack;
        
        internal readonly List<AnimationTrack> tracks = new List<AnimationTrack>();
        
        public AnimationData(
            Transform transform,
            double time,
            bool recordBlendShapes = true,
            bool inWorldSpace = false,
            bool recordAnimationPointer = false
        ) {
            this.transform = transform;
            this.recordBlendShapes = recordBlendShapes;
            this.inWorldSpace = inWorldSpace;
            this.recordAnimationPointer = recordAnimationPointer;
            
            // the visibility track always starts at time = 0, inserting additional invisible samples at the start of the time if required
            visibilityTrack = AnimationSamplers.visibilitySampler.startNewAnimationTrackAtStartOfTime(this, time);
            if (time > 0) {
                // if we are not at the start of time, add another visibility sample to the current time, where the object started to exist
                visibilityTrack.SampleIfChanged(time);
            }

            foreach (var plan in AnimationSamplers.getAllAnimationSamplers(recordBlendShapes, recordAnimationPointer)) {
                if (plan.GetTarget(transform)) {
                    tracks.Add(plan.StartNewAnimationTrackAt(this, time));
                }
            }
        }

        public void Update(double time) {
            visibilityTrack.SampleIfChanged(time);
            // the object is currently visible, sample the other tracks
            if (visibilityTrack.lastValue) {
                foreach (var track in tracks) {
                    track.SampleIfChanged(time);
                }    
            }
        }
    }
}