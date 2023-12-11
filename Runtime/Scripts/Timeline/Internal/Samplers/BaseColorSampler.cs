using UnityEngine;

namespace UnityGLTF.Timeline.Samplers
{
    internal sealed class BaseColorSampler : AnimationSampler<Material, Color?>, SimpleAnimationSampler
    {
        private static readonly MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        
        public override string PropertyName => "weights";
        public AnimationTrack StartNewAnimationTrackAt(AnimationData data, double time) =>
            new AnimationTrack<Material, Color?>(data, this, time);
        
        protected override Material getTarget(Transform transform) => transform.GetComponent<MeshRenderer>()?.sharedMaterial;

        protected override Color? getValue(Transform transform, Material target, AnimationData data) {
            var r = transform.GetComponent<Renderer>();

            if (r.HasPropertyBlock()) {
                r.GetPropertyBlock(materialPropertyBlock);
                #if UNITY_2021_1_OR_NEWER
                if (materialPropertyBlock.HasColor("_BaseColor"))
                    return materialPropertyBlock.GetColor("_BaseColor").linear;
                if (materialPropertyBlock.HasColor("_Color"))
                    return materialPropertyBlock.GetColor("_Color").linear;
                if (materialPropertyBlock.HasColor("baseColorFactor"))
                    return materialPropertyBlock.GetColor("baseColorFactor").linear;
                #else
					var c = materialPropertyBlock.GetColor("_BaseColor");
					if (c.r != 0 || c.g != 0 || c.b != 0 || c.a != 0) return c;
					c = materialPropertyBlock.GetColor("_Color");
					if (c.r != 0 || c.g != 0 || c.b != 0 || c.a != 0) return c;
					// this leaves an edge case where someone is actually animating color to black:
					// in that case, the un-animated color would now be exported...
                #endif
            }

            if (target) {
                if (target.HasProperty("_BaseColor")) return target.GetColor("_BaseColor").linear;
                if (target.HasProperty("_Color")) return target.GetColor("_Color").linear;
                if (target.HasProperty("baseColorFactor")) return target.GetColor("baseColorFactor").linear;
            }
            return null;
        }
    }
}