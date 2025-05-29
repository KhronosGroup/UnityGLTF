using GLTF.Schema;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.Playback
{
    public class InteractivityImportContext : GLTFImportPluginContext
    {
        internal readonly InteractivityImportPlugin settings;
        private PointerResolver _pointerResolver;
        private GLTFImportContext _context;
        private InteractivityGraphExtension _interactivityGraph;
        private bool _hasSelectOrHoverNode;
        private List<GameObject> _selectableOrHoverableObjects;

        public InteractivityImportContext(InteractivityImportPlugin interactivityLoader, GLTFImportContext context)
        {
            settings = interactivityLoader;
            _context = context;
        }

        /// <summary>
        /// Called before import starts
        /// </summary>
        public override void OnBeforeImport()
        {
            _hasSelectOrHoverNode = false;
            _selectableOrHoverableObjects = new();
            _pointerResolver = new();
            Util.Log($"InteractivityImportContext::OnBeforeImport Complete");
        }

        public override void OnBeforeImportRoot()
        {
            Util.Log($"InteractivityImportContext::OnBeforeImportRoot Complete");
        }

        /// <summary>
        /// Called when the GltfRoot has been deserialized
        /// </summary>
        public override void OnAfterImportRoot(GLTFRoot gltfRoot)
        {
            var extensions = _context.SceneImporter.Root?.Extensions;

            if (extensions == null)
            {
                Util.Log("Extensions are null.");
                return;
            }

            if (!extensions.TryGetValue(InteractivityGraphExtension.EXTENSION_NAME, out IExtension extensionValue))
            {
                Util.Log("Extensions does not contain interactivity.");
                return;
            }

            if (extensionValue is not InteractivityGraphExtension interactivityGraph)
            {
                Util.Log("Extensions does not contain a graph.");
                return;
            }

            Util.Log("Extensions contains interactivity.");

            _interactivityGraph = interactivityGraph;

            var graph = interactivityGraph.extensionData.graphs[interactivityGraph.extensionData.defaultGraphIndex];

            for (int i = 0; i < graph.declarations.Count; i++)
            {
                switch(graph.declarations[i].op)
                {
                    case "event/onSelect":
                    case "event/onHoverIn":
                    case "event/onHoverOut":
                        _hasSelectOrHoverNode = true;
                        break;
                }
            }

            if(!_hasSelectOrHoverNode)
                return;

            Util.Log("Select or hover node present.");

            Util.Log($"InteractivityImportContext::OnAfterImportRoot Complete: {gltfRoot.ToString()}");
        }

        public override void OnBeforeImportScene(GLTFScene scene)
        {
            Util.Log($"InteractivityImportContext::OnBeforeImportScene Complete: {scene.ToString()}");
        }

        public override void OnAfterImportNode(GLTF.Schema.Node node, int nodeIndex, GameObject nodeObject)
        {
            CheckIfNodeIsInteractable(node, nodeIndex, nodeObject);
       
            Util.Log($"InteractivityImportContext::OnAfterImportNode Complete: {node.ToString()}");
            _pointerResolver.RegisterNode(node, nodeIndex, nodeObject);
        }

        private void CheckIfNodeIsInteractable(GLTF.Schema.Node node, int nodeIndex, GameObject nodeObject)
        {
            if (!_hasSelectOrHoverNode)
                return;

            var selectable = false;
            var hoverable = false;

            if (node.Extensions != null)
            {
                if (node.Extensions.TryGetValue(GLTF.Schema.KHR_node_selectability_Factory.EXTENSION_NAME, out var selectableExtension))
                    selectable = (selectableExtension as GLTF.Schema.KHR_node_selectability).selectable;

                if (node.Extensions.TryGetValue(GLTF.Schema.KHR_node_hoverability_Factory.EXTENSION_NAME, out var hoverableExtension))
                    hoverable = (hoverableExtension as GLTF.Schema.KHR_node_hoverability).hoverable;
            }

            if (!selectable && !hoverable)
                return;

            _selectableOrHoverableObjects.Add(nodeObject);
        }

        public override void OnAfterImportMesh(GLTFMesh mesh, int meshIndex, Mesh meshObject)
        {
            Util.Log($"InteractivityImportContext::OnAfterImportMesh Complete: {mesh.ToString()}");
            _pointerResolver.RegisterMesh(mesh, meshIndex, meshObject);
        }

        public override void OnAfterImportMaterialWithVertexColors(GLTFMaterial material, int materialIndex, Material materialObject)
        {
            Util.Log($"InteractivityImportContext::OnAfterImportMaterial Complete: {material.ToString()}");
            _pointerResolver.RegisterMaterial(material, materialIndex, materialObject);
        }

        public override void OnAfterImportCamera(GLTFCamera camera, int cameraIndex, Camera cameraObject)
        {
            Util.Log($"InteractivityImportContext::OnAfterImportCamera Complete: {camera.ToString()}");
            _pointerResolver.RegisterCamera(camera, cameraIndex, cameraObject);
        }

        public override void OnAfterImportTexture(GLTFTexture texture, int textureIndex, Texture textureObject)
        {
            Util.Log($"InteractivityImportContext::OnAfterImportTexture Complete: {texture.ToString()}");
        }

        public override void OnAfterImportScene(GLTFScene scene, int sceneIndex, GameObject sceneObject)
        {
            Util.Log($"InteractivityImportContext::OnAfterImportScene Complete: {scene.Extensions}");

            if (_interactivityGraph == null)
                return;

            for (int i = 0; i < _selectableOrHoverableObjects.Count; i++)
            {
                AddCollidersToChildrenOfInteractableNode(_selectableOrHoverableObjects[i]);
            }

            try
            {
                _pointerResolver.RegisterSceneData(_context.SceneImporter.Root);
                _pointerResolver.CreatePointers();

                var defaultGraphIndex = _interactivityGraph.extensionData.defaultGraphIndex;
                // Can be used to inject a graph created from code in a hacky way for testing.
                //interactivityGraph.extensionData.graphs[defaultGraphIndex] = TestGraph.CreateTestGraph();
                var defaultGraph = _interactivityGraph.extensionData.graphs[defaultGraphIndex];
                var eng = new BehaviourEngine(defaultGraph, _pointerResolver);

                GLTFInteractivityAnimationWrapper animationWrapper = null;
                var animationComponents = sceneObject.GetComponents<Animation>();
                if (animationComponents != null && animationComponents.Length > 0)
                {
                    animationWrapper = sceneObject.AddComponent<GLTFInteractivityAnimationWrapper>();
                    eng.SetAnimationWrapper(animationWrapper, animationComponents[0]);
                }

                var playback = sceneObject.AddComponent<GLTFInteractivityPlayback>();

                playback.SetData(eng, _interactivityGraph.extensionData);

                var colliders = sceneObject.GetComponentsInChildren<Collider>(true);

                for (int i = 0; i < colliders.Length; i++)
                {
                    var wrapper = colliders[i].gameObject.AddComponent<GLTFInteractivityEventWrapper>();
                    wrapper.playback = playback;
                }

                if (_context.AssetContext != null)
                {
                    var data = sceneObject.AddComponent<GLTFInteractivityData>();
                    data.interactivityJson = _interactivityGraph.json;
                    data.animationWrapper = animationWrapper;
                    data.pointerReferences = _pointerResolver;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }
        }

        private void AddCollidersToChildrenOfInteractableNode(GameObject nodeObject)
        {
            var meshFilters = nodeObject.GetComponentsInChildren<MeshFilter>();

            if (meshFilters.Length <= 0)
                return;

            GameObject go;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                go = meshFilters[i].gameObject;

                if (!go.TryGetComponent(out Collider collider))
                    go.AddComponent<BoxCollider>();
            }         
        }
    }
}