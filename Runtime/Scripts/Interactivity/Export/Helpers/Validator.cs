using System.Linq;
using System.Text;
using System.Text.RegularExpressions; // added for placeholder parsing
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public static class Validator
    {
        public static void ValidateData(InteractivityExportContext context)
        {
            var sb = new StringBuilder();
            int refTypeIndex = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Ref);
            
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
                
                sb.AppendLine($"Node Index {node.Index} with Schema={node.Schema.Op}: {message} "+node.AdditionalDebugString);
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
                    
                    if (valueSocket.Value.Value != null && valueSocket.Value.Type == refTypeIndex && valueSocket.Value.Value is string refString)
                    {
                        if (!string.IsNullOrEmpty(refString) && refString.EndsWith("/"))
                        {
                            NodeAppendLine(node, $"Socket <{valueSocket.Key}> has invalid Ref Value (ends with '/'): {refString}");
                        }
                    }
                    if (valueSocket.Value.Value != null && valueSocket.Value.Value is StaticRefPointer staticRefPointer)
                    {
                        if (!string.IsNullOrEmpty(staticRefPointer.pointer) && staticRefPointer.pointer.EndsWith("/"))
                        {
                            NodeAppendLine(node, $"Socket <{valueSocket.Key}> has invalid Ref Value (ends with '/'): {staticRefPointer.pointer}");
                        }

                        if (!string.IsNullOrEmpty(staticRefPointer.pointer) && !staticRefPointer.pointer.StartsWith("/"))
                        {
                            NodeAppendLine(node, $"Socket <{valueSocket.Key}> has invalid Ref Value (does not start with '/'): {staticRefPointer.pointer}");
                        }
                        
                        if (!string.IsNullOrEmpty(staticRefPointer.pointer) && staticRefPointer.pointer.Contains("//"))
                        {
                            NodeAppendLine(node, $"Socket <{valueSocket.Key}> has invalid Ref Value (contains '//'): {staticRefPointer.pointer}");
                        }
                    }
                }
                
                foreach (var flowSocket in node.FlowConnections)
                {
                    if (flowSocket.Value.Node == -1)
                    {
                        NodeAppendLine(node, $"Flow Socket <{flowSocket.Key}> has invalid Node Index (-1)");
                    }
                }

                if (node.Schema.Op == "debug/log")
                {
                    if (node.Configuration.ContainsKey(Debug_LogNode.IdConfigMessage))
                    {
                        if (node.Configuration[Debug_LogNode.IdConfigMessage].Value == null)
                            NodeAppendLine(node, $"Debug Log Node has no Message");
                        else
                        {
                            var templateStr = node.Configuration[Debug_LogNode.IdConfigMessage].Value;
                            // Extract placeholders in the form {placeholder} from the template string and validate they exist as value socket keys
                            if (templateStr is string template)
                            {
                                var matches = Regex.Matches(template, @"\{([^{}]+)\}");
                                foreach (Match m in matches)
                                {
                                    var placeholder = m.Groups[1].Value.Trim();
                                    if (string.IsNullOrEmpty(placeholder))
                                        continue; // ignore empty braces
                                    if (!node.ValueInConnection.ContainsKey(placeholder))
                                    {
                                        NodeAppendLine(node, $"Debug Log template placeholder '{{{placeholder}}}' has no matching ValueIn socket");
                                    }
                                    else
                                    {
                                        var socket = node.ValueInConnection[placeholder];
                                        if (socket.Node == null && socket.Value == null)
                                            NodeAppendLine(node, $"Debug Log template placeholder '{{{placeholder}}}' socket has neither connection nor default value");
                                    }
                                }
                            }
                            else
                            {
                                NodeAppendLine(node, $"Debug Log Node Message config is not a string (Type={templateStr.GetType().Name})");
                            }
                        }
                    }
                }

                if (node.Schema.Op == "pointer/set" || node.Schema.Op == "pointer/get" || node.Schema.Op == "pointer/interpolate")
                {
                    if (node.ValueInConnection.TryGetValue(PointersHelper.IdPointerNodeIndex, out var valueSocket))
                    {
                        if (valueSocket.Value != null && valueSocket.Value.GetType() != typeof(int))
                            NodeAppendLine(node, $"Node Pointer Node has invalid nodeIndex Type: {valueSocket.Value.GetType().Name}");
                        else
                        if (valueSocket.Value != null && valueSocket.Node == null && (int)valueSocket.Value == -1)
                            NodeAppendLine(node, $"Node Pointer Node has invalid nodeIndex Value: -1");
                    }
                    if (node.ValueInConnection.TryGetValue(PointersHelper.IdPointerNodeRef, out var valueSocketRef))
                    {
                        if (valueSocketRef.Value != null &&
                            GltfTypes.GetTypeMapping(valueSocketRef.Value.GetType())?.GltfSignature != GltfTypes.Ref)
                        {
                            if (valueSocketRef.Value is not string)
                                NodeAppendLine(node, $"Node Pointer Node has invalid nodeRef Type: {valueSocketRef.Value.GetType().Name}");
                        }
                        else
                        if (valueSocketRef.Value == null && valueSocketRef.Node == null )
                            NodeAppendLine(node, $"Node Pointer Node has no connection and NULL as nodeRef Value!");
                    }
                    if (node.Configuration.TryGetValue(Pointer_SetNode.IdPointer, out var templateConfig))
                    {
                        if (templateConfig.Value == null)
                            NodeAppendLine(node, $" {node.Schema.Op} Node has no pointer config value");
                        else if (templateConfig.Value != null && !(templateConfig.Value is string))
                            NodeAppendLine(node, $" {node.Schema.Op} Node has invalid Template Type. Should be a string. Current Type: {templateConfig.Value.GetType().Name}");
                        else if (templateConfig.Value is string configValue && !string.IsNullOrEmpty(configValue) && configValue.EndsWith("/"))
                            NodeAppendLine(node, $" {node.Schema.Op} Node has invalid Template Value (ends with '/'): {configValue}");
                        else if (templateConfig.Value is string configValue2 && !string.IsNullOrEmpty(configValue2) && !configValue2.StartsWith("/"))
                            NodeAppendLine(node, $" {node.Schema.Op} Node has invalid Template Value (starts with '/'): {configValue2}");
                        else if (templateConfig.Value is string configValue3 && !string.IsNullOrEmpty(configValue3) && configValue3.Contains("//"))
                            NodeAppendLine(node, $" {node.Schema.Op} Node has invalid Template Value (contains '//'): {configValue3}");
                        
                    }
                    else
                    {
                        NodeAppendLine(node, $" {node.Schema.Op} Node has no pointer config");
                    }
                    
                }
                
                if (node.Schema.Op == "variable/get")
                {
                    if (node.Configuration.TryGetValue(Variable_GetNode.IdConfigVarIndex, out var varConfig))
                    {
                        if (varConfig.Value == null)
                            NodeAppendLine(node, $"Variable Get Node has no Variable Index");
                        if (varConfig.Value != null && (int)varConfig.Value == -1)
                            NodeAppendLine(node, $"Variable Get Node has invalid Variable Index: -1");
                    }
                }
                
                if (node.Schema.Op == "variable/set")
                {
                    if (node.Configuration.TryGetValue(Variable_SetNode.IdConfigVarIndices, out var varConfig))
                    {
                        if (varConfig.Value == null)
                            NodeAppendLine(node, $"Variable Set Node has no Variable Index");
                        else
                        if (varConfig.Value != null && !(varConfig.Value is int[]))
                            NodeAppendLine(node, $"Variable Set Node has invalid config value type. Should be a int[]. Current Type: {varConfig.Value.GetType().Name}");
                        else
                        if (varConfig.Value != null && (varConfig.Value as int[]).Contains(-1))
                            NodeAppendLine(node, $"Variable Set Node contains invalid Variable Index: -1");
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