using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF.Timeline
{
    internal class ExportPlan
    {
        private static List<ExportPlan> exportPlans;
        private static MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        
        public string propertyName;
        public Type dataType;
        public Func<Transform, Object> GetTarget;
        public Func<Transform, Object, AnimationData, object> GetData;

        private ExportPlan(
            string propertyName,
            Type dataType,
            Func<Transform, Object> GetTarget,
            Func<Transform, Object, AnimationData, object> GetData
        ) {
            this.propertyName = propertyName;
            this.dataType = dataType;
            this.GetTarget = GetTarget;
            this.GetData = GetData;
        }

        internal static IReadOnlyList<ExportPlan> getExportPlans(bool recordBlendShapes, bool recordAnimationPointer) {
            if (exportPlans == null) {
                exportPlans = new List<ExportPlan> {
                    new(
                        "translation",
                        typeof(Vector3),
                        x => x,
                        (tr0, _, options) => options.inWorldSpace ? tr0.position : tr0.localPosition
                    ),
                    new(
                        "rotation",
                        typeof(Quaternion),
                        x => x,
                        (tr0, _, options) => {
                            var q = options.inWorldSpace ? tr0.rotation : tr0.localRotation;
                            return new Quaternion(q.x, q.y, q.z, q.w);
                        }
                    ),
                    new(
                        "scale",
                        typeof(Vector3),
                        x => x,
                        (tr0, _, options) => options.inWorldSpace ? tr0.lossyScale : tr0.localScale
                    )
                };

                if (recordBlendShapes) {
                    exportPlans.Add(
                        new ExportPlan(
                            "weights",
                            typeof(float[]),
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

                    exportPlans.Add(
                        new ExportPlan(
                            "baseColorFactor",
                            typeof(Color),
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
            return exportPlans;
        }
        
        public object Sample(AnimationData data) {
            var target = GetTarget(data.tr);
            return GetData(data.tr, target, data);
        }
    }

}