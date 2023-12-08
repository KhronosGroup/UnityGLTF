using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Timeline
{
    internal class AnimationData
    {
        internal Transform transform;
        private SkinnedMeshRenderer smr;
        
        internal readonly bool recordBlendShapes;
        internal readonly bool inWorldSpace = false;
        internal readonly bool recordAnimationPointer;

        internal List<AnimationTrack> tracks = new List<AnimationTrack>();
        
        public AnimationData(
            Transform transform,
            double time,
            bool zeroScale = false,
            bool recordBlendShapes = true,
            bool inWorldSpace = false,
            bool recordAnimationPointer = false
        ) {
            this.transform = transform;
            this.smr = transform.GetComponent<SkinnedMeshRenderer>();
            this.recordBlendShapes = recordBlendShapes;
            this.inWorldSpace = inWorldSpace;
            this.recordAnimationPointer = recordAnimationPointer;
            
            foreach (var plan in AnimationSampler.getAllAnimationSamplers(recordBlendShapes, recordAnimationPointer)) {
                if (plan.GetTarget(transform)) {
                    tracks.Add(plan.StartNewAnimationTrackFor(this, time));
                }
            }
        }

        public void Update(double time) {
            foreach (var track in tracks) { track.SampleIfChanged(time); }
        }
    }
}