namespace UnityGLTF.Plugins
{
    public class Ktx2Import: GLTFImportPlugin
    {
        public override string DisplayName => "KHR_texture_basisu";
        public override string Description => "Import textures using the KTX2 supercompression format (ETC1S, UASTC).";
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new Ktx2ImportContext();
        }
        
#if !HAVE_KTX
        public override string Warning => "Add the package \"com.unity.cloud.ktx\" version v1.3+ to your project for KTX2 texture support.";
#endif
    }
    
    public class Ktx2ImportContext: GLTFImportPluginContext
    {
        
    }
}