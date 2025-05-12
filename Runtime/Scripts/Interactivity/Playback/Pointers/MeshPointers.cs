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

        public MeshPointers(GLTFMesh mesh)
        {
            if(mesh.Weights == null || mesh.Weights.Count == 0)
            {
                weightsLength = new ReadOnlyPointer<int>(() => 0);
                weights = new Pointer<float>[0];
                return;
            }

            weightsLength = new ReadOnlyPointer<int>(() => mesh.Weights.Count);
            weights = new Pointer<float>[mesh.Weights.Count];

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = new Pointer<float>()
                {
                    setter = (v) => { }, // TODO: Figure this out, Unity does not handle blend shapes like GLTF does so setting it directly on a mesh is difficult.
                    getter = () => (float)mesh.Weights[i],
                    evaluator = (a, b, t) => math.lerp(a, b, t)
                };
            }
        }

        public static IPointer ProcessPointer(StringSpanReader reader, BehaviourEngineNode engineNode, List<MeshPointers> pointers)
        {
            reader.AdvanceToNextToken('/');

            var nodeIndex = PointerResolver.GetIndexFromArgument(reader, engineNode);

            var pointer = pointers[nodeIndex];

            reader.AdvanceToNextToken('/');

            // Path so far: /meshes/{}/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is(Pointers.WEIGHTS) => ProcessWeightsPointer(reader, engineNode, pointer),
                var a when a.Is(Pointers.WEIGHTS_LENGTH) => pointer.weightsLength,
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
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