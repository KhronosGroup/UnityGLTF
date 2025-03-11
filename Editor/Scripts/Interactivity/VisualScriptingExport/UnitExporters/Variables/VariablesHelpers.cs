using Unity.VisualScripting;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class VariablesHelpers
    {

        public static GltfInteractivityUnitExporterNode GetVariable(UnitExporter unitExporter, int id, out GltfInteractivityUnitExporterNode.ValueOutputSocketData value)
        {
            var node = unitExporter.CreateNode(new Variable_GetNode());
            
            var varType = unitExporter.exportContext.variables[id].Type;
            node.OutValueSocket[Variable_GetNode.IdOutputValue].expectedType = ExpectedType.GtlfType(varType);
            node.ConfigurationData["variable"].Value = id;
            value = node.FirstValueOut();
            return node;
        }

        public static GltfInteractivityUnitExporterNode SetVariableStaticValue(UnitExporter unitExporter, int id, object value, ControlInput flowIn, ControlOutput flowOut)
        {
            var node = unitExporter.CreateNode(new Variable_SetNode());
            
            var variableType = unitExporter.exportContext.variables[id].Type;
            node.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(flowIn);
            node.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(flowOut);
            node.ValueIn(Variable_SetNode.IdInputValue).SetValue(value);

            node.ConfigurationData["variable"].Value = id;
            return node;
        }
        
        public static GltfInteractivityUnitExporterNode SetVariable(UnitExporter unitExporter, int id)
        {
            var node = unitExporter.CreateNode(new Variable_SetNode());
            node.ConfigurationData["variable"].Value = id;
            return node;
        }
        
        public static GltfInteractivityUnitExporterNode SetVariable(UnitExporter unitExporter, int id, ValueInput value, ControlInput flowIn, ControlOutput flowOut)
        {
            var node = unitExporter.CreateNode(new Variable_SetNode());
            
            var variableType = unitExporter.exportContext.variables[id].Type;
            node.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(flowIn);
            node.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(flowOut);
            node.ValueIn(Variable_SetNode.IdInputValue).MapToInputPort(value);

            node.ConfigurationData["variable"].Value = id;
            return node;
        }
        
        public static GltfInteractivityUnitExporterNode SetVariable(UnitExporter unitExporter, int id, GltfInteractivityUnitExporterNode.ValueOutputSocketData valueSource, ControlInput flowIn, ControlOutput flowOut)
        {
            var node = unitExporter.CreateNode(new Variable_SetNode());
            
            var variableType = unitExporter.exportContext.variables[id].Type;
            node.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(flowIn);
            node.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(flowOut);
            node.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(valueSource);
            node.ConfigurationData["variable"].Value = id;
            return node;
        }
        
        public static GltfInteractivityUnitExporterNode SetVariable(UnitExporter unitExporter, int id, GltfInteractivityUnitExporterNode.ValueOutputSocketData valueSource, GltfInteractivityUnitExporterNode.FlowOutSocketData fromFlow, GltfInteractivityUnitExporterNode.FlowInSocketData targetFlowIn)
        {
            var node = unitExporter.CreateNode(new Variable_SetNode());
            
            var variableType = unitExporter.exportContext.variables[id].Type;
            fromFlow.ConnectToFlowDestination(node.FlowIn(Variable_SetNode.IdFlowIn));
            
            if (targetFlowIn != null) 
                node.FlowOut(Variable_SetNode.IdFlowOut).ConnectToFlowDestination(targetFlowIn);
            
            node.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(valueSource);
            node.ConfigurationData["variable"].Value = id;
            return node;
        }
        
    }
}   