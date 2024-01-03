namespace UnityGLTF.Plugins
{
    public class TextureTransformImport: GLTFImportPlugin
    {
        public override string DisplayName => "KHR_texture_transform";
        public override string Description => "Imports texture transforms (offset, scale, rotation).";
        public override bool AlwaysEnabled => true;
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            // always enabled
            return null;
        }
    }
}