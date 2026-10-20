using Newtonsoft.Json.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    ///
    public class VisualScriptingExportPlugin: GLTFExportPlugin
    {
#if !UNITY_EDITOR
        public override bool Enabled => false;
#endif
        
        public override bool EnabledByDefault => true;

#if !HAVE_VISUAL_SCRIPTING   
        public override bool PackageMissing => true;
#endif
        
        public override string DisplayName => "KHR_interactivity (VisualScripting)";
        public override string Description => "Exports flow graph data for Visual Scripting ScriptMachines.";
        public override string Warning => "When exporting with interactivity (active Script Machines in the exported objects), KHR_node_visibility, KHR_animation_pointer and \"Export Disabled GameObjects\" are force enabled.";
 
#if HAVE_VISUAL_SCRIPTING
        [Header("This settings should only be disabled for debugging purposes.")]
        public bool cleanUpAndOptimizeExportedGraph = true;
#endif
        
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
#if UNITY_EDITOR && HAVE_VISUAL_SCRIPTING
            // Only exports with graphs need these, other exports keep the export settings
            if (VisualScriptingExportContext.HasScriptMachinesToExport(context.Exporter?.RootTransforms))
                UnityGLTF.Interactivity.Export.InteractivityExportContext.ForceRequiredExportSettings(context);

            var newContext = new VisualScriptingExportContext();
            newContext.cleanUpAndOptimizeExportedGraph = cleanUpAndOptimizeExportedGraph;
            return newContext;
#else
            Debug.LogWarning("Visual Scripting export is only supported in the Unity Editor.");
            return null;
#endif
        }
        

    }
}
