using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.Playback
{
    public class InteractivityExportPlugin : GLTFExportPlugin
    {
        public override string DisplayName => "KHR_interactivity_Exporter";
        public override string Description => "Exports KHR compliant interactivity graphs";

        private InteractivityExportContext _context;
        public readonly KHR_interactivity extensionData;

        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            _context = new InteractivityExportContext();

            return _context;
        }
    }
}
