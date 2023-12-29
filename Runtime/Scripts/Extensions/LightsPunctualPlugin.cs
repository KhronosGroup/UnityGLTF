using UnityGLTF.Plugins;

namespace Scripts.Extensions
{
    public class LightsPunctualPlugin: GltfImportPlugin
    {
        public override string DisplayName => "KHR_lights_punctual";
        public override string Description => "Imports punctual lights (directional, point, spot).";
        public override GltfImportPluginContext CreateInstance(GLTFImportContext context)
        {
            // always enabled
            return null;
        }
    }
}