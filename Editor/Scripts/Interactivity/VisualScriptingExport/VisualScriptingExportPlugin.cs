using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
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
            [Header("Which Gltf Interactivity Log should be used")]
            public bool GltfLog = true;
            public bool ADBEConsole = true;
            public bool BabylonLog = false;
        }

        public override JToken AssetExtras 
        { 
            get => new JObject(
                    new JProperty("Spec.Version URL", "https://github.com/KhronosGroup/glTF/blob/d9bfdb08f0c09c125f588783921d9edceb7ee78c/extensions/2.0/Khronos/KHR_interactivity/Specification.adoc"),
                    new JProperty("Spec.Version Date", "2025-03-10"));
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
