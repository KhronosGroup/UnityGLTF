using GLTF.Schema;

namespace UnityGLTF.Plugins
{
    public class VisibilityImport: GLTFImportPlugin
    {
        public override string DisplayName => KHR_node_visibility_Factory.EXTENSION_NAME;
        public override string Description => "Imports visibility of objects";
        public override bool AlwaysEnabled => true;
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            // always enabled
            return null;
        }
        
    }
}