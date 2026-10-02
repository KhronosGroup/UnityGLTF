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

        private Dictionary<string, IPointer> _pointerCache = new();

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
            // KHR_node_selectability / KHR_node_hoverability: a node without the extension is selectable and hoverable.
            var selectable = true;
            var hoverable = true;

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
                sceneCount = (root.Scenes == null) ? 0 : root.Scenes.Count,
                skinCount = root.Skins?.Count ?? 0,
                textureCount = root.Textures?.Count ?? 0,
                imageCount = root.Images?.Count ?? 0,
                samplerCount = root.Samplers?.Count ?? 0,
                accessorCount = root.Accessors?.Count ?? 0,
                bufferViewCount = root.BufferViews?.Count ?? 0,
                bufferCount = root.Buffers?.Count ?? 0,
                assetVersion = root.Asset?.Version,
                extensionsUsed = root.ExtensionsUsed != null ? new List<string>(root.ExtensionsUsed) : new List<string>(),
            };
        }

        /// <summary>
        /// Whether a static glTF reference such as "/animations/1" addresses an object in the asset.
        /// Non-glTF references (delays, events) are runtime objects and are not checked here.
        /// </summary>
        public bool RefExists(Ref r)
        {
            if (r.kind != RefKind.Gltf)
                return !r.isNull;

            var count = r.collection switch
            {
                "/animations" => Math.Max(_sceneData.animationCount, _animationPointers.Count),
                "/cameras" => Math.Max(_sceneData.cameraCount, _cameras.Count),
                "/materials" => Math.Max(_sceneData.materialCount, _materials.Count),
                "/meshes" => Math.Max(_sceneData.meshCount, _meshes.Count),
                "/nodes" => Math.Max(_sceneData.nodeCount, _nodes.Count),
                "/scenes" => _sceneData.sceneCount,
                "/skins" => _sceneData.skinCount,
                "/textures" => _sceneData.textureCount,
                "/images" => _sceneData.imageCount,
                "/samplers" => _sceneData.samplerCount,
                "/accessors" => _sceneData.accessorCount,
                "/bufferViews" => _sceneData.bufferViewCount,
                "/buffers" => _sceneData.bufferCount,
                _ => 0,
            };

            return r.id >= 0 && r.id < count;
        }

        public SceneData sceneData => _sceneData;

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

        /// <summary>
        /// Resolves an effective (fully substituted) JSON pointer. Template parameters are substituted beforehand
        /// by <see cref="PointerTemplate.TryGenerate"/>, so every path segment here is a literal or a plain index.
        /// </summary>
        public IPointer GetPointer(string pointerString, BehaviourEngineNode engineNode)
        {
            Util.Log($"Getting pointer: {pointerString}");

            if (string.IsNullOrEmpty(pointerString) || pointerString[0] != '/')
                return PointerHelpers.InvalidPointer();

            var reader = new StringSpanReader(pointerString);

            reader.Slice('/', '/');

            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("nodes") => NodePointers.ProcessNodePointer(reader, engineNode, _nodePointers),
                var a when a.Is("materials") => MaterialPointers.ProcessMaterialPointer(reader, engineNode, _materialPointers),
                var a when a.Is("cameras") => CameraPointers.ProcessCameraPointer(reader, engineNode, _cameraPointers),
                var a when a.Is("meshes") => MeshPointers.ProcessPointer(reader, engineNode, _meshPointers),
                var a when a.Is("animations") => AnimationPointers.ProcessPointer(reader, engineNode, _animationPointers),
                var a when a.Is(Pointers.EXTENSIONS) => InteractivityExtensionPointers.Process(reader, engineNode?.engine, _activeCameraPointers, _sceneData),
                var a when a.Is(Pointers.ANIMATIONS_LENGTH) => _scenePointers.animationsLength,
                var a when a.Is(Pointers.MATERIALS_LENGTH) => _scenePointers.materialsLength,
                var a when a.Is(Pointers.MESHES_LENGTH) => _scenePointers.meshesLength,
                var a when a.Is(Pointers.NODES_LENGTH) => _scenePointers.nodesLength,
                _ => PointerHelpers.InvalidPointer(),
            };
        }

        /// <summary>Parses the current path segment as a canonical, non-negative array index.</summary>
        public static int GetIndexFromArgument(StringSpanReader reader, BehaviourEngineNode engineNode)
        {
            if (!Ref.TryParseCanonicalIndex(reader.AsReadOnlySpan(), out var index))
                throw new FormatException($"\"{reader.ToString()}\" is not an array index.");

            return index;
        }

        public static bool TryGetIndexFromArgument<T>(StringSpanReader reader, BehaviourEngineNode engineNode, IReadOnlyList<T> list, out int index)
        {
            if (!Ref.TryParseCanonicalIndex(reader.AsReadOnlySpan(), out index))
                return false;

            return index >= 0 && index < list.Count;
        }
    }
}