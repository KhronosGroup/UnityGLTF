using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;
using UnityGLTF.Interactivity.Playback.Materials;

namespace UnityGLTF.Interactivity.Playback
{
    [Serializable]
    public class PointerResolver
    {
        private readonly List<NodePointers> _nodePointers = new();
        private readonly List<MaterialPointers> _materialPointers = new();
        private readonly List<CameraPointers> _cameraPointers = new();
        private readonly List<AnimationPointers> _animationPointers = new();
        private readonly List<MeshPointers> _meshPointers = new();
        private ScenePointers _scenePointers;
        private readonly ActiveCameraPointers _activeCameraPointers = ActiveCameraPointers.CreatePointers();

        [SerializeField] private List<MeshData> _meshes = new();
        [SerializeField] private List<MaterialData> _materials = new();
        [SerializeField] private List<CameraData> _cameras = new();
        [SerializeField] private List<NodeData> _nodes = new();
        [SerializeField] private SceneData _sceneData;

        public IReadOnlyList<NodeData> nodes => _nodes;
        public ReadOnlyCollection<NodePointers> nodePointers { get; private set; }

        public void RegisterMesh(GLTF.Schema.GLTFMesh mesh, int meshIndex, Mesh unityMesh)
        {
            _meshes.Add(new MeshData(mesh, meshIndex, unityMesh));
        }

        public void RegisterMaterial(GLTF.Schema.GLTFMaterial material, int materialIndex, Material unityMaterial)
        {
            _materials.Add(new MaterialData(material, materialIndex, unityMaterial));
        }

        public void RegisterCamera(GLTF.Schema.GLTFCamera camera, int cameraIndex, Camera unityCamera)
        {
            _cameras.Add(new CameraData(camera, cameraIndex, unityCamera));
        }

        public void RegisterNode(GLTF.Schema.Node node, int nodeIndex, GameObject unityObject)
        {
            var selectable = false;
            var hoverable = false;

            if (node.Extensions != null)
            {
                if (node.Extensions.TryGetValue(GLTF.Schema.KHR_node_selectability_Factory.EXTENSION_NAME, out var extension))
                {
                    var selectabilityExtension = extension as GLTF.Schema.KHR_node_selectability;
                    selectable = selectabilityExtension.selectable;
                }

                if (node.Extensions.TryGetValue(GLTF.Schema.KHR_node_hoverability_Factory.EXTENSION_NAME, out extension))
                {
                    var hoverabilityExtension = extension as GLTF.Schema.KHR_node_hoverability;
                    hoverable = hoverabilityExtension.hoverable;
                }
            }

            _nodes.Add(new NodeData(node, nodeIndex, unityObject, unityObject.GetComponent<SkinnedMeshRenderer>(), selectable, hoverable));
        }

        public void RegisterSceneData(GLTF.Schema.GLTFRoot root)
        {
            _sceneData = new()
            {
                animationCount = (root.Animations == null) ? 0 : root.Animations.Count,
                cameraCount = (root.Cameras == null) ? 0 : root.Cameras.Count,
                materialCount = (root.Materials == null) ? 0 : root.Materials.Count,
                meshCount = (root.Meshes == null) ? 0 : root.Meshes.Count,
                nodeCount = (root.Nodes == null) ? 0 : root.Nodes.Count,
                sceneCount = (root.Scenes == null) ? 0 : root.Scenes.Count
            };
        }

        public void CreatePointers()
        {
            Util.Log("Creating all Pointers for GLTF.");

            _meshes.Sort((a, b) => a.meshIndex.CompareTo(b.meshIndex));
            _materials.Sort((a, b) => a.materialIndex.CompareTo(b.materialIndex));
            _cameras.Sort((a, b) => a.cameraIndex.CompareTo(b.cameraIndex));
            _nodes.Sort((a, b) => a.nodeIndex.CompareTo(b.nodeIndex));

            CreateMeshPointers();
            CreateNodePointers();
            CreateCameraPointers();
            CreateMaterialPointers();
            _scenePointers = new(_sceneData);
        }

        private void CreateMeshPointers()
        {
            for (int i = 0; i < _meshes.Count; i++)
            {
                _meshPointers.Add(new MeshPointers(_meshes[i], _nodes));
            }
        }

        public void CreateAnimationPointers(GLTFInteractivityAnimationWrapper wrapper)
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
        }

        private void CreateCameraPointers()
        {
            for (int i = 0; i < _cameras.Count; i++)
            {
                Util.Log($"Registered Camera {_cameras[i].cameraIndex}", _cameras[i].unityCamera.gameObject);
                _cameraPointers.Add(new CameraPointers(_cameras[i]));
            }
        }

        private void CreateMaterialPointers()
        {
            for (int i = 0; i < _materials.Count; i++)
            {
                _materialPointers.Add(new MaterialPointers(_materials[i]));
            }
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