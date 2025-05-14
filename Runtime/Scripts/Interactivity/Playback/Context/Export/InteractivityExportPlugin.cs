using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.Playback
{
    public class InteractivityExportPlugin : GLTFExportPlugin
    {
        public override string DisplayName => "KHR_interactivity Playback Exporter";
        public override string Description => "Allows the export of KHR compliant interactivity graphs at runtime.";

        private InteractivityExportContext _context;
        public readonly KHR_interactivity extensionData;

        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            _context = new InteractivityExportContext();

            return _context;
        }
    }
}
