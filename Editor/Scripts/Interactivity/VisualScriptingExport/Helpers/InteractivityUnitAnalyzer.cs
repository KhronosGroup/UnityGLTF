using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.VisualScripting.Export;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.VisualScripting
{
    [Analyser(typeof(Unit))] 
    [Analyser(typeof(InvokeMember))] 
    [Analyser(typeof(GetMember))] 
    [Analyser(typeof(SetMember))] 
    [UsedImplicitly]
    public class InteractivityUnitAnalyzer: UnitAnalyser<IUnit>
    {
        private GLTFSettings gltfSettings;
        private GLTFExportPlugin interactivityPlugin;
        
        public InteractivityUnitAnalyzer(GraphReference reference, IUnit target) : base(reference, target)
        {
            TypeCache.GetTypesDerivedFrom<IUnitExporter>();
        }

        private bool InteractivityPluginEnabled()
        {
            if (!gltfSettings)
            {
                gltfSettings = GLTFSettings.GetOrCreateSettings();
                interactivityPlugin = null;
            }

            if (!interactivityPlugin)
            {
                var plugin = gltfSettings.ExportPlugins.FirstOrDefault(p => p.GetType().Name.Contains("VisualScriptingExportPlugin"));
                if (plugin != null)
                    interactivityPlugin = plugin;
            }

            if (interactivityPlugin)
                return interactivityPlugin.Enabled;
            else
                return false;
        }

        protected override IEnumerable<Warning> Warnings()
        {
            if (!InteractivityPluginEnabled())
                yield break;
            
            foreach (var baseWarning in base.Warnings())
            {
                yield return baseWarning;
            }
            
            // These are exported implicitly, so let's not warn.
            // TODO for some types we might want to warn if we don't support them altogether
            if (target is Literal || target is This || target is Null)
                yield break;
            
            if (!UnitExporterRegistry.HasUnitExporter(target))
                yield return Warning.Error("Node will not be exported with KHR_interactivity");
            else
            {
                string infoFeedback = "";
                string warningFeedback = "";
                string errorFeedback = "";
                var exporter = UnitExporterRegistry.GetUnitExporter(target);
                if (exporter is IUnitExporterFeedback unitWithFeedback)
                {
                    var unitFeedback = unitWithFeedback.GetFeedback(target);
                    if (unitFeedback != null)
                    {
                        if (unitFeedback.HasInfos())
                            infoFeedback += System.Environment.NewLine + System.Environment.NewLine + unitFeedback.GetInfosAsString();
                        if (unitFeedback.HasErrors())
                            errorFeedback = unitFeedback.GetErrorsAsString();
                        if (unitFeedback.HasWarnings())
                            warningFeedback = unitFeedback.GetWarningsAsString();
                    }
                }
                    
                    
                string[] supportedMembers = null;
                
                if (target is Expose expose)
                    supportedMembers = ExposeUnitExport.GetSupportedMembers(expose.type);
                
                if (supportedMembers != null)
                    yield return Warning.Info("Node will be exported with KHR_interactivity." 
                                              +  System.Environment.NewLine 
                                              + "Supported members:"
                                              + System.Environment.NewLine + "•"
                                              + string.Join(System.Environment.NewLine+ "•"+infoFeedback, supportedMembers));
                else
                {
                    yield return Warning.Info("Node will be exported with KHR_interactivity"+infoFeedback);
                }

                if (!string.IsNullOrEmpty(warningFeedback))
                    yield return Warning.Caution(warningFeedback);
                if (!string.IsNullOrEmpty(errorFeedback))
                    yield return Warning.Error(errorFeedback);
            }

            if (UnitExportLogging.unitLogMessages.TryGetValue(target, out var logMessages))
            {
                if (logMessages.HasWarnings())
                    yield return Warning.Caution($"Last Gltf Export Warnings: {System.Environment.NewLine} {logMessages.GetWarningsAsString()}");
                else if (logMessages.HasErrors())
                    yield return Warning.Error($"Last Gltf Export Errors: {System.Environment.NewLine} {logMessages.GetErrorsAsString()}");
                
            }
            
        }
    }
}