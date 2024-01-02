namespace UnityGLTF.Plugins
{
    public class DracoImport: GltfImportPlugin
    {
        public override string DisplayName => "KHR_draco_mesh_compression";
        public override string Description => "Import Draco compressed meshes.";
        public override GltfImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new DracoImportContext();
        }
        
#if !HAVE_DRACO
        public override string Warning => "Please add the package \"com.atteneder.draco\" to your project for Draco mesh compression support.";
#endif
    }
    
    public class DracoImportContext: GltfImportPluginContext
    {
        
    }
}