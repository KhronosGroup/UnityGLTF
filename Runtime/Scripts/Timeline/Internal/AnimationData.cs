using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF.Timeline
{
    internal class AnimationData
    {
        internal Transform tr;
        private SkinnedMeshRenderer smr;
        private bool recordBlendShapes;
        private bool inWorldSpace = false;
        private bool recordAnimationPointer;

        private static List<ExportPlan> exportPlans;
        private static MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        internal List<Track> tracks = new List<Track>();

        internal class ExportPlan
        {
            public string propertyName;
            public Type dataType;
            public Func<Transform, Object> GetTarget;
            public Func<Transform, Object, AnimationData, object> GetData;

            public ExportPlan(
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

            public object Sample(AnimationData data) {
                var target = GetTarget(data.tr);
                return GetData(data.tr, target, data);
            }
        }

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

            if (exportPlans == null) {
                exportPlans = new List<ExportPlan>();
                exportPlans.Add(
                    new ExportPlan(
                        "translation",
                        typeof(Vector3),
                        x => x,
                        (tr0, _, options) => options.inWorldSpace ? tr0.position : tr0.localPosition
                    )
                );
                exportPlans.Add(
                    new ExportPlan(
                        "rotation",
                        typeof(Quaternion),
                        x => x,
                        (tr0, _, options) => {
                            var q = options.inWorldSpace ? tr0.rotation : tr0.localRotation;
                            return new Quaternion(q.x, q.y, q.z, q.w);
                        }
                    )
                );
                exportPlans.Add(
                    new ExportPlan(
                        "scale",
                        typeof(Vector3),
                        x => x,
                        (tr0, _, options) => options.inWorldSpace ? tr0.lossyScale : tr0.localScale
                    )
                );

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

            foreach (var plan in exportPlans) {
                if (plan.GetTarget(tr)) { tracks.Add(new Track(this, plan, time)); }
            }
        }

        internal class Track
        {
            public Object animatedObject => plan.GetTarget(tr.tr);

            public string propertyName => plan.propertyName;

            // TODO sample as floats?
            public float[] times => samples.Keys.Select(x => (float)x).ToArray();
            public object[] values => samples.Values.ToArray();

            private AnimationData tr;
            private ExportPlan plan;
            private Dictionary<double, object> samples;
            private Tuple<double, object> lastSample = null;
            private Tuple<double, object> secondToLastSample = null;

            public Track(AnimationData tr, ExportPlan plan, double time) {
                this.tr = tr;
                this.plan = plan;
                samples = new Dictionary<double, object>();
                SampleIfChanged(time);
            }

            public void SampleIfChanged(double time) {
                var value = plan.Sample(tr);
                if (value == null || (value is Object o && !o)) return;
                // As a memory optimization we want to be able to skip identical samples.
                // But, we cannot always skip samples when they are identical to the previous one - otherwise cases like this break:
                // - First assume an object is invisible at first (by having a scale of (0,0,0))
                // - At some point in time, it is instantaneously set "visible" by updating its scale from (0,0,0) to (1,1,1)
                // If we simply skip identical samples on insert, instead of a instantaneous
                // visibility/scale changes we get a linearly interpolated scale change because only two samples will be recorded:
                // - one (0,0,0) at the start of time
                // - (1,1,1) at the time of the visibility change
                // What we want to get is
                // - one sample with (0,0,0) at the start,
                // - one with the same value right before the instantaneous change,
                // - and then at the time of the change, we need a sample with (1,1,1)
                // With this setup, now the linear interpolation only has an effect in the very short duration between the last two samples and we get the animation we want.

                // How do we achieve both?
                // Always sample & record and then on adding the next sample(s) we check
                // if the *last two* samples were identical to the current sample.
                // If that is the case we can remove/overwrite the middle sample with the new value.
                if (lastSample != null
                    && secondToLastSample != null
                    && lastSample.Item2.Equals(secondToLastSample.Item2)
                    && lastSample.Item2.Equals(value)) { samples.Remove(lastSample.Item1); }

                samples[time] = value;
                secondToLastSample = lastSample;
                lastSample = new Tuple<double, object>(time, value);
            }

        }

        public void Update(double time) {
            foreach (var track in tracks) { track.SampleIfChanged(time); }
        }
    }
}