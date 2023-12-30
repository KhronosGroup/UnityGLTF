namespace UnityGLTF.Plugins
{
    public class TextureTransformExport: GltfExportPlugin
    {
        public override string DisplayName => "KHR_texture_transform";
        public override string Description => "Exports texture transforms (offset, scale, rotation).";
        public override bool AlwaysEnabled => true;
        public override GltfExportPluginContext CreateInstance(ExportContext context)
        {
            // always enabled
            return null;
        }
    }
}