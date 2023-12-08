using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Timeline
{
    internal class AnimationData
    {
        internal Transform tr;
        private SkinnedMeshRenderer smr;
        
        internal readonly bool recordBlendShapes;
        internal readonly bool inWorldSpace = false;
        internal readonly bool recordAnimationPointer;

        internal List<Track> tracks = new List<Track>();
        
        public AnimationData(
            Transform tr,
            double time,
            bool zeroScale = false,
            bool recordBlendShapes = true,
            bool inWorldSpace = false,
            bool recordAnimationPointer = false
        ) {
            this.tr = tr;
            this.smr = tr.GetComponent<SkinnedMeshRenderer>();
            this.recordBlendShapes = recordBlendShapes;
            this.inWorldSpace = inWorldSpace;
            this.recordAnimationPointer = recordAnimationPointer;
            
            foreach (var plan in ExportPlan.getExportPlans(recordBlendShapes, recordAnimationPointer)) {
                if (plan.GetTarget(tr)) { tracks.Add(new Track(this, plan, time)); }
            }
        }

        public void Update(double time) {
            foreach (var track in tracks) { track.SampleIfChanged(time); }
        }
    }
}