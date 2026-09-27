namespace UnityGLTF.Plugins
{
#if !UNITY_6000_0_OR_NEWER
    [UnsupportedUnityVersionPlugin("Unity 6.0+")]
#endif
    public class ExrImport: GLTFImportPlugin
    {
        public override string DisplayName => "EXT_texture_exr";
        public override string Description => "Import textures using the EXR format.";

        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new ExrImportContext();
        }
    }
    
    public class ExrImportContext: GLTFImportPluginContext
    {
        
    }
}