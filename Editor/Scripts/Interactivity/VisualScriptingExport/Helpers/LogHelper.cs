using System;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Interactivity.VisualScripting.Export;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public static class LogHelper
    {
        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }
        
        public static void AddLog(UnitExporter unitExporter, LogLevel level, ValueInput messageInput, ControlInput enter, ControlOutput exit)
        {
            var addGltfLog = unitExporter.exportContext.plugin.debugLogSetting.GltfLog;
            var addBabylon = unitExporter.exportContext.plugin.debugLogSetting.BabylonLog;
            var addADBE = unitExporter.exportContext.plugin.debugLogSetting.ADBEConsole;

            if (!addBabylon && !addADBE && !addGltfLog)
            {
                UnitExportLogging.AddWarningLog(unitExporter.unit, "No debug log output selected for Debug.Log unit. Skipping export. See Project Settings > UnityGltf");
                return;
            }
            
            var sequence_node = unitExporter.CreateNode(new Flow_SequenceNode());
           
            if (addGltfLog)
            {
                string messagePrefix = "";
                switch (level)
                {
                    case LogLevel.Warning:
                        messagePrefix = "Warning: ";
                        break;
                    case LogLevel.Error:
                        messagePrefix = "Error: ";
                        break;
                }
        
                var gltf_Node = unitExporter.CreateNode(new Debug_LogNode());
                if (unitExporter.IsInputLiteralOrDefaultValue(messageInput, out var message))
                {
                    gltf_Node.Configuration[Debug_LogNode.IdConfigMessage].Value = messagePrefix + message;
                }
                else
                {
                    gltf_Node.Configuration[Debug_LogNode.IdConfigMessage].Value = messagePrefix + "{0}";
                    gltf_Node.ValueIn("0").MapToInputPort(messageInput);
                }
                
                sequence_node.FlowOut("0").ConnectToFlowDestination(gltf_Node.FlowIn(Debug_LogNode.IdFlowIn));
            }
            
            if (addADBE)
            {
                var adbe_node = unitExporter.CreateNode(new ADBE_OutputConsoleNode());

                adbe_node.ValueIn(ADBE_OutputConsoleNode.IdMessage).MapToInputPort(messageInput);
                sequence_node.FlowOut("1").ConnectToFlowDestination(adbe_node.FlowIn(ADBE_OutputConsoleNode.IdFlowIn));

                unitExporter.exportContext.exporter.DeclareExtensionUsage(ADBE_OutputConsoleNode.EXTENSION_ID, false);
            }

            if (addBabylon)
            {
                var babylon_node = unitExporter.CreateNode(new Babylon_LogNode());
             
                babylon_node.ValueIn(Babylon_LogNode.IdMessage).MapToInputPort(messageInput);
                sequence_node.FlowOut("2").ConnectToFlowDestination(babylon_node.FlowIn(Babylon_LogNode.IdFlowIn));
    
                unitExporter.exportContext.exporter.DeclareExtensionUsage(Babylon_LogNode.EXTENSION_ID, false);
            }
            
            sequence_node.FlowOut("9").MapToControlOutput(exit);
            sequence_node.FlowIn(Flow_SequenceNode.IdFlowIn).MapToControlInput(enter);
        }
    }
}