using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class CoroutineHelper
    {
        public static CoroutineAwaiterNode AddCoroutineAwaiter(UnitExporter unitExporter, GltfInteractivityNode node,  string outFlowSocket)
        {
            return new CoroutineAwaiterNode(unitExporter, node, outFlowSocket);
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
            var nodes = unitExporter.vsExportContext.Nodes;

            Queue<GltfInteractivityNode> queue = new Queue<GltfInteractivityNode>();
            var visited = new HashSet<int>();
            
            queue.Enqueue(coroutineNode);
            visited.Add(coroutineNode.Index);
            
            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();
                
                foreach (var node in nodes)
                foreach (var flow in node.FlowConnections)
                {
                    if (flow.Value == null || flow.Value.Node == null)
                        continue;
                    
                    if (flow.Value.Node == currentNode.Index && flow.Value.Socket == "in")
                    {
                        if (coroutineNode != node && node is GltfInteractivityUnitExporterNode exporterNode)
                        {
                            if (exporterNode.Exporter != coroutineNode.Exporter)
                            if (exporterNode.Exporter.exporter is ICoroutineAwaiter coroutineAwaiter)
                            {
                                var awaiter = CoroutineAwaiterNode.GetAwaiterNode(exporterNode.Exporter, node, flow.Key);
                                if (awaiter != null)
                                    return awaiter;
                            }
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
            public GltfInteractivityNode OutFlowNode;
            
            public static CoroutineAwaiterNode GetAwaiterNode(UnitExporter unitExporter, GltfInteractivityNode fromNode, string outFlowSocket)
            {
                foreach (var node in unitExporter.Nodes)
                        if (node is CoroutineAwaiterNode awaiterNode && awaiterNode.OutFlowSocketTarget == outFlowSocket && awaiterNode.OutFlowNode == fromNode)
                        return awaiterNode;
                return null;
            }
            
            public CoroutineAwaiterNode(UnitExporter exporter, GltfInteractivityNode node, string outFlowSocketTarget) : base(exporter, new Flow_WaitAllNode())
            {
                this.OutFlowNode = node;
                this.OutFlowSocketTarget = outFlowSocketTarget;
                exporter.AddCustomNode(this);
                Configuration[Flow_WaitAllNode.IdConfigInputFlows].Value = 0;
            }

            public FlowOutRef FlowOutDoneSocket()
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
                
                var value = Configuration[Flow_WaitAllNode.IdConfigInputFlows].Value;
                if (value == null)
                    value = 1;
                else
                    value = (int)value + 1;

                int currentIndex = (int)value - 1;
                
                Configuration[Flow_WaitAllNode.IdConfigInputFlows].Value = value;
                
                var sequence = unitExporter.CreateNode<Flow_SequenceNode>();
                if (unitExporter.vsExportContext.Nodes.Count > 0)
                {
                    unitExporter.vsExportContext.Nodes.Add(sequence);
                    sequence.Index = unitExporter.vsExportContext.Nodes.Count - 1;

                    var sourceSocket = node.FlowConnections[socket];
                    
                    sequence.FlowConnections.Add("a", new FlowSocketData { Socket = sourceSocket.Socket, Node = sourceSocket.Node});
                    sequence.FlowConnections.Add("b", new FlowSocketData { Socket = currentIndex.ToString(), Node = Index});
                    
                    node.FlowConnections[socket].Node = sequence.Index;
                    node.FlowConnections[socket].Socket = Flow_SequenceNode.IdFlowIn;
                }
                else
                {
                    //??
                }
                
            }
        }
    }
}