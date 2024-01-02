namespace UnityGLTF.Plugins
{
    public class LightsPunctualExport: GltfExportPlugin
    {
        public override string DisplayName => "KHR_lights_punctual";
        public override string Description => "Exports punctual lights (directional, point, spot).";
        public override GltfExportPluginContext CreateInstance(ExportContext context)
        {
            return new LightsPunctualExportContext();
        }
    }
    
    public class LightsPunctualExportContext: GltfExportPluginContext
    {
        
    }
}