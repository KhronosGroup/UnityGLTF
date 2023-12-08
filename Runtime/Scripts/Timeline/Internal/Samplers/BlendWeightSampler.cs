using UnityEngine;

namespace UnityGLTF.Timeline.Samplers
{
    internal class BlendWeightSampler : AnimationSampler<SkinnedMeshRenderer, float[]>
    {
        public override string propertyName => "weights";
        public override AnimationTrack StartNewAnimationTrackFor(AnimationData data, double time) =>
            new AnimationTrack<SkinnedMeshRenderer, float[]>(data, this, time);
        
        protected override SkinnedMeshRenderer getTarget(Transform transform) => transform.GetComponent<SkinnedMeshRenderer>();

        protected override float[] getValue(Transform transform, SkinnedMeshRenderer target, AnimationData data) {
            if (target.sharedMesh) {
                var mesh = target.sharedMesh;
                var blendShapeCount = mesh.blendShapeCount;
                if (blendShapeCount == 0) return null;
                var weights = new float[blendShapeCount];
                for (var i = 0; i < blendShapeCount; i++)
                    weights[i] = target.GetBlendShapeWeight(i);
                return weights;
            }
            return null;
        }
    }
}