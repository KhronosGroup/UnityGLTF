namespace UnityGLTF.Plugins
{
    [NonRatifiedPlugin]
    public class LodsImport: GLTFImportPlugin
    {
        public override string DisplayName => "MSFT_lod";
        public override string Description => "Creates LODGroups from glTF LODs.";
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new LodsImportContext();
        }
    }
    
    public class LodsImportContext: GLTFImportPluginContext
    {
           
    }
}