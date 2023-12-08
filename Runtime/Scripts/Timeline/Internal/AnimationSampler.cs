using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF.Timeline
{
    internal interface AnimationSampler
    {
        private static List<AnimationSampler> animationSamplers;
        private static readonly MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

        public string propertyName { get; }
        
        object Sample(AnimationData data);

        public abstract AnimationTrack StartNewAnimationTrackFor(AnimationData data, double time);

        public abstract Object GetTarget(Transform transform);
        
        internal static IReadOnlyList<AnimationSampler> getAllAnimationSamplers(bool recordBlendShapes, bool recordAnimationPointer) {
            if (animationSamplers == null) {
                animationSamplers = new List<AnimationSampler> {
                    new AnimationSamplerImpl<Transform, Vector3>(
                        "translation",
                        x => x,
                        (tr0, _, options) => options.inWorldSpace ? tr0.position : tr0.localPosition
                    ),
                    new AnimationSamplerImpl<Transform,Quaternion>(
                        "rotation",
                        x => x,
                        (tr0, _, options) => {
                            var q = options.inWorldSpace ? tr0.rotation : tr0.localRotation;
                            return new Quaternion(q.x, q.y, q.z, q.w);
                        }
                    ),
                    new AnimationSamplerImpl<Transform,Vector3>(
                        "scale",
                        x => x,
                        (tr0, _, options) => options.inWorldSpace ? tr0.lossyScale : tr0.localScale
                    )
                };

                if (recordBlendShapes) {
                    animationSamplers.Add(
                        new AnimationSamplerImpl<SkinnedMeshRenderer, float[]>(
                            "weights",
                            x => x.GetComponent<SkinnedMeshRenderer>(),
                            (tr0, x, options) => {
                                if (x.sharedMesh) {
                                    var mesh = x.sharedMesh;
                                    var blendShapeCount = mesh.blendShapeCount;
                                    if (blendShapeCount == 0) return null;
                                    var weights = new float[blendShapeCount];
                                    for (var i = 0; i < blendShapeCount; i++)
                                        weights[i] = x.GetBlendShapeWeight(i);
                                    return weights;
                                }

                                return null;
                            }
                        )
                    );
                }

                if (recordAnimationPointer) {
                    // TODO add other animation pointer export plans

                    animationSamplers.Add(
                        new AnimationSamplerImpl<Material, Color?>(
                            "baseColorFactor",
                            x => x.GetComponent<MeshRenderer>() ? x.GetComponent<MeshRenderer>().sharedMaterial : null,
                            (tr0, mat, options) => {
                                var r = tr0.GetComponent<Renderer>();

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

                                if (mat) {
                                    if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor").linear;
                                    if (mat.HasProperty("_Color")) return mat.GetColor("_Color").linear;
                                    if (mat.HasProperty("baseColorFactor")) return mat.GetColor("baseColorFactor").linear;
                                }

                                return null;
                            }
                        )
                    );
                }
            }
            return animationSamplers;
        }
    }

    internal abstract class AnimationSampler<TObject, TData> : AnimationSampler
        where TObject : UnityEngine.Object
    {
        public string propertyName { get; }
        public Type dataType => typeof(TData);

        internal AnimationSampler(
            string propertyName
        ) {
            this.propertyName = propertyName;
        }

        public object Sample(AnimationData data) => sample(data);
        public abstract AnimationTrack StartNewAnimationTrackFor(AnimationData data, double time);

        public Object GetTarget(Transform transform) => getTarget(transform);
        protected abstract TObject getTarget(Transform transform);
        protected abstract TData getValue(Transform transform, TObject target, AnimationData data);

        internal TData sample(AnimationData data) {
            var target = getTarget(data.transform);
            return getValue(data.transform, target, data);
        }
    }
    
    internal class AnimationSamplerImpl<TObject, TData> : AnimationSampler<TObject, TData> where TObject : Object
    {
        public Func<Transform, TObject> GetTargetFunc { get; }
        public Func<Transform, TObject, AnimationData, TData> GetDataFunc { get; }
     
        internal AnimationSamplerImpl(
            string propertyName,
            Func<Transform, TObject> GetTarget,
            Func<Transform, TObject, AnimationData, TData> GetData
        ) : base(propertyName) {
            this.GetTargetFunc = GetTarget;
            this.GetDataFunc = GetData;
        }

        public override AnimationTrack StartNewAnimationTrackFor(AnimationData data, double time) => 
            new AnimationTrack<TObject, TData>( data, this, time);
        
        protected override TObject getTarget(Transform transform) => GetTargetFunc(transform);

        protected override TData getValue(Transform transform, TObject target, AnimationData data) =>
            GetDataFunc(transform, target, data);
    }

}