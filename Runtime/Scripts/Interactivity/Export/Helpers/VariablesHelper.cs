using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class VariablesHelpers
    {
        public static GltfInteractivityExportNode GetVariable(INodeExporter exporter, int id, out ValueOutRef value)
        {
            var node = exporter.CreateNode(new Variable_GetNode());
            
            var varType = exporter.Context.variables[id].Type;
            node.OutputValueSocket[Variable_GetNode.IdOutputValue].expectedType = ExpectedType.GtlfType(varType);
            node.Configuration["variable"].Value = id;
            value = node.FirstValueOut();
            return node;
        }
        
        public static GltfInteractivityExportNode SetVariable(INodeExporter exporter, int id)
        {
            var node = exporter.CreateNode(new Variable_SetNode());
            node.Configuration["variable"].Value = id;
            node.ValueIn(Variable_SetNode.IdInputValue).SetType(TypeRestriction.LimitToType(exporter.Context.variables[id].Type));
            return node;
        }

        public static GltfInteractivityExportNode SetVariableStaticValue(INodeExporter exporter, int id, object value, out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            var node = exporter.CreateNode(new Variable_SetNode());
            
            var variableType = exporter.Context.variables[id].Type;
            flowIn = node.FlowIn(Variable_SetNode.IdFlowIn);
            flowOut = node.FlowOut(Variable_SetNode.IdFlowOut);
            node.ValueIn(Variable_SetNode.IdInputValue).SetValue(value).SetType(TypeRestriction.LimitToType(variableType));

            node.Configuration["variable"].Value = id;
            return node;
        }
        
        public static GltfInteractivityExportNode SetVariable(INodeExporter exporter, int id, ValueOutRef valueSource, FlowOutRef fromFlow, FlowInRef targetFlowIn = null)
        {
            var node = exporter.CreateNode(new Variable_SetNode());
            
            var variableType = exporter.Context.variables[id].Type;
            fromFlow.ConnectToFlowDestination(node.FlowIn(Variable_SetNode.IdFlowIn));
            
            if (targetFlowIn != null) 
                node.FlowOut(Variable_SetNode.IdFlowOut).ConnectToFlowDestination(targetFlowIn);

            node.ValueIn(Variable_SetNode.IdInputValue).ConnectToSource(valueSource)
                .SetType(TypeRestriction.LimitToType(variableType));
            node.Configuration["variable"].Value = id;
            return node;
        }
        
    }
}