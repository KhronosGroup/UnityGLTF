namespace UnityGLTF.Plugins
{
    public class AnimationPointerImport: GLTFImportPlugin
    {
        public override string DisplayName => "KHR_animation_pointer";
        public override string Description => "Animate arbitrary material and object properties. Without this extension, only node transforms and blend shape weights can be animated.";
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new AnimationPointerImportContext();
        }
    }

    public class AnimationPointerImportContext: GLTFImportPluginContext
    {
        public MaterialPropertiesRemapper materialPropertiesRemapper = new DefaultMaterialPropertiesRemapper();
    }
}