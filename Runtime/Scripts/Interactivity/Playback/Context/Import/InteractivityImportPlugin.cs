using GLTF.Schema;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.Playback
{
    public class InteractivityImportPlugin : GLTFImportPlugin
    {
        public override string DisplayName => "KHR_interactivity_Importer";
        public override string Description => "Imports KHR compliant interactivity graphs";

        private InteractivityImportContext _context;

        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            _context = new InteractivityImportContext(this);
            GLTFProperty.RegisterExtension(new InteractivityGraphFactory());

            return _context;
        }

    }
}
