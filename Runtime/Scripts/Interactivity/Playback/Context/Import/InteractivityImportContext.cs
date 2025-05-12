using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.Playback
{
    public class InteractivityImportContext : GLTFImportPluginContext
    {
        internal readonly InteractivityImportPlugin settings;

        public InteractivityImportContext(InteractivityImportPlugin interactivityLoader)
        {
            settings = interactivityLoader;
        }

        /// <summary>
        /// Called before import starts
        /// </summary>
        public override void OnBeforeImport()
        {
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
        }

        public override void OnAfterImportMaterial(GLTFMaterial material, int materialIndex, Material materialObject)
        {
            Util.Log($"InteractivityImportContext::OnAfterImportMaterial Complete: {material.ToString()}");
        }

        public override void OnAfterImportTexture(GLTFTexture texture, int textureIndex, Texture textureObject)
        {
            Util.Log($"InteractivityImportContext::OnAfterImportTexture Complete: {texture.ToString()}");
        }

        public override void OnAfterImportScene(GLTFScene scene, int sceneIndex, GameObject sceneObject)
        {
            Util.Log($"InteractivityImportContext::OnAfterImportScene Complete: {scene.Extensions}");
        }

        public override void OnAfterImport()
        {
            Util.Log($"InteractivityImportContext::OnAfterImport Complete");
        }
    }

}