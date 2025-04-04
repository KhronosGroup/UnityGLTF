namespace UnityGLTF.Plugins
{
    public class AudioImport : GLTFImportPlugin
    {
        public override string DisplayName => "KHR_audio";
        public override string Description => "Import positional and global audio sources and .mp3 audio clips.";
        
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new AudioImportContext(context);
        }

    }
    
    public class AudioImportContext : GLTFImportPluginContext
    {
        private GLTFImportContext _context;
        
        public AudioImportContext(GLTFImportContext context) 
        {
            _context = context;
        }
    }
}