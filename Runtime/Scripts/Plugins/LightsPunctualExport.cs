using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    public class LightsPunctualExport: GLTFExportPlugin
    {
        public override string DisplayName => "KHR_lights_punctual";
        public override string Description => "Exports punctual lights (directional, point, spot).";
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return new LightsPunctualExportContext();
        }
    }
    
    public class LightsPunctualExportContext: GLTFExportPluginContext
    {
        public override void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
        {
            base.AfterNodeExport(exporter, gltfRoot, transform, node);
        }
    }
}