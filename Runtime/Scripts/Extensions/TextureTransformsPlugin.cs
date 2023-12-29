using UnityGLTF.Plugins;

namespace Scripts.Extensions
{
    public class TextureTransformsPlugin: GltfImportPlugin
    {
        public override string DisplayName => "KHR_texture_transforms";
        public override string Description => "Imports texture transforms (offset, scale, rotation).";
        public override GltfImportPluginContext CreateInstance(GLTFImportContext context)
        {
            // always enabled
            return null;
        }
    }
}