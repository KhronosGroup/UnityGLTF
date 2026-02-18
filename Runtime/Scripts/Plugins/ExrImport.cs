namespace UnityGLTF.Plugins
{
    public class ExrImport: GLTFImportPlugin
    {
#if UNITY_6000_0_OR_NEWER
        public override string DisplayName => "EXT_texture_exr";
        public override string Description => "Import textures using the EXR format.";
#else
        public override string DisplayName => "EXT_texture_exr (Unsupported Unity Version)";
        public override string Description => "Import textures using the EXR format. (Only supported in Unity 6000 or later!)";
#endif

#if !UNITY_6000_0_OR_NEWER
        public override bool Enabled { get => false; set { } }
#endif

        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new ExrImportContext();
        }
    }
    
    public class ExrImportContext: GLTFImportPluginContext
    {
        
    }
}