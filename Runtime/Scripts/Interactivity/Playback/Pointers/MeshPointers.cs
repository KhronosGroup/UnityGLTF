using GLTF.Schema;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public struct MeshPointers
    {
        public ReadOnlyPointer<int> weightsLength;
        public Pointer<float>[] weights;
        /// <summary>Material index of each primitive, -1 when a primitive has no material.</summary>
        public int[] primitiveMaterials;

        public MeshPointers(in MeshData data, IReadOnlyList<NodeData> nodes)
        {
            var primitives = data.mesh?.Primitives;
            primitiveMaterials = new int[primitives?.Count ?? 0];
            for (int i = 0; i < primitiveMaterials.Length; i++)
                primitiveMaterials[i] = primitives[i].Material?.Id ?? -1;

            var skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
            SkinnedMeshRenderer smr;

            for (int i = 0; i < nodes.Count; i++)
            {
                smr = nodes[i].skinnedMeshRenderer;
                if (smr == null)
                    continue;

                if (smr.sharedMesh != data.unityMesh)
                    continue;

                skinnedMeshRenderers.Add(smr);
            }

            if(skinnedMeshRenderers.Count <= 0)
            {
                weightsLength = new ReadOnlyPointer<int>(() => 0);
                weights = new Pointer<float>[0];
                return;
            }

            smr = skinnedMeshRenderers[0];
            var blendShapeCount = data.unityMesh.blendShapeCount;

            weightsLength = new ReadOnlyPointer<int>(() => blendShapeCount);
            weights = new Pointer<float>[blendShapeCount];

            // TODO: Figure out how this should play with node.weight modifications.
            // Right now this will overwrite those completely.
            for (int i = 0; i < weights.Length; i++)
            {
                // Copy the loop variable so each closure keeps its own blend shape index.
                var index = i;
                weights[i] = new Pointer<float>()
                {
                    setter = (v) => SetAllBlendShapeWeights(index, v),
                    getter = () => smr.GetBlendShapeWeight(index),
                    evaluator = (a, b, t) => math.lerp(a, b, t)
                };
            }

            void SetAllBlendShapeWeights(int index, float value)
            {
                for (int i = 0; i < skinnedMeshRenderers.Count; i++)
                {
                    skinnedMeshRenderers[i].SetBlendShapeWeight(index, value);
                }
            }
        }

        public static IPointer ProcessPointer(StringSpanReader reader, BehaviourEngineNode engineNode, List<MeshPointers> pointers)
        {
            reader.AdvanceToNextToken('/');

            if (!PointerResolver.TryGetIndexFromArgument(reader, engineNode, pointers, out int nodeIndex))
                return PointerHelpers.InvalidPointer();

            var pointer = pointers[nodeIndex];

            reader.AdvanceToNextToken('/');

            // Path so far: /meshes/{}/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is(Pointers.WEIGHTS) => ProcessWeightsPointer(reader, engineNode, pointer),
                var a when a.Is(Pointers.WEIGHTS_LENGTH) => pointer.weightsLength,
                var a when a.Is(Pointers.PRIMITIVES_LENGTH) => new ReadOnlyPointer<int>(() => pointer.primitiveMaterials.Length),
                var a when a.Is(Pointers.PRIMITIVES) => ProcessPrimitivePointer(reader, pointer),
                _ => PointerHelpers.InvalidPointer(),
            };
        }

        private static IPointer ProcessPrimitivePointer(StringSpanReader reader, MeshPointers pointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /meshes/{}/primitives/
            if (!Ref.TryParseCanonicalIndex(reader.AsReadOnlySpan(), out var primitiveIndex) || primitiveIndex >= pointer.primitiveMaterials.Length)
                return PointerHelpers.InvalidPointer();

            reader.AdvanceToNextToken('/');

            // Path so far: /meshes/{}/primitives/{}/
            if (!reader.AsReadOnlySpan().Is(Pointers.MATERIAL))
                return PointerHelpers.InvalidPointer();

            var material = pointer.primitiveMaterials[primitiveIndex];
            return material >= 0 ? new ObjectIndexPointer("/materials", material) : PointerHelpers.InvalidPointer();
        }

        private static IPointer ProcessWeightsPointer(StringSpanReader reader, BehaviourEngineNode engineNode, MeshPointers pointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /meshes/{}/weights/
            if (!PointerResolver.TryGetIndexFromArgument(reader, engineNode, pointer.weights, out var weightIndex))
                return PointerHelpers.InvalidPointer();

            return pointer.weights[weightIndex];
        }
    }
}