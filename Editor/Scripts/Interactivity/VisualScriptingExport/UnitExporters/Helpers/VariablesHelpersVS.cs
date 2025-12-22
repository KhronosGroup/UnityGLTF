using Unity.VisualScripting;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class VariablesHelpersVS : VariablesHelpers
    {
        public static GltfInteractivityExportNode SetVariableStaticValue(INodeExporter unitExporter, int id, object value, ControlInput flowIn, ControlOutput flowOut)
        {
            var node = SetVariableStaticValue(unitExporter, id, value, out var flowInSocket, out var flowOutScoket); 
            flowInSocket.MapToControlInput(flowIn);
            flowOutScoket.MapToControlOutput(flowOut);
            return node;
        }
        
        public static GltfInteractivityExportNode SetVariable(INodeExporter unitExporter, int id, ValueInput value, ControlInput flowIn, ControlOutput flowOut)
        {
            var node = SetVariable(unitExporter, id, out var valueSocket, out var flowInSocket, out var flowOutSocket);
            flowInSocket.MapToControlInput(flowIn);
            flowOutSocket.MapToControlOutput(flowOut);
            valueSocket.MapToInputPort(value);
            
            return node;
        }
        
        public static GltfInteractivityExportNode SetVariable(INodeExporter unitExporter, int id, ValueOutRef valueSource, ControlInput flowIn, ControlOutput flowOut)
        {
            var node = SetVariable(unitExporter, id, out var valueSocket, out var flowInSocket, out var flowOutSocket);
            flowInSocket.MapToControlInput(flowIn);
            flowOutSocket.MapToControlOutput(flowOut);
            valueSocket.ConnectToSource(valueSource);
            return node;
        }
        
    }
}   