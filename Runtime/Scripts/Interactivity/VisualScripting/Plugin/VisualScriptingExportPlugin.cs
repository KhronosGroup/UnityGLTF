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
        // Disabled by default until Gltf Interactivity spec is ratified
        public override bool EnabledByDefault => false;

#if !HAVE_VISUAL_SCRIPTING   
        public override bool PackageMissing => true;
#endif
        
        public override string DisplayName => "KHR_interactivity (VisualScripting)";
        public override string Description => "Exports flow graph data for Visual Scripting ScriptMachines.";
 
#if HAVE_VISUAL_SCRIPTING
        [Header("This settings should only be disabled for debugging purposes.")]
        public bool cleanUpAndOptimizeExportedGraph = true;
#endif
        
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
#if UNITY_EDITOR && HAVE_VISUAL_SCRIPTING
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
