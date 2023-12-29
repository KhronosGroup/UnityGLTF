using UnityGLTF.Plugins;

namespace Scripts.Extensions
{
    public class DracoImport: GltfImportPlugin
    {
        public override string DisplayName => "KHR_draco_mesh_compression";
        public override string Description => "Import Draco compressed meshes.";
        public override GltfImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new DracoImportContext();
        }
    }
    
    public class DracoImportContext: GltfImportPluginContext
    {
        
    }
}