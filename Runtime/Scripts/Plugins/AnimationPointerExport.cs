namespace UnityGLTF.Plugins
{
    public class AnimationPointerExport: GLTFExportPlugin
    {
        public override string DisplayName => "KHR_animation_pointer";
        public override string Description => "Animate arbitrary material and object properties. Without this extension, only node transforms and blend shape weights can be animated.";
        public override bool EnabledByDefault => false;
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return new AnimationPointerExportContext();
        }
    }
    
    public class AnimationPointerExportContext: GLTFExportPluginContext
    {
        public MaterialPropertiesRemapper materialPropertiesRemapper = new DefaultMaterialPropertiesRemapper();
        
    }
}