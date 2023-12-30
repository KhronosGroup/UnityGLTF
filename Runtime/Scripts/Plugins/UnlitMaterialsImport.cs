namespace UnityGLTF.Plugins
{
    public class UnlitMaterialsImport: GltfImportPlugin
    {
        public override string DisplayName => "KHR_materials_unlit";
        public override string Description => "Imports unlit materials.";
        public override bool AlwaysEnabled => true;
        public override GltfImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return null;
        }
    }
}