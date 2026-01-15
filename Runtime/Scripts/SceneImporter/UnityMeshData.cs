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
    }
}