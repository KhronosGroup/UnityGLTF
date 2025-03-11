using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport
{
    public static class CoroutineHelper
    {
        public static CoroutineAwaiterNode AddCoroutineAwaiter(UnitExporter unitExporter, string outFlowSocket)
        {
            return new CoroutineAwaiterNode(unitExporter, outFlowSocket);
        }

        public static bool CoroutineRequired(Unit unit)
        {
            var  visited = new HashSet<IUnit>();
            visited.Add(unit);
            
            var queue = new Queue<IUnit>();
            queue.Enqueue(unit);
            
            while (queue.Count > 0)
            {
                var currentUnit = queue.Dequeue();

                if (currentUnit.controlInputs.Any(input => input.requiresCoroutine))
                    return true;
                
                foreach (var output in currentUnit.controlOutputs.Where( c => c.hasValidConnection))
                {
                    if (!visited.Contains(output.connection.destination.unit))
                    {
                        visited.Add(output.connection.destination.unit);   
                        queue.Enqueue(output.connection.destination.unit);
                    }
                }
            }

            return false;
        }
        
        public static CoroutineAwaiterNode FindCoroutineAwaiter(UnitExporter unitExporter, GltfInteractivityUnitExporterNode coroutineNode)
        {
            var nodes = unitExporter.exportContext.Nodes;

            Queue<GltfInteractivityNode> queue = new Queue<GltfInteractivityNode>();
            var visited = new HashSet<int>();
            
            queue.Enqueue(coroutineNode);
            visited.Add(coroutineNode.Index);
            
            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();
                
                foreach (var node in nodes)
                foreach (var flow in node.FlowSocketConnectionData)
                {
                    if (flow.Value == null || flow.Value.Node == null)
                        continue;
                    
                    if (flow.Value.Node == currentNode.Index && flow.Value.Socket == "in")
                    {
                        if (coroutineNode != node && node is GltfInteractivityUnitExporterNode exporterNode)
                        {
                            if (exporterNode.Exporter.exporter is ICoroutineAwaiter coroutineAwaiter)
                                return CoroutineAwaiterNode.GetAwaiterNode(exporterNode.Exporter, flow.Key);
                        }
                        
                        if (!visited.Contains(node.Index))
                        {
                            visited.Add(node.Index);
                            queue.Enqueue(node);
                        }
                        
                        break;
                    }
                }
            }

            return null;
        }

        public class CoroutineAwaiterNode : GltfInteractivityUnitExporterNode
        {
            public string OutFlowSocketTarget = "";

            public static CoroutineAwaiterNode GetAwaiterNode(UnitExporter unitExporter, string outFlowSocket)
            {
                foreach (var node in unitExporter.Nodes)
                    if (node is CoroutineAwaiterNode awaiterNode && awaiterNode.OutFlowSocketTarget == outFlowSocket)
                        return awaiterNode;
                return null;
            }
            
            public CoroutineAwaiterNode(UnitExporter exporter, string outFlowSocketTarget) : base(exporter, new Flow_WaitAllNode())
            {
                this.OutFlowSocketTarget = outFlowSocketTarget;
                exporter.AddCustomNode(this);
                ConfigurationData[Flow_WaitAllNode.IdConfigInputFlows].Value = 0;
            }

            public FlowOutSocketData FlowOutDoneSocket()
            {
                return FlowOut(Flow_WaitAllNode.IdFlowOutCompleted);
            }

            public void AddCoroutineWait(UnitExporter unitExporter, GltfInteractivityNode node, string socket)
            {
                if (unitExporter.exporter is not ICoroutineWait coroutineWait)
                {
                    Debug.LogError("Exporter does not implement ICoroutineWait. Cannot add coroutine wait.");
                    return;
                }
                
                var value = ConfigurationData[Flow_WaitAllNode.IdConfigInputFlows].Value;
                if (value == null)
                    value = 1;
                else
                    value = (int)value + 1;

                int currentIndex = (int)value - 1;
                
                ConfigurationData[Flow_WaitAllNode.IdConfigInputFlows].Value = value;
                
                var sequence = unitExporter.CreateNode(new Flow_SequenceNode());
                if (unitExporter.exportContext.Nodes.Count > 0)
                {
                    unitExporter.exportContext.Nodes.Add(sequence);
                    sequence.Index = unitExporter.exportContext.Nodes.Count - 1;

                    var sourceSocket = node.FlowSocketConnectionData[socket];
                    
                    sequence.FlowSocketConnectionData.Add("a", new FlowSocketData { Socket = sourceSocket.Socket, Node = sourceSocket.Node});
                    sequence.FlowSocketConnectionData.Add("b", new FlowSocketData { Socket = currentIndex.ToString(), Node = Index});
                    
                    node.FlowSocketConnectionData[socket].Node = sequence.Index;
                    node.FlowSocketConnectionData[socket].Socket = Flow_SequenceNode.IdFlowIn;
                }
                else
                {
                    //??
                }
                
            }
        }
    }
}