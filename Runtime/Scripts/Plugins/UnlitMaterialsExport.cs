namespace UnityGLTF.Plugins
{
    public class UnlitMaterialsExport: GLTFExportPlugin
    {
        public override string DisplayName => "KHR_materials_unlit";
        public override string Description => "Exports unlit materials.";
        public override bool AlwaysEnabled => true;
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            // always enabled
            return null;
        }
    }
}