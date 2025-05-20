namespace UnityGLTF.Plugins
{
    public class MeshoptImport: GLTFImportPlugin
    {
        public override string DisplayName => "EXT_meshopt_compression";
        public override string Description => "Import Meshopt compressed meshes.";
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new MeshoptImportContext();
        }
                
#if !HAVE_MESHOPT_DECOMPRESS
        public override bool PackageMissing => true;
#endif
    }
    
    public class MeshoptImportContext: GLTFImportPluginContext
    {
        
    }
}