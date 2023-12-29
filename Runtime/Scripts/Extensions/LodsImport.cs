namespace UnityGLTF.Plugins
{
    public class LodsImport: GltfImportPlugin
    {
        public override string DisplayName => "MSFT_lod";
        public override string Description => "Creates LODGroups from glTF LODs.";
        public override GltfImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new LodsImportContext();
        }
    }
    
    public class LodsImportContext: GltfImportPluginContext
    {
           
    }
}