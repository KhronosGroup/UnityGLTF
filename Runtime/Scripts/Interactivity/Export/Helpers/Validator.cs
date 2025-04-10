using System.Text;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public static class Validator
    {
        public static void ValidateData(InteractivityExportContext context)
        {
            var sb = new StringBuilder();
            
            void NodeAppendLine(GltfInteractivityNode node, string message)
            {
                if (node.Configuration != null)
                {
                    foreach (var config in node.Configuration)
                        if (config.Value == null)
                            message += $" (config: {config.Key} has no Value)";
                        else
                            message += $" (config: {config.Key}={config.Value.Value})";
                }
                
                sb.AppendLine($"Node Index {node.Index} with Schema={node.Schema.Op}: {message}");
            }
            
            foreach (var node in context.Nodes)
            {
                foreach (var config in node.Configuration)
                {
                    if (config.Value.Value == null)
                    {
                        NodeAppendLine(node, $"Configuration with <{config.Key}> has no Value");
                    }
                }
                
                foreach (var valueSocket in node.ValueInConnection)
                {
                    if (valueSocket.Value.Node == null)
                    {
                        if (valueSocket.Value.Value == null)
                            NodeAppendLine(node, $"Socket <{valueSocket.Key}> has no connection and no Value");
                        else if (valueSocket.Value.Type == -1)
                            NodeAppendLine(node, $"Socket <{valueSocket.Key}> has invalid Type (-1). Value-Type: {valueSocket.Value.Value.GetType().Name}");
                    }
                    else if (valueSocket.Value.Node == -1)
                    {
                        NodeAppendLine(node, $"Socket <{valueSocket.Key}> has invalid Node Index (-1)");
                    }
                }
                
                foreach (var flowSocket in node.FlowConnections)
                {
                    if (flowSocket.Value.Node == -1)
                    {
                        NodeAppendLine(node, $"Flow Socket <{flowSocket.Key}> has invalid Node Index (-1)");
                    }
                }

                if (node.Schema.Op == "pointer/set" || node.Schema.Op == "pointer/get")
                {
                    if (node.ValueInConnection.TryGetValue(PointersHelper.IdPointerNodeIndex, out var valueSocket))
                    {
                        if (valueSocket.Value != null && valueSocket.Value.GetType() != typeof(int))
                            NodeAppendLine(node, $"Node Pointer Node has invalid nodeIndex Type: {valueSocket.Value.GetType().Name}");
                        else
                        if (valueSocket.Value != null && valueSocket.Node == null && (int)valueSocket.Value == -1)
                            NodeAppendLine(node, $"Node Pointer Node has invalid nodeIndex Value: -1");
                    }
                }
                
                if (node.Schema.Op == "variable/set" || node.Schema.Op == "variable/get")
                {
                    if (node.Configuration.TryGetValue(Variable_SetNode.IdConfigVarIndex, out var varConfig))
                    {
                        if (varConfig.Value == null)
                            NodeAppendLine(node, $"Variable Node has no Variable Index");
                        if (varConfig.Value != null && (int)varConfig.Value == -1)
                            NodeAppendLine(node, $"Variable Node has invalid Variable Index: -1");
                    }
                }

                if (node.Schema.Op == "event/receive" || node.Schema.Op == "event/send")
                {
                    if (node.Configuration.TryGetValue("event", out var varConfig))
                    {
                        if (varConfig.Value == null)
                            NodeAppendLine(node, $"Event Node has no Event Index");
                        if (varConfig.Value != null && (int)varConfig.Value == -1)
                            NodeAppendLine(node, $"Event Node has invalid Event Index: -1");
                    }
                }
            }
            
            foreach (var variable in context.variables)
            {
                if (variable.Type == -1)
                    sb.AppendLine($"Variable with Id >{variable.Id}< has invalid Type (-1)");
            }
            
            foreach (var customEvent in context.customEvents)
            {
                foreach (var customEventValue in customEvent.Values)
                {
                    if (customEventValue.Value.Type == -1)
                        sb.AppendLine($"Custom Event with Id >{customEvent.Id}< with Value >{customEventValue.Key}< has invalid Value Type (-1)");
                }
            }
            
            if (sb.Length == 0)
                return;
            
            Debug.LogError($"Validation Errors Found: "+ System.Environment.NewLine + sb.ToString());
        }
    }
}