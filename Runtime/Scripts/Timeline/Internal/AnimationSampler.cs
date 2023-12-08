using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF.Timeline
{
    internal interface AnimationSampler
    {
        private static List<AnimationSampler> animationSamplers;
        private static MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

        public string propertyName { get; }
        
        public Func<Transform, Object> GetTarget { get; }

        object Sample(AnimationData data);

        public abstract AnimationTrack StartNewAnimationTrackFor(AnimationData data, double time);
        
        internal static IReadOnlyList<AnimationSampler> getAllAnimationSamplers(bool recordBlendShapes, bool recordAnimationPointer) {
            if (animationSamplers == null) {
                animationSamplers = new List<AnimationSampler> {
                    new AnimationSampler<Vector3>(
                        "translation",
                        x => x,
                        (tr0, _, options) => options.inWorldSpace ? tr0.position : tr0.localPosition
                    ),
                    new AnimationSampler<Quaternion>(
                        "rotation",
                        x => x,
                        (tr0, _, options) => {
                            var q = options.inWorldSpace ? tr0.rotation : tr0.localRotation;
                            return new Quaternion(q.x, q.y, q.z, q.w);
                        }
                    ),
                    new AnimationSampler<Vector3>(
                        "scale",
                        x => x,
                        (tr0, _, options) => options.inWorldSpace ? tr0.lossyScale : tr0.localScale
                    )
                };

                if (recordBlendShapes) {
                    animationSamplers.Add(
                        new AnimationSampler<float[]>(
                            "weights",
                            x => x.GetComponent<SkinnedMeshRenderer>(),
                            (tr0, x, options) => {
                                if (x is SkinnedMeshRenderer skinnedMesh && skinnedMesh.sharedMesh) {
                                    var mesh = skinnedMesh.sharedMesh;
                                    var blendShapeCount = mesh.blendShapeCount;
                                    if (blendShapeCount == 0) return null;
                                    var weights = new float[blendShapeCount];
                                    for (var i = 0; i < blendShapeCount; i++)
                                        weights[i] = skinnedMesh.GetBlendShapeWeight(i);
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
                        new AnimationSampler<Color?>(
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

                                if (mat is Material m && m) {
                                    if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor").linear;
                                    if (m.HasProperty("_Color")) return m.GetColor("_Color").linear;
                                    if (m.HasProperty("baseColorFactor")) return m.GetColor("baseColorFactor").linear;
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

    internal class AnimationSampler<T> : AnimationSampler
    {
        
        public string propertyName { get; }
        public Type dataType => typeof(T);
        public Func<Transform, Object> GetTarget { get; }
        public Func<Transform, Object, AnimationData, T> GetData;

        internal AnimationSampler(
            string propertyName,
            Func<Transform, Object> GetTarget,
            Func<Transform, Object, AnimationData, T> GetData
        ) {
            this.propertyName = propertyName;
            this.GetTarget = GetTarget;
            this.GetData = GetData;
        }

        public object Sample(AnimationData data) => sample(data);
        public AnimationTrack StartNewAnimationTrackFor(AnimationData data, double time) => 
            new AnimationTrack<T>( data, this, time);

        internal T sample(AnimationData data) {
            var target = GetTarget(data.transform);
            return GetData(data.transform, target, data);
        }
    }

}