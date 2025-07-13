using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class GltfLogUnitExporter : IUnitExporter, IUnitExporterFeedback
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
            
            var logNode = unitExporter.CreateNode<Debug_LogNode>();
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

        public UnitLogs GetFeedback(IUnit unit)
        {
            var unitLog = new UnitLogs();
            
            var log = unit as DebugLogGltf;
            var msg = log.message;
            
            int paramCount = 0;
            int index = 0;
            while ((index = msg.IndexOf('{', index)) != -1)
            {
                int indexEnd = msg.IndexOf('}', index);
                if (indexEnd == -1)
                {
                    unitLog.errors.Add("Invalid Format: Missing closing brace for parameter at index " + index);
                    return unitLog;
                }
                
                string param = msg.Substring(index + 1, indexEnd - index - 1); 
                if (!int.TryParse(param, out int paramIndex) || paramIndex < 0)
                {
                    unitLog.errors.Add("Invalid Format: Parameter index must be a non-negative integer at index " + index);
                    return unitLog;
                }
                
                if (paramIndex >= log.argumentCount)
                {
                    unitLog.errors.Add($"Invalid Format: Parameter index {paramIndex} exceeds argument count {log.argumentCount}.");
                    return unitLog;
                }
                index++;
            }
            
            return unitLog;
        }
    }
}