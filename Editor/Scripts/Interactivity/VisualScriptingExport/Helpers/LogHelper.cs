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
    
            var gltf_Node = unitExporter.CreateNode<Debug_LogNode>();
            if (unitExporter.IsInputLiteralOrDefaultValue(messageInput, out var message))
            {
                gltf_Node.Configuration[Debug_LogNode.IdConfigMessage].Value = messagePrefix + message;
            }
            else
            {
                gltf_Node.Configuration[Debug_LogNode.IdConfigMessage].Value = messagePrefix + "{0}";
                gltf_Node.ValueIn("0").MapToInputPort(messageInput);
            }
            
            gltf_Node.FlowIn().MapToControlInput(enter);
            gltf_Node.FlowOut().MapToControlOutput(exit);
        }
    }
}