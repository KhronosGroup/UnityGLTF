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
        // Read-only in the glTF Object Model: both reflect the runtime transform and are changed through TRS.
        public ReadOnlyPointer<float4x4> matrix;
        public ReadOnlyPointer<float4x4> globalMatrix;
        public ReadOnlyPointer<int> weightsLength;
        public Pointer<float>[] weights;
        public GameObject gameObject;
        // Read-only structure from the glTF JSON: child node indices, mesh index (-1 if none), parent index (-1 for roots).
        public int[] children;
        public int mesh;
        public int parent;

        public NodePointers(in NodeData data, int parent = -1)
        {
            var go = data.unityObject;
            gameObject = go;
            // Cached: GameObject.transform is a native call, and these closures run every tick during interpolation.
            var tr = go != null ? go.transform : null;

            var childIds = data.node?.Children;
            children = new int[childIds?.Count ?? 0];
            for (int i = 0; i < children.Length; i++)
                children[i] = childIds[i].Id;

            mesh = data.node?.Mesh?.Id ?? -1;
            this.parent = parent;

            // Unity coordinate system differs from the GLTF one.
            // Unity is left-handed with y-up and z-forward.
            // GLTF is right-handed with y-up and z-forward.
            // Handedness is easiest to swap here though we could do it during deserialization for performance.
            translation = new Pointer<float3>()
            {
                setter = (v) => tr.localPosition = v.SwapHandedness(),
                getter = () => tr.localPosition.SwapHandedness(),
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };

            rotation = new Pointer<quaternion>()
            {
                setter = (v) => tr.localRotation = ((Quaternion)v).SwapHandedness(),
                getter = () => tr.localRotation.SwapHandedness(),
                evaluator = (a, b, t) => math.slerp(a, b, t)
            };

            scale = new Pointer<float3>()
            {
                setter = (v) => tr.localScale = v,
                getter = () => tr.localScale,
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };

            matrix = new ReadOnlyPointer<float4x4>(() => tr.GetWorldMatrix(worldSpace: false, rightHanded: true));
            globalMatrix = new ReadOnlyPointer<float4x4>(() => tr.GetWorldMatrix(worldSpace: true, rightHanded: true));

            // TODO: Handle visibility pointers better? Do we report the value back to the extension?
            // Should we make the extension handle the SetActive call so we just change the value of visibility?
            visibility = new Pointer<bool>()
            {
                setter = (v) => go.SetActive(v),
                getter = () => go.activeSelf,
                evaluator = null
            };

            var isSelectable = data.isSelectable;

            selectability = new Pointer<bool>()
            {
                setter = (v) => isSelectable = v,
                getter = () => isSelectable,
                evaluator = null
            };

            var isHoverable = data.isHoverable;

            hoverability = new Pointer<bool>()
            {
                setter = (v) => isHoverable = v,
                getter = () => isHoverable,
                evaluator = null
            };

            if (data.skinnedMeshRenderer != null)
            {
                var smr = data.skinnedMeshRenderer;
                weightsLength = new ReadOnlyPointer<int>(() => smr.sharedMesh.blendShapeCount);
                weights = new Pointer<float>[smr.sharedMesh.blendShapeCount];

                for (int i = 0; i < weights.Length; i++)
                {
                    // Copy the loop variable so each closure keeps its own blend shape index.
                    var index = i;
                    weights[i] = new Pointer<float>()
                    {
                        setter = (v) => smr.SetBlendShapeWeight(index, v),
                        getter = () => smr.GetBlendShapeWeight(index),
                        evaluator = (a, b, t) => math.lerp(a, b, t)
                    };
                }
            }
            else
            {
                // A mesh without morph targets has zero weights. Nodes without a mesh have no weights.length (see ProcessNodePointer).
                weightsLength = new ReadOnlyPointer<int>(() => 0);
                weights = default;
            }
        }

        public static IPointer ProcessNodePointer(StringSpanReader reader, BehaviourEngineNode engineNode, List<NodePointers> pointers)
        {
            reader.AdvanceToNextToken('/');

            if (!PointerResolver.TryGetIndexFromArgument(reader, engineNode, pointers, out int nodeIndex))
                return PointerHelpers.InvalidPointer();

            var nodePointer = pointers[nodeIndex];

            reader.AdvanceToNextToken('/');

            // Path so far: /nodes/{}/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is(Pointers.TRANSLATION) => nodePointer.translation,
                var a when a.Is(Pointers.ROTATION) => nodePointer.rotation,
                var a when a.Is(Pointers.SCALE) => nodePointer.scale,
                var a when a.Is(Pointers.WEIGHTS) => ProcessWeightsPointer(reader, engineNode, nodePointer),
                var a when a.Is(Pointers.WEIGHTS_LENGTH) => nodePointer.mesh >= 0 ? nodePointer.weightsLength : PointerHelpers.InvalidPointer(),
                var a when a.Is(Pointers.EXTENSIONS) => ProcessExtensionPointer(reader, nodePointer),
                var a when a.Is(Pointers.MATRIX) => nodePointer.matrix,
                var a when a.Is(Pointers.GLOBAL_MATRIX) => nodePointer.globalMatrix,
                var a when a.Is(Pointers.CHILDREN_LENGTH) => new ReadOnlyPointer<int>(() => nodePointer.children.Length),
                var a when a.Is(Pointers.CHILDREN) => ProcessChildrenPointer(reader, nodePointer),
                var a when a.Is(Pointers.MESH) => nodePointer.mesh >= 0 ? new ObjectIndexPointer("/meshes", nodePointer.mesh) : PointerHelpers.InvalidPointer(),
                var a when a.Is(Pointers.PARENT) => nodePointer.parent >= 0 ? new ObjectIndexPointer("/nodes", nodePointer.parent) : PointerHelpers.InvalidPointer(),
                _ => PointerHelpers.InvalidPointer(),
            };
        }

        private static IPointer ProcessChildrenPointer(StringSpanReader reader, NodePointers nodePointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /nodes/{}/children/
            if (!Ref.TryParseCanonicalIndex(reader.AsReadOnlySpan(), out var childIndex) || childIndex >= nodePointer.children.Length)
                return PointerHelpers.InvalidPointer();

            return new ObjectIndexPointer("/nodes", nodePointer.children[childIndex]);
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
                _ => PointerHelpers.InvalidPointer(),
            };
        }

        private static IPointer ProcessWeightsPointer(StringSpanReader reader, BehaviourEngineNode engineNode, NodePointers pointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /nodes/{}/weights/
            if (pointer.weights == null || !PointerResolver.TryGetIndexFromArgument(reader, engineNode, pointer.weights, out var weightIndex))
                return PointerHelpers.InvalidPointer();

            return pointer.weights[weightIndex];
        }
    }
}