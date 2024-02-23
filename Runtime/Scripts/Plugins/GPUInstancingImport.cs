namespace UnityGLTF.Plugins
{
    public class GPUInstancingImport: GLTFImportPlugin
    {
        public override string DisplayName => "EXT_mesh_gpu_instancing";
        public override string Description => "Imports GPU instancing.";
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new GPUInstancingImportContext();
        }
    }
    
    public class GPUInstancingImportContext: GLTFImportPluginContext
    {
        
    }
}