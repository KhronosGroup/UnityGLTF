using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Interactivity.VisualScripting.Export;

namespace Editor.UnitExporters.GltfInteractivityUnits
{
    public class GltfLogUnitExporter : IUnitExporter
    {
        public Type unitType { get => typeof(DebugLogGltf); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new GltfLogUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as DebugLogGltf;
            
            var logNode = unitExporter.CreateNode(new Debug_LogNode());
            logNode.FlowIn(Debug_LogNode.IdFlowIn).MapToControlInput(unit.enter);
            logNode.FlowOut(Debug_LogNode.IdFlowOut).MapToControlOutput(unit.exit);

            string msgPrefix = "";
            switch (unit.logVerbosity)
            {
                case DebugLogGltf.LogVerbosity.Warning:
                    msgPrefix = "Warning: ";
                    break;
                case DebugLogGltf.LogVerbosity.Error:
                    msgPrefix = "Error: ";
                    break;
            }
            
            logNode.Configuration[Debug_LogNode.IdConfigMessage].Value = msgPrefix + unit.message;

            for (int i = 0; i < unit.argumentCount; i++)
            {
                logNode.ValueIn(i.ToString()).MapToInputPort(unit.argumentPorts[i]);
            }
            
            return true;
        }
    }
}