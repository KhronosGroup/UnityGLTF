using System;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

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
    public class VisualScriptingExportPlugin: GLTFExportPlugin
    {
        [Serializable]
        public class DebugLogSetting
        {
            public bool GltfLog = true;
            public bool ADBEConsole = true;
            public bool BabylonLog = false;
        }
        
        // Disabled by default until Gltf Interactivity spec is ratified
        public override bool EnabledByDefault => false;
        
        public override string DisplayName => GltfInteractivityExtension.ExtensionName + " (VisualScripting)";
        public override string Description => "Exports flow graph data for Visual Scripting ScriptMachines.";
        
        public DebugLogSetting debugLogSetting = new DebugLogSetting();

        [Header("This settings should only be disabled for debugging purposes.")]
        public bool cleanUpAndOptimizeExportedGraph = true;
        public bool addUnityToGltfSpaceConversion = true;
        
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return new VisualScriptingExportContext(this);
        }
    }
}
