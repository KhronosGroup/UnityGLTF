using UnityGLTF.Plugins;

namespace a
{
    public class MeshoptImport: GltfImportPlugin
    {
        public override string DisplayName => "EXT_meshopt_compression";
        public override string Description => "Import Meshopt compressed meshes.";
        public override GltfImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new MeshoptImportContext();
        }
    }
    
    public class MeshoptImportContext: GltfImportPluginContext
    {
        
    }
}