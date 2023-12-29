using UnityGLTF.Plugins;

namespace Scripts.Extensions
{
    public class Ktx2Import: GltfImportPlugin
    {
        public override string DisplayName => "KHR_texture_basisu";
        public override string Description => "Import textures using the KTX2 supercompression format (ETC1S, UASTC).";
        public override GltfImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new Ktx2ImportContext();
        }
    }
    
    public class Ktx2ImportContext: GltfImportPluginContext
    {
        
    }
}