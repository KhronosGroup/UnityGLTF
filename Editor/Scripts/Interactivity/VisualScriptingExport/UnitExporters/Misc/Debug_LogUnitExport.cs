using Editor.UnitExporters;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Debug_LogUnitExport : IUnitExporter, IUnitExporterFeedback
    {
        public System.Type unitType { get => typeof(InvokeMember); }
         
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Debug), nameof(Debug.Log),
                new Debug_LogUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Debug), nameof(Debug.LogWarning),
                new Debug_LogUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Debug), nameof(Debug.LogError),
                new Debug_LogUnitExport());
        }
        
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            bool isWarning = unit.member.name == nameof(Debug.LogWarning);
            bool isError = unit.member.name == nameof(Debug.LogError);
            
            var addGltfLog = unitExporter.exportContext.plugin.debugLogSetting.GltfLog;
            var addBabylon = unitExporter.exportContext.plugin.debugLogSetting.BabylonLog;
            var addADBE = unitExporter.exportContext.plugin.debugLogSetting.ADBEConsole;

            if (!addBabylon && !addADBE && !addGltfLog)
            {
                UnitExportLogging.AddWarningLog(unit,"No debug log output selected for Debug.Log unit. Skipping export. See Project Settings > UnityGltf");
                return false;
            }
            
            var sequence_node = unitExporter.CreateNode(new Flow_SequenceNode());
           
            if (addGltfLog)
            {
                string messagePrefix = "";
                if (isWarning)
                    messagePrefix = "Warning: ";
                else if (isError)
                    messagePrefix = "Error: ";

                var gltf_Node = unitExporter.CreateNode(new Debug_LogNode());
                if (unitExporter.IsInputLiteralOrDefaultValue(unit.inputParameters[0], out var message))
                {
                    gltf_Node.ConfigurationData[Debug_LogNode.IdConfigMessage].Value = messagePrefix + message;
                }
                else
                {
                    gltf_Node.ConfigurationData[Debug_LogNode.IdConfigMessage].Value = messagePrefix + "{0}";
                    gltf_Node.ValueIn("0").MapToInputPort(unit.inputParameters[0]);
                }
                
                sequence_node.FlowOut("0").ConnectToFlowDestination(gltf_Node.FlowIn(Debug_LogNode.IdFlowIn));
            }
            
            if (addADBE)
            {
                var adbe_node = unitExporter.CreateNode(new ADBE_OutputConsoleNode());

                adbe_node.ValueIn(ADBE_OutputConsoleNode.IdMessage).MapToInputPort(unit.inputParameters[0]);
                sequence_node.FlowOut("1").ConnectToFlowDestination(adbe_node.FlowIn(ADBE_OutputConsoleNode.IdFlowIn));

                unitExporter.exportContext.exporter.DeclareExtensionUsage(ADBE_OutputConsoleNode.EXTENSION_ID, false);
            }

            if (addBabylon)
            {
                var babylon_node = unitExporter.CreateNode(new Babylon_LogNode());
             
                babylon_node.ValueIn(Babylon_LogNode.IdMessage).MapToInputPort(unit.inputParameters[0]);
                sequence_node.FlowOut("2").ConnectToFlowDestination(babylon_node.FlowIn(Babylon_LogNode.IdFlowIn));
    
                unitExporter.exportContext.exporter.DeclareExtensionUsage(Babylon_LogNode.EXTENSION_ID, false);
            }
            
            sequence_node.FlowOut("9").MapToControlOutput(unit.exit);
            sequence_node.FlowIn(Flow_SequenceNode.IdFlowIn).MapToControlInput(unit.enter);
   
            return true;
        }

        public UnitLogs GetFeedback(IUnit unit)
        {
            var logs = new UnitLogs();
            logs.infos.Add("See Project Settings > UnityGltf for debug log output settings.");
            return logs;
        }
    }
}