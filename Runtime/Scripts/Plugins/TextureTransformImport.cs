namespace UnityGLTF.Plugins
{
    public class TextureTransformImport: GltfImportPlugin
    {
        public override string DisplayName => "KHR_texture_transform";
        public override string Description => "Imports texture transforms (offset, scale, rotation).";
        public override bool AlwaysEnabled => true;
        public override GltfImportPluginContext CreateInstance(GLTFImportContext context)
        {
            // always enabled
            return null;
        }
    }
}