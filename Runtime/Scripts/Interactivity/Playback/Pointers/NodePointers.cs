using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public struct NodePointers
    {
        public Pointer<float3> translation;
        public Pointer<quaternion> rotation;
        public Pointer<float3> scale;
        public Pointer<bool> visibility;
        public Pointer<bool> selectability;
        public Pointer<bool> hoverability;
        public Pointer<float4x4> matrix;
        public Pointer<float4x4> globalMatrix;
        public ReadOnlyPointer<int> weightsLength;
        public Pointer<float>[] weights;
        public GameObject gameObject;

        public NodePointers(in NodeData data)
        {
            var go = data.unityObject;
            var schema = data.node;
            gameObject = go;

            // Unity coordinate system differs from the GLTF one.
            // Unity is left-handed with y-up and z-forward.
            // GLTF is right-handed with y-up and z-forward.
            // Handedness is easiest to swap here though we could do it during deserialization for performance.
            translation = new Pointer<float3>()
            {
                setter = (v) => go.transform.localPosition = v.SwapHandedness(),
                getter = () => go.transform.localPosition.SwapHandedness(),
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };

            rotation = new Pointer<quaternion>()
            {
                setter = (v) => go.transform.localRotation = ((Quaternion)v).SwapHandedness(),
                getter = () => go.transform.localRotation.SwapHandedness(),
                evaluator = (a, b, t) => math.slerp(a, b, t)
            };

            scale = new Pointer<float3>()
            {
                setter = (v) => go.transform.localScale = v,
                getter = () => go.transform.localScale,
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };

            matrix = new Pointer<float4x4>()
            {
                setter = (v) => go.transform.SetWorldMatrix(v, worldSpace: false, rightHanded: true),
                getter = () => go.transform.GetWorldMatrix(worldSpace: false, rightHanded: true),
                evaluator = (a, b, t) => a.LerpToComponentwise(b, t) // Spec has floatNxN lerp componentwise.
            };

            globalMatrix = new Pointer<float4x4>()
            {
                setter = (v) => go.transform.SetWorldMatrix(v, worldSpace: true, rightHanded: true),
                getter = () => go.transform.GetWorldMatrix(worldSpace: true, rightHanded: true),
                evaluator = (a, b, t) => a.LerpToComponentwise(b, t) // Spec has floatNxN lerp componentwise.
            };

            // TODO: Handle visibility pointers better? Do we report the value back to the extension?
            // Should we make the extension handle the SetActive call so we just change the value of visibility?
            visibility = new Pointer<bool>()
            {
                setter = (v) => go.SetActive(v),
                getter = () => go.activeSelf,
                evaluator = null
            };

            selectability = GetSelectabilityPointers(schema);
            hoverability = GetHoverabilityPointers(schema);

            if(go.TryGetComponent(out SkinnedMeshRenderer smr))
            {
                weightsLength = new ReadOnlyPointer<int>(() => smr.sharedMesh.blendShapeCount);
                weights = new Pointer<float>[smr.sharedMesh.blendShapeCount];

                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] = new Pointer<float>()
                    {
                        setter = (v) => smr.SetBlendShapeWeight(i, v),
                        getter = () => smr.GetBlendShapeWeight(i),
                        evaluator = (a, b, t) => math.lerp(a, b, t)
                    };
                }
            }
            else
            {
                weightsLength = default;
                weights = default;
            }
        }

        private static Pointer<bool> GetSelectabilityPointers(GLTF.Schema.Node schema)
        {
            Pointer<bool> selectability;

            if (schema.Extensions != null && schema.Extensions.TryGetValue(GLTF.Schema.KHR_node_selectability_Factory.EXTENSION_NAME, out var extension))
            {
                var selectabilityExtension = extension as GLTF.Schema.KHR_node_selectability;

                selectability = new Pointer<bool>()
                {
                    setter = (v) => selectabilityExtension.selectable = v,
                    getter = () => selectabilityExtension.selectable,
                    evaluator = null
                };
            }
            else
            {
                selectability = new Pointer<bool>()
                {
                    setter = (v) => { },
                    getter = () => true,
                    evaluator = null
                };
            }

            return selectability;
        }

        private static Pointer<bool> GetHoverabilityPointers(GLTF.Schema.Node schema)
        {
            Pointer<bool> hoverability;

            if (schema.Extensions != null && schema.Extensions.TryGetValue(GLTF.Schema.KHR_node_hoverability_Factory.EXTENSION_NAME, out var extension))
            {
                var hoverabilityExtension = extension as GLTF.Schema.KHR_node_hoverability;

                hoverability = new Pointer<bool>()
                {
                    setter = (v) => hoverabilityExtension.hoverable = v,
                    getter = () => hoverabilityExtension.hoverable,
                    evaluator = null
                };
            }
            else
            {
                hoverability = new Pointer<bool>()
                {
                    setter = (v) => { },
                    getter = () => true,
                    evaluator = null
                };
            }

            return hoverability;
        }

        public static IPointer ProcessNodePointer(StringSpanReader reader, BehaviourEngineNode engineNode, List<NodePointers> pointers)
        {
            reader.AdvanceToNextToken('/');

            var nodeIndex = PointerResolver.GetIndexFromArgument(reader, engineNode);

            var nodePointer = pointers[nodeIndex];

            reader.AdvanceToNextToken('/');

            // Path so far: /nodes/{}/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is(Pointers.TRANSLATION) => nodePointer.translation,
                var a when a.Is(Pointers.ROTATION) => nodePointer.rotation,
                var a when a.Is(Pointers.SCALE) => nodePointer.scale,
                var a when a.Is(Pointers.WEIGHTS) => ProcessWeightsPointer(reader, engineNode, nodePointer),
                var a when a.Is(Pointers.WEIGHTS_LENGTH) => nodePointer.weightsLength,
                var a when a.Is(Pointers.EXTENSIONS) => ProcessExtensionPointer(reader, nodePointer),
                var a when a.Is(Pointers.MATRIX) => nodePointer.matrix,
                var a when a.Is(Pointers.GLOBAL_MATRIX) => nodePointer.globalMatrix,
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        private static IPointer ProcessExtensionPointer(StringSpanReader reader, NodePointers nodePointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /nodes/{}/extensions/
            return reader.AsReadOnlySpan() switch
            {
                // TODO: Handle these properly via extensions in UnityGLTF?
                var a when a.Is(GLTF.Schema.KHR_node_selectability_Factory.EXTENSION_NAME) => nodePointer.selectability,
                var a when a.Is(GLTF.Schema.KHR_node_visibility_Factory.EXTENSION_NAME) => nodePointer.visibility,
                var a when a.Is(GLTF.Schema.KHR_node_hoverability_Factory.EXTENSION_NAME) => nodePointer.hoverability,
                _ => throw new InvalidOperationException($"Extension {reader.ToString()} is unsupported at this time!"),
            };
        }

        private static IPointer ProcessWeightsPointer(StringSpanReader reader, BehaviourEngineNode engineNode, NodePointers pointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /nodes/{}/weights/
            var weightIndex = PointerResolver.GetIndexFromArgument(reader, engineNode);

            return pointer.weights[weightIndex];
        }
    }
}