using Unity.VisualScripting;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class VariablesHelpersVS : VariablesHelpers
    {
        public static GltfInteractivityExportNode SetVariableStaticValue(INodeExporter unitExporter, int id, object value, ControlInput flowIn, ControlOutput flowOut)
        {
            var node = unitExporter.CreateNode<Variable_SetNode>();
            
            var variableType = unitExporter.Context.variables[id].Type;
            node.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(flowIn);
            node.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(flowOut);
            node.ValueIn(Variable_SetNode.IdInputValue).SetValue(value).SetType(TypeRestriction.LimitToType(variableType));

            node.Configuration["variable"].Value = id;
            return node;
        }
        
        
        public static GltfInteractivityExportNode SetVariable(INodeExporter unitExporter, int id, ValueInput value, ControlInput flowIn, ControlOutput flowOut)
        {
            var node = unitExporter.CreateNode<Variable_SetNode>();
            
            var variableType = unitExporter.Context.variables[id].Type;
            node.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(flowIn);
            node.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(flowOut);
            node.ValueIn(Variable_SetNode.IdInputValue).MapToInputPort(value).SetType(TypeRestriction.LimitToType(variableType));

            node.Configuration["variable"].Value = id;
            return node;
        }
        
        public static GltfInteractivityExportNode SetVariable(INodeExporter unitExporter, int id, ValueOutRef valueSource, ControlInput flowIn, ControlOutput flowOut)
        {
            var node = unitExporter.CreateNode<Variable_SetNode>();
            
            var variableType = unitExporter.Context.variables[id].Type;
            node.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(flowIn);
            node.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(flowOut);
            node.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(valueSource)
                .SetType(TypeRestriction.LimitToType(variableType));
            node.Configuration["variable"].Value = id;
            return node;
        }
        
    }
}   