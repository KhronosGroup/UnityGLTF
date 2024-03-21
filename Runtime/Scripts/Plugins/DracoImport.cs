namespace UnityGLTF.Plugins
{
    public class DracoImport: GLTFImportPlugin
    {
        public override string DisplayName => "KHR_draco_mesh_compression";
        public override string Description => "Import Draco compressed meshes.";
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new DracoImportContext();
        }
        
#if !HAVE_DRACO
        public override string Warning => "Add the package \"com.unity.cloud.draco\" to your project for Draco mesh compression support.";
#endif
    }

    public class DracoImportContext: GLTFImportPluginContext
    {
        
    }
}