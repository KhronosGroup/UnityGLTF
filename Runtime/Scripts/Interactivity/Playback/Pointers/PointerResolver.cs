using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;
using UnityGLTF.Interactivity.Playback.Materials;

namespace UnityGLTF.Interactivity.Playback
{
    public struct MeshData
    {
        public GLTF.Schema.GLTFMesh mesh;
        public int meshIndex;
        public Mesh unityMesh;

        public MeshData(GLTFMesh mesh, int meshIndex, Mesh unityMesh)
        {
            this.mesh = mesh;
            this.meshIndex = meshIndex;
            this.unityMesh = unityMesh;
        }
    }

    public struct MaterialData
    {
        public GLTF.Schema.GLTFMaterial material;
        public int materialIndex;
        public Material unityMaterial;

        public MaterialData(GLTFMaterial material, int materialIndex, Material unityMaterial)
        {
            this.material = material;
            this.materialIndex = materialIndex;
            this.unityMaterial = unityMaterial;
        }
    }

    public struct CameraData
    {
        public GLTF.Schema.GLTFCamera camera;
        public int cameraIndex;
        public Camera unityCamera;

        public CameraData(GLTFCamera camera, int cameraIndex, Camera unityCamera)
        {
            this.camera = camera;
            this.cameraIndex = cameraIndex;
            this.unityCamera = unityCamera;
        }
    }

    public struct NodeData
    {
        public GLTF.Schema.Node node;
        public int nodeIndex;
        public GameObject unityObject;

        public NodeData(GLTF.Schema.Node node, int nodeIndex, GameObject unityObject)
        {
            this.node = node;
            this.nodeIndex = nodeIndex;
            this.unityObject = unityObject;
        }
    }

    public class PointerResolver
    {
        private readonly List<NodePointers> _nodePointers = new();
        private readonly List<MaterialPointers> _materialPointers = new();
        private readonly List<CameraPointers> _cameraPointers = new();
        private readonly List<AnimationPointers> _animationPointers = new();
        private readonly List<MeshPointers> _meshPointers = new();
        private ScenePointers _scenePointers;
        private readonly ActiveCameraPointers _activeCameraPointers = ActiveCameraPointers.CreatePointers();

        private readonly List<MeshData> _meshes = new();
        private readonly List<MaterialData> _materials = new();
        private readonly List<CameraData> _cameras = new();
        private readonly List<NodeData> _nodes = new();

        public ReadOnlyCollection<NodePointers> nodePointers { get; private set; }

        public void RegisterMesh(GLTF.Schema.GLTFMesh mesh, int meshIndex, Mesh unityMesh)
        {
            _meshes.Add(new MeshData(mesh, meshIndex, unityMesh));
        }

        public void RegisterMaterial(GLTFMaterial material, int materialIndex, Material unityMaterial)
        {
            _materials.Add(new MaterialData(material, materialIndex, unityMaterial));
        }

        public void RegisterCamera(GLTFCamera camera, int cameraIndex, Camera unityCamera)
        {
            _cameras.Add(new CameraData(camera, cameraIndex, unityCamera));
        }

        public void RegisterNode(GLTF.Schema.Node node, int nodeIndex, GameObject unityObject)
        {
            _nodes.Add(new NodeData(node, nodeIndex, unityObject));
        }

        public void CreateScenePointers(GLTF.Schema.GLTFRoot root)
        {
            _scenePointers = new ScenePointers(root);
        }

        public void CreatePointers()
        {
            _meshes.Sort((a, b) => a.meshIndex.CompareTo(b.meshIndex));
            _materials.Sort((a, b) => a.materialIndex.CompareTo(b.materialIndex));
            _cameras.Sort((a, b) => a.cameraIndex.CompareTo(b.cameraIndex));
            _nodes.Sort((a, b) => a.nodeIndex.CompareTo(b.nodeIndex));

            CreateMeshPointers();
            CreateNodePointers();
            CreateCameraPointers();
            CreateMaterialPointers();
        }

        private void CreateMeshPointers()
        {
            for (int i = 0; i < _meshes.Count; i++)
            {
                _meshPointers.Add(new MeshPointers(_meshes[i]));
            }

            _meshes.Clear();
        }

        public void CreateAnimationPointers(AnimationWrapper wrapper)
        {
            for (int i = 0; i < wrapper.animationComponent.GetClipCount(); i++)
            {
                _animationPointers.Add(new AnimationPointers(wrapper, i));
            }
        }

        private void CreateNodePointers()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                Util.Log($"Registered Node Pointer {_nodes[i].nodeIndex}", _nodes[i].unityObject);
                _nodePointers.Add(new NodePointers(_nodes[i]));
            }

            nodePointers = new(_nodePointers);
            _nodes.Clear();
        }

        private void CreateCameraPointers()
        {
            for (int i = 0; i < _cameras.Count; i++)
            {
                Util.Log($"Registered Camera {_cameras[i].cameraIndex}", _cameras[i].unityCamera.gameObject);
                _cameraPointers.Add(new CameraPointers(_cameras[i]));
            }

            _cameras.Clear();
        }

        private void CreateMaterialPointers()
        {
            for (int i = 0; i < _materials.Count; i++)
            {
                _materialPointers.Add(new MaterialPointers(_materials[i]));
            }

            _materials.Clear();
        }

        public bool TryGetPointersOf(GameObject go, out NodePointers pointers)
        {
            pointers = default;

            for (int i = 0; i < _nodePointers.Count; i++)
            {
                if (_nodePointers[i].gameObject == go)
                {
                    pointers = _nodePointers[i];
                    return true;
                }
            }

            Debug.LogWarning($"No node pointers found for {go.name}!");
            return false;
        }

        public int IndexOf(GameObject go)
        {
            for (int i = 0; i < _nodePointers.Count; i++)
            {
                if (_nodePointers[i].gameObject == go)
                    return i;
            }

            return -1;
        }

        public IPointer GetPointer(string pointerString, BehaviourEngineNode engineNode)
        {
            Util.Log($"Getting pointer: {pointerString}");

            var reader = new StringSpanReader(pointerString);

            reader.Slice('/', '/');

            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("nodes") => NodePointers.ProcessNodePointer(reader, engineNode, _nodePointers),
                var a when a.Is("materials") => MaterialPointers.ProcessMaterialPointer(reader, engineNode, _materialPointers),
                var a when a.Is("activeCamera") => _activeCameraPointers.ProcessActiveCameraPointer(reader),
                var a when a.Is("cameras") => CameraPointers.ProcessCameraPointer(reader, engineNode, _cameraPointers),
                var a when a.Is("meshes") => MeshPointers.ProcessPointer(reader, engineNode, _meshPointers),
                var a when a.Is("animations") => AnimationPointers.ProcessPointer(reader, engineNode, _animationPointers),
                var a when a.Is(Pointers.ANIMATIONS_LENGTH) => _scenePointers.animationsLength,
                var a when a.Is(Pointers.MATERIALS_LENGTH) => _scenePointers.materialsLength,
                var a when a.Is(Pointers.MESHES_LENGTH) => _scenePointers.meshesLength,
                var a when a.Is(Pointers.NODES_LENGTH) => _scenePointers.nodesLength,
                _ => throw new InvalidOperationException($"No valid pointer found with name {reader.ToString()}"),
            };
        }

        public static int GetIndexFromArgument(StringSpanReader reader, BehaviourEngineNode engineNode)
        {
            int nodeIndex;

            if (reader[0] == '{')
            {
                reader.Slice('{', '}');
                // Can't access the values dictionary with a Span, prevents this from being 0 allocation.
                var property = (Property<int>)engineNode.engine.ParseValue(engineNode.values[reader.ToString()]);
                nodeIndex = property.value;
            }
            else
            {
                nodeIndex = int.Parse(reader.AsReadOnlySpan());
            }

            return nodeIndex;
        }
    }
}