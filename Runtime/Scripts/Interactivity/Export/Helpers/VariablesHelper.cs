using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class VariablesHelpers
    {
        public static GltfInteractivityExportNode GetVariable(INodeExporter exporter, int id, out ValueOutRef value)
        {
            var node = exporter.CreateNode<Variable_GetNode>();
            
            var varType = exporter.Context.variables[id].Type;
            node.OutputValueSocket[Variable_GetNode.IdOutputValue].expectedType = ExpectedType.GtlfType(varType);
            node.Configuration["variable"].Value = id;
            value = node.FirstValueOut();
            return node;
        }
        
        public static GltfInteractivityExportNode SetVariable(INodeExporter exporter, int id)
        {
            var node = exporter.CreateNode<Variable_SetNode>();
            node.Configuration["variable"].Value = id;
            node.ValueIn(Variable_SetNode.IdInputValue).SetType(TypeRestriction.LimitToType(exporter.Context.variables[id].Type));
            return node;
        }

        public static GltfInteractivityExportNode SetVariableStaticValue(INodeExporter exporter, int id, object value, out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            var node = exporter.CreateNode<Variable_SetNode>();
            
            var variableType = exporter.Context.variables[id].Type;
            flowIn = node.FlowIn(Variable_SetNode.IdFlowIn);
            flowOut = node.FlowOut(Variable_SetNode.IdFlowOut);
            node.ValueIn(Variable_SetNode.IdInputValue).SetValue(value).SetType(TypeRestriction.LimitToType(variableType));

            node.Configuration["variable"].Value = id;
            return node;
        }

        public static GltfInteractivityExportNode SetVariable(INodeExporter exporter, int id, ValueOutRef valueSource,
            FlowOutRef fromFlow, FlowInRef targetFlowIn = null)
        {
            var node = SetVariable(exporter, id, out var value, out var flowIn, out var flowOut);
            value.ConnectToSource(valueSource); 
            fromFlow.ConnectToFlowDestination(flowIn);
            if (targetFlowIn != null) 
                flowOut.ConnectToFlowDestination(targetFlowIn);
            
            return node;
        }
        
        public static GltfInteractivityExportNode SetVariable(INodeExporter exporter, int id, out ValueInRef value, out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            var node = exporter.CreateNode<Variable_SetNode>();
            
            var variableType = exporter.Context.variables[id].Type;
            flowIn = node.FlowIn(Variable_SetNode.IdFlowIn);
            flowOut = node.FlowOut(Variable_SetNode.IdFlowOut);

            value = node.ValueIn(Variable_SetNode.IdInputValue).SetType(TypeRestriction.LimitToType(variableType));
            node.Configuration["variable"].Value = id;
            
            return node;
        }
        
    }
}