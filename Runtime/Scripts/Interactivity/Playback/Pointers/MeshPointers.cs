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

        public MeshPointers(in MeshData data, IReadOnlyList<NodeData> nodes)
        {
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
                weights[i] = new Pointer<float>()
                {
                    setter = (v) => SetAllBlendShapeWeights(i,v),
                    getter = () => smr.GetBlendShapeWeight(i),
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
                _ => PointerHelpers.InvalidPointer(),
            };
        }

        private static IPointer ProcessWeightsPointer(StringSpanReader reader, BehaviourEngineNode engineNode, MeshPointers pointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /meshes/{}/weights/
            var weightIndex = PointerResolver.GetIndexFromArgument(reader, engineNode);

            return pointer.weights[weightIndex];
        }
    }
}