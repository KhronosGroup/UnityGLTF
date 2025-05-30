using GLTF.Schema;
using System.Linq;
using UnityEngine;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.Playback
{

    public class InteractivityExportContext : GLTFExportPluginContext
    {
        public override void AfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode)
        {
            Util.Log($"InteractivityExportContext::AfterMaterialExport ");
        }
        public override void AfterMeshExport(GLTFSceneExporter exporter, Mesh mesh, GLTFMesh gltfMesh, int index)
        {
            Util.Log($"InteractivityExportContext::AfterMeshExport ");
        }
        public override void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, GLTF.Schema.Node node)
        {
            Util.Log($"InteractivityExportContext::AfterNodeExport ");
        }
        public override void AfterPrimitiveExport(GLTFSceneExporter exporter, Mesh mesh, MeshPrimitive primitive, int index)
        {
            Util.Log($"InteractivityExportContext::AfterPrimitiveExport ");
        }
        public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            Util.Log($"InteractivityExportContext::AfterSceneExport ");

            if (exporter.RootTransforms == null) return;
            GLTFInteractivityPlayback playback = null;
            Transform t;

            // This assumes that EventWrapper exists on one of the root transforms which I think must be true due to how we import.
            foreach (var transform in exporter.RootTransforms)
            {
                t = transform;

                if (t.TryGetComponent(out playback))
                    break;

                while (t.parent != null)
                {
                    if (t.parent.TryGetComponent(out playback))
                        break;

                    t = t.parent;
                }
            }

            if (playback == null)
                return;

            exporter.DeclareExtensionUsage(InteractivityGraphExtension.EXTENSION_NAME, true);

            var extensionData = playback.extensionData;

            if(extensionData == null && playback.gameObject.TryGetComponent(out GLTFInteractivityData data))
            {
                var serializer = new GraphSerializer();
                extensionData = serializer.Deserialize(data.interactivityJson);
            }

            gltfRoot.AddExtension(InteractivityGraphExtension.EXTENSION_NAME, new InteractivityGraphExtension(extensionData));
        }
        public override void AfterTextureExport(GLTFSceneExporter exporter, GLTFSceneExporter.UniqueTexture texture, int index, GLTFTexture tex)
        {
            Util.Log($"InteractivityExportContext::AfterTextureExport ");
        }
        public override bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode)
        {
            Util.Log($"InteractivityExportContext::BeforeMaterialExport ");
            return false;
        }
        public override void BeforeNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, GLTF.Schema.Node node)
        {
            Util.Log($"InteractivityExportContext::BeforeNodeExport ");
        }
        public override void BeforeSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            Util.Log($"InteractivityExportContext::BeforeSceneExport ");
        }
        public override void BeforeTextureExport(GLTFSceneExporter exporter, ref GLTFSceneExporter.UniqueTexture texture, string textureSlot)
        {
            Util.Log($"InteractivityExportContext::BeforeTextureExport ");
        }
    }

}