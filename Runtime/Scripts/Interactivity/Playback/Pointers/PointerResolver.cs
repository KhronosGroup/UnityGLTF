using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;
using UnityGLTF.Interactivity.Playback.Materials;

namespace UnityGLTF.Interactivity.Playback
{
    public class PointerResolver
    {
        private readonly List<NodePointers> _nodePointers = new();
        private readonly List<MaterialPointers> _materialPointers = new();
        private readonly List<CameraPointers> _cameraPointers = new();
        private readonly List<AnimationPointers> _animationPointers = new();
        private readonly List<MeshPointers> _meshPointers = new();
        private readonly ScenePointers _scenePointers;
        private readonly ActiveCameraPointers _activeCameraPointers = ActiveCameraPointers.CreatePointers();

        public ReadOnlyCollection<NodePointers> nodePointers { get; private set; }

        public PointerResolver(GLTFSceneImporter importer)
        {
            RegisterNodes(importer);
            RegisterMaterials(importer);
            RegisterMeshes(importer);

            _scenePointers = new ScenePointers(importer);
        }

        private void RegisterMeshes(GLTFSceneImporter importer)
        {
            for (int i = 0; i < importer.Root.Meshes.Count; i++)
            {
                _meshPointers.Add(new MeshPointers(importer.Root.Meshes[i]));
            }
        }

        public void RegisterAnimations(AnimationWrapper wrapper)
        {
            for (int i = 0; i < wrapper.animationComponent.GetClipCount(); i++)
            {
                _animationPointers.Add(new AnimationPointers(wrapper, i));
            }
        }

        private void RegisterNodes(GLTFSceneImporter importer)
        {
            var nodeSchemas = importer.Root.Nodes;
            var nodeGameObjects = importer.NodeCache;

            for (int i = 0; i < nodeGameObjects.Length; i++)
            {
                Util.Log($"Registered Node Pointer {i}", nodeGameObjects[i]);
                _nodePointers.Add(new NodePointers(nodeGameObjects[i], nodeSchemas[i]));

                if (nodeGameObjects[i].TryGetComponent(out Camera cam))
                {
                    Util.Log($"Registered Camera", nodeGameObjects[i]);
                    _cameraPointers.Add(new CameraPointers(cam));
                }
            }

            nodePointers = new(_nodePointers);
        }

        private void RegisterMaterials(GLTFSceneImporter importer)
        {
            var materials = importer.MaterialCache;
            for (int i = 0; i < materials.Length; i++)
            {
                _materialPointers.Add(new MaterialPointers(materials[i].UnityMaterialWithVertexColor));
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