using Editor.UnitExporters;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class Debug_LogUnitExport : IUnitExporter, IUnitExporterFeedback
    {
        public System.Type unitType { get => typeof(InvokeMember); }
         
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Debug), nameof(Debug.Log),
                new Debug_LogUnitExport());
        }
        
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            
            var addBabylon = unitExporter.exportContext.plugin.debugLogSetting.BabylonLog;
            var addADBE = unitExporter.exportContext.plugin.debugLogSetting.ADBEConsole;

            if (!addBabylon && !addADBE)
            {
                UnitExportLogging.AddWarningLog(unit,"No debug log output selected for Debug.Log unit. Skipping export. See Project Settings > UnityGltf");
                return false;
            }
            
            var sequence_node = unitExporter.CreateNode(new Flow_SequenceNode());
            
            if (addADBE)
            {
                var adbe_node = unitExporter.CreateNode(new ADBE_OutputConsoleNode());
                unitExporter.MapInputPortToSocketName(unit.inputParameters[0], ADBE_OutputConsoleNode.IdMessage, adbe_node);

                var flowAdbe = new GltfInteractivityUnitExporterNode.FlowSocketData();
                sequence_node.FlowSocketConnectionData.Add("0", flowAdbe);
                unitExporter.MapOutFlowConnection(adbe_node, ADBE_OutputConsoleNode.IdFlowIn, sequence_node, "0");
    
                unitExporter.exportContext.exporter.DeclareExtensionUsage(ADBE_OutputConsoleNode.EXTENSION_ID, false);
            }

            if (addBabylon)
            {
                var babylon_node = unitExporter.CreateNode(new Babylon_LogNode());
             
                unitExporter.MapInputPortToSocketName(unit.inputParameters[0], Babylon_LogNode.IdMessage, babylon_node);

                unitExporter.MapOutFlowConnection(babylon_node, Babylon_LogNode.IdFlowIn, sequence_node, "1");
                var flowBabylon = new GltfInteractivityUnitExporterNode.FlowSocketData();
                sequence_node.FlowSocketConnectionData.Add("1", flowBabylon);
    
                unitExporter.exportContext.exporter.DeclareExtensionUsage(Babylon_LogNode.EXTENSION_ID, false);
            }
            
            var flowExit = new GltfInteractivityUnitExporterNode.FlowSocketData();
            sequence_node.FlowSocketConnectionData.Add("2", flowExit);
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, "2", sequence_node);
            
            unitExporter.MapInputPortToSocketName(unit.enter, Flow_SequenceNode.IdFlowIn, sequence_node);
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