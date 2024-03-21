namespace UnityGLTF.Plugins
{
    public class LightsPunctualImport: GLTFImportPlugin
    {
        public override string DisplayName => "KHR_lights_punctual";
        public override string Description => "Imports punctual lights (directional, point, spot).";
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new LightsPunctualImportContext();
        }
    }
    
    public class LightsPunctualImportContext: GLTFImportPluginContext
    {
        
    }
}