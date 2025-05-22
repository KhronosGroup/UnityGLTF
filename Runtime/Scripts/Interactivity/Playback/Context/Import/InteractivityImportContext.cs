using GLTF.Schema;
using System;
using UnityEngine;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.Playback
{
    public class InteractivityImportContext : GLTFImportPluginContext
    {
        internal readonly InteractivityImportPlugin settings;
        private PointerResolver _pointerResolver;
        private GLTFImportContext _context;

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

            try
            {
                _pointerResolver.RegisterSceneData(_context.SceneImporter.Root);
                _pointerResolver.CreatePointers();

                var defaultGraphIndex = interactivityGraph.extensionData.defaultGraphIndex;
                // Can be used to inject a graph created from code in a hacky way for testing.
                //interactivityGraph.extensionData.graphs[defaultGraphIndex] = TestGraph.CreateTestGraph();
                var defaultGraph = interactivityGraph.extensionData.graphs[defaultGraphIndex];
                var eng = new BehaviourEngine(defaultGraph, _pointerResolver);

                GLTFInteractivityAnimationWrapper animationWrapper = null;
                var animationComponents = sceneObject.GetComponents<Animation>();
                if (animationComponents != null && animationComponents.Length > 0)
                {
                    animationWrapper = sceneObject.AddComponent<GLTFInteractivityAnimationWrapper>();
                    eng.SetAnimationWrapper(animationWrapper, animationComponents[0]);
                }

                var eventWrapper = sceneObject.AddComponent<GLTFInteractivityPlayback>();

                eventWrapper.SetData(eng, interactivityGraph.extensionData);

                if (!Application.isPlaying)
                {
                    var data = sceneObject.AddComponent<GLTFInteractivityData>();
                    data.interactivityJson = interactivityGraph.json;
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
    }
}