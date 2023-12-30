using UnityGLTF.Plugins;

namespace UnityGLTF.Extensions
{
    public class MeshoptImport: GltfImportPlugin
    {
        public override string DisplayName => "EXT_meshopt_compression";
        public override string Description => "Import Meshopt compressed meshes.";
        public override GltfImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new MeshoptImportContext();
        }
                
#if !HAVE_MESHOPT_DECOMPRESS
        public override string Warning => "Please add the package \"com.unity.meshopt.decompress\" to your project for Meshopt compression support.";
#endif
    }
    
    public class MeshoptImportContext: GltfImportPluginContext
    {
        
    }
}