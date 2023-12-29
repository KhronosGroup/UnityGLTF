using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    public abstract class GltfExportPlugin: GltfPlugin
    {
        /// <summary>
        /// Return the Plugin Instance that receives the import callbacks
        /// </summary>
        public abstract GltfExportPluginContext CreateInstance(ExportContext context);
    }

    public abstract class GltfExportPluginContext
    {
        public virtual void BeforeSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) {}
        public virtual void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) {}
        public virtual void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node) {}
        public virtual bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode) => false;
        public virtual void AfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode) {}
        public virtual void BeforeTextureExport(GLTFSceneExporter exporter, ref GLTFSceneExporter.UniqueTexture texture, string textureSlot) {}
        public virtual void AfterTextureExport(GLTFSceneExporter exporter, GLTFSceneExporter.UniqueTexture texture, int index, GLTFTexture tex) {}
        public virtual void AfterPrimitiveExport(GLTFSceneExporter exporter, Mesh mesh, MeshPrimitive primitive, int index) {}
        
    }
}