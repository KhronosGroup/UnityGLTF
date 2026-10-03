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
        /// <summary>The glTF doubleSided property; copied because <see cref="material"/> is not serialized.</summary>
        public bool doubleSided;

        public MaterialData(GLTF.Schema.GLTFMaterial material, int materialIndex, Material unityMaterial)
        {
            this.material = material;
            this.materialIndex = materialIndex;
            this.unityMaterial = unityMaterial;
            doubleSided = material?.DoubleSided ?? false;
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

        // The node's glTF JSON structure. Copied because Unity does not serialize `node`, so prefabs from an
        // editor import only have these. Indices are -1 when the property is undefined.
        /// <summary>False for data serialized before these fields existed; the structure is then unknown.</summary>
        public bool hasStructure;
        public int[] children;
        public int mesh;
        public int skin;
        public int camera;

        public NodeData(GLTF.Schema.Node node, int nodeIndex, GameObject unityObject, SkinnedMeshRenderer skinnedMeshRenderer, bool isSelectable, bool isHoverable)
        {
            this.node = node;
            this.nodeIndex = nodeIndex;
            this.unityObject = unityObject;
            this.skinnedMeshRenderer = skinnedMeshRenderer;
            this.isSelectable = isSelectable;
            this.isHoverable = isHoverable;

            hasStructure = true;
            var childIds = node?.Children;
            children = new int[childIds?.Count ?? 0];
            for (int i = 0; i < children.Length; i++)
                children[i] = childIds[i].Id;

            mesh = node?.Mesh?.Id ?? -1;
            skin = node?.Skin?.Id ?? -1;
            camera = node?.Camera?.Id ?? -1;
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
        public int skinCount;
        public int textureCount;
        public int imageCount;
        public int samplerCount;
        public int accessorCount;
        public int bufferViewCount;
        public int bufferCount;
        /// <summary>The asset.version string of the glTF JSON, e.g. "2.0".</summary>
        public string assetVersion;
        public List<string> extensionsUsed;
        /// <summary>Index of the scene being presented, or -1 when the asset has no scenes.</summary>
        public int scene;
        /// <summary>Root node indices of each scene, indexed like the glTF scenes array.</summary>
        public List<IndexList> sceneNodes;
        /// <summary>Joints and skeleton of each skin, indexed like the glTF skins array.</summary>
        public List<SkinData> skins;
    }

    /// <summary>A list of glTF indices; a wrapper because Unity cannot serialize a list of arrays.</summary>
    [Serializable]
    public struct IndexList
    {
        public int[] indices;

        public IndexList(int[] indices)
        {
            this.indices = indices;
        }
    }

    [Serializable]
    public struct SkinData
    {
        public int[] joints;
        /// <summary>Node index of the skeleton root, or -1 when undefined.</summary>
        public int skeleton;

        public SkinData(int[] joints, int skeleton)
        {
            this.joints = joints;
            this.skeleton = skeleton;
        }
    }
}