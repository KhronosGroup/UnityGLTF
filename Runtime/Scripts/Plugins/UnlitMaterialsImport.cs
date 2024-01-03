namespace UnityGLTF.Plugins
{
    public class UnlitMaterialsImport: GLTFImportPlugin
    {
        public override string DisplayName => "KHR_materials_unlit";
        public override string Description => "Imports unlit materials.";
        public override bool AlwaysEnabled => true;
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            // always enabled
            return null;
        }
    }
}