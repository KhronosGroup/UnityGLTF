using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF
{
    public class UnityMeshData
    {
        public bool[] subMeshDataCreated;
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector4[] Tangents;
        public Vector2[] Uv1;
        public Vector2[] Uv2;
        public Vector2[] Uv3;
        public Vector2[] Uv4;
        public Color[] Colors;
        public BoneWeight[] BoneWeights;

        public Vector3[][] MorphTargetVertices;
        public Vector3[][] MorphTargetNormals;
        public Vector3[][] MorphTargetTangents;

        public MeshTopology[] Topology;
        public DrawMode[] DrawModes;
        public int[][] Indices;

        public HashSet<int> alreadyAddedAccessors = new HashSet<int>();
        public uint[] subMeshVertexOffset;

        public void Clear()
        {
            Vertices = null;
            Normals = null;
            Tangents = null;
            Uv1 = null;
            Uv2 = null;
            Uv3 = null;
            Uv4 = null;
            Colors = null;
            BoneWeights = null;
            MorphTargetVertices = null;
            MorphTargetNormals = null;
            MorphTargetTangents = null;
            Topology = null;
            Indices = null;
            subMeshVertexOffset = null;
        }

        public bool IsEqual(UnityMeshData other)
        {
            bool CompareArray<T>(T[] array1, T[] array2)
            {
                if (array1 == null && array2 == null)
                    return true;
                if (array1 == array2)
                    return true;
                if (array1?.Length != array2?.Length)
                    return false;

                for (int i = 0; i < array1.Length; i++)
                {
                    if (!array1[i].Equals(array2[i]))
                        return false;
                }

                return true;
            }

            bool CompareArray2<T>(T[][] array1, T[][] array2)
            {
                if (array1 == null && array2 == null)
                    return true;
                if (array1 == array2)
                    return true;
                if (array1?.Length != array2?.Length)
                    return false;

                for (int i = 0; i < array1.Length; i++)
                {
                    if (array1[i] == null && array2[i] == null)
                        continue;
                    if (array1[i] == array2[i])
                        continue;

                    if (array1[i].Length != array2[i].Length)
                        return false;

                    for (int j = 0; j < array1.Length; j++)
                    {
                        if (!array1[i][j].Equals(array2[i][j]))
                            return false;
                    }
                }

                return true;
            }

            if (!CompareArray(Vertices, other.Vertices)
                || !CompareArray(Normals, other.Normals)
                || !CompareArray(Tangents, other.Tangents)
                || !CompareArray(Uv1, other.Uv1)
                || !CompareArray(Uv2, other.Uv2)
                || !CompareArray(Uv3, other.Uv3)
                || !CompareArray(Uv4, other.Uv4)
                || !CompareArray(Colors, other.Colors)
                || !CompareArray(BoneWeights, other.BoneWeights)
                || !CompareArray(Topology, other.Topology)
                || !CompareArray(DrawModes, other.DrawModes)
                || !CompareArray2(MorphTargetVertices, other.MorphTargetVertices)
                || !CompareArray2(MorphTargetNormals, other.MorphTargetNormals)
                || !CompareArray2(MorphTargetTangents, other.MorphTargetTangents)
                || !CompareArray2(Indices, other.Indices))
                return false;

            return true;
        }
    }
}