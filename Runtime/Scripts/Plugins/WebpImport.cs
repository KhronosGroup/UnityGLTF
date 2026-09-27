namespace UnityGLTF.Plugins
{
    public class WebpImport: GLTFImportPlugin
    {
        public override string DisplayName => "EXT_texture_webp";
        public override string Description => "Import textures using the WebP compression format.";
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new WebPImportContext();
        }
        
#if !HAVE_WEBP
        public override bool PackageMissing => true;
#endif
    }
    
    public class WebPImportContext: GLTFImportPluginContext
    {
        
    }
}