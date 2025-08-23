using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    [Serializable]
    public struct MeshData
    {
        public GLTF.Schema.GLTFMesh mesh;
        public int meshIndex;
        public Mesh unityMesh;

        public MeshData(GLTF.Schema.GLTFMesh mesh, int meshIndex, Mesh unityMesh)
        {
            this.mesh = mesh;
            this.meshIndex = meshIndex;
            this.unityMesh = unityMesh;
        }
    }

    [Serializable]
    public struct MaterialData
    {
        public GLTF.Schema.GLTFMaterial material;
        public int materialIndex;
        public Material unityMaterial;

        public MaterialData(GLTF.Schema.GLTFMaterial material, int materialIndex, Material unityMaterial)
        {
            this.material = material;
            this.materialIndex = materialIndex;
            this.unityMaterial = unityMaterial;
        }
    }

    [Serializable]
    public struct CameraData
    {
        public GLTF.Schema.GLTFCamera camera;
        public int cameraIndex;
        public Camera unityCamera;

        public CameraData(GLTF.Schema.GLTFCamera camera, int cameraIndex, Camera unityCamera)
        {
            this.camera = camera;
            this.cameraIndex = cameraIndex;
            this.unityCamera = unityCamera;
        }
    }

    [Serializable]
    public struct NodeData
    {
        public GLTF.Schema.Node node;
        public int nodeIndex;
        public GameObject unityObject;
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public bool isSelectable;
        public bool isHoverable;

        public NodeData(GLTF.Schema.Node node, int nodeIndex, GameObject unityObject, SkinnedMeshRenderer skinnedMeshRenderer, bool isSelectable, bool isHoverable)
        {
            this.node = node;
            this.nodeIndex = nodeIndex;
            this.unityObject = unityObject;
            this.skinnedMeshRenderer = skinnedMeshRenderer;
            this.isSelectable = isSelectable;
            this.isHoverable = isHoverable;
        }
    }

    [Serializable]
    public struct SceneData
    {
        public int animationCount;
        public int cameraCount;
        public int materialCount;
        public int meshCount;
        public int nodeCount;
        public int sceneCount;
    }
}