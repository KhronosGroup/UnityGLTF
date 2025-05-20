using UnityEditor;

#if !HAVE_VISUAL_SCRIPTING

namespace UnityGLTF.Interactivity.VisualScripting
{
    using UnityGLTF;
    using UnityGLTF.Plugins;

    /// <summary>
    /// Extends UnityGLTF with an extension for KHR_Interactivity Metadata
    ///
    /// See https://github.com/KhronosGroup/UnityGLTF?tab=readme-ov-file#extensibility
    /// for the external documentation on how to extend UnityGLTF.
    /// </summary>
    [NonRatifiedPlugin]
    public class VisualScriptingExportPlugin: GLTFExportPlugin
    {
        // Disabled by default until Gltf Interactivity spec is ratified
        public override bool EnabledByDefault => false;

        public override bool Enabled => false;
        
        public override string DisplayName => "KHR_Interactivity (VisualScripting)";
        public override string Description => "Exports flow graph data for Visual Scripting ScriptMachines.";

        public override bool PackageMissing => true;

        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return null;
        }
    }
    
        
    [CustomEditor(typeof(VisualScriptingExportPlugin))]
    internal class VisualScriptingExportEditor : PackageInstallEditor
    {
        protected override string PackageName => "com.unity.visualscripting";
    }
}
#endif
