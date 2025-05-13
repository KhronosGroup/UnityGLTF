using GLTF.Schema;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.Playback
{
    public class InteractivityImportPlugin : GLTFImportPlugin
    {
        public override string DisplayName => "KHR_interactivity_Importer";
        public override string Description => "Imports KHR compliant interactivity graphs for runtime playback.";

        private InteractivityImportContext _context;

        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            GLTFProperty.RegisterExtension(new InteractivityGraphFactory());
            _context = new InteractivityImportContext(this, context);

            return _context;
        }

    }
}
