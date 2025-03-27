using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    public abstract class GLTFExportPlugin: GLTFPlugin
    {
        public virtual JToken AssetExtras
        {
            get => null;
        }
        
        /// <summary>
        /// Return the Plugin Instance that receives the import callbacks
        /// </summary>
        public abstract GLTFExportPluginContext CreateInstance(ExportContext context);
    }

    public abstract class GLTFExportPluginContext
    {
        public virtual void BeforeSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) {}
        public virtual void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) {}
        public virtual bool ShouldNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform) => true;
        public virtual void BeforeNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node) {}
        public virtual void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node) {}
        public virtual bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode) => false;
        public virtual void AfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode) {}
        public virtual void BeforeTextureExport(GLTFSceneExporter exporter, ref GLTFSceneExporter.UniqueTexture texture, string textureSlot) {}
        public virtual void AfterTextureExport(GLTFSceneExporter exporter, GLTFSceneExporter.UniqueTexture texture, int index, GLTFTexture tex) {}
        public virtual void AfterPrimitiveExport(GLTFSceneExporter exporter, Mesh mesh, MeshPrimitive primitive, int index) {}
        public virtual void AfterMeshExport(GLTFSceneExporter exporter, Mesh mesh, GLTFMesh gltfMesh, int index) {}
        
    }
}