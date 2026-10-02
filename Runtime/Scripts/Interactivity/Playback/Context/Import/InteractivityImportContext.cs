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

            if (!interactivityGraph.extensionData.TryGetDefaultGraph(out var graph))
            {
                Debug.LogWarning($"KHR_interactivity: the default graph is invalid, the asset is treated as having no interactivity.\n{string.Join("\n", interactivityGraph.extensionData.errors)}");
                return;
            }

            _interactivityGraph = interactivityGraph;

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
            Util.Log($"InteractivityImportContext::OnAfterImportNode Complete: {node.ToString()}");
            _pointerResolver.RegisterNode(node, nodeIndex, nodeObject);
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

            // Selection and hover rays are tested against each node's own geometry, and every node is selectable
            // and hoverable unless it or an ancestor says otherwise, so every mesh needs an exact collider.
            if (_hasSelectOrHoverNode)
            {
                AddCollidersToChildSkinnedMeshRenderers(sceneObject);
                AddCollidersToChildMeshRenderers(sceneObject);
            }

            try
            {
                var importer = _context.SceneImporter;
                _pointerResolver.RegisterSceneData(importer.Root);
                _pointerResolver.RegisterMissingMeshesAndMaterials(importer.Root, i => i < importer.MeshCache.Length ? importer.MeshCache[i]?.LoadedMesh : null);
                _pointerResolver.CreatePointers();

                _interactivityGraph.extensionData.TryGetDefaultGraph(out var defaultGraph);
                var eng = new BehaviourEngine(defaultGraph, _pointerResolver);

                if (!eng.isValid)
                {
                    Debug.LogWarning($"KHR_interactivity: the graph was rejected, the asset is treated as having no interactivity.\n{string.Join("\n", defaultGraph.errors)}");
                    return;
                }

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
                    // A node may have both an importer collider and an exact one; one wrapper per object avoids double events.
                    if (colliders[i].TryGetComponent(out GLTFInteractivityEventWrapper _))
                        continue;

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

        // Mesh colliders need triangles; line and point meshes (e.g. debug lines) cannot be hit by a ray anyway.
        private static bool IsTriangleMesh(Mesh mesh)
        {
            if (mesh == null || mesh.subMeshCount == 0)
                return false;

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var topology = mesh.GetTopology(i);
                if (topology != MeshTopology.Triangles && topology != MeshTopology.Quads)
                    return false;
            }

            return true;
        }

        private void AddCollidersToChildSkinnedMeshRenderers(GameObject nodeObject)
        {
            var smrs = nodeObject.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (smrs.Length <= 0)
                return;

            GameObject go;

            for (int i = 0; i < smrs.Length; i++)
            {
                go = smrs[i].gameObject;

                if (IsTriangleMesh(smrs[i].sharedMesh) && !GLTFInteractivityEventWrapper.HasExactCollider(go))
                {
                    var mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = smrs[i].sharedMesh;
                }
            }
        }

        private void AddCollidersToChildMeshRenderers(GameObject nodeObject)
        {
            var meshFilters = nodeObject.GetComponentsInChildren<MeshFilter>();

            if (meshFilters.Length <= 0)
                return;

            GameObject go;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                go = meshFilters[i].gameObject;

                if (IsTriangleMesh(meshFilters[i].sharedMesh) && !GLTFInteractivityEventWrapper.HasExactCollider(go))
                {
                    var mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = meshFilters[i].sharedMesh;
                }
            }         
        }
    }
}