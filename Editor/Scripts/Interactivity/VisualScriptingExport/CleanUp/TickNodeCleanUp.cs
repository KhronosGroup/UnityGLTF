using System.Linq;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Interactivity.VisualScripting.Export;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public class TickNodeCleanUp : ICleanUp
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new TickNodeCleanUp());
        }

        public void OnCleanUp(CleanUpTask task)
        {
            var nodes = task.context.Nodes;
            
            var tickNodes = nodes.FindAll(node => node.Schema is Event_OnTickNode).ToArray();
    
            if (tickNodes.Length <= 1)
                return;

            
            // Ensure the first OnTickNode is from a TimeUnitExports, so we have access to additional helper nodes, like isNaN check
            for (int i = 0; i < tickNodes.Length; i++)
            {
                if (tickNodes[i] is GltfInteractivityUnitExporterNode exporterNode)
                {
                    if (exporterNode.Exporter.exporter is TimeUnitExports)
                    {
                        var tmp = tickNodes[i];
                        tickNodes[i] = tickNodes[0];
                        tickNodes[0] = tmp;
                        break;
                    }
                }
                
            }
            var firstTickNode = tickNodes[0];
            
            var firstTickNodeFlowOut = firstTickNode.FlowConnections[Event_OnTickNode.IdFlowOut];

            GltfInteractivityNode firstTickNodeSelectNode = null;
            
            if (firstTickNode is GltfInteractivityUnitExporterNode firstTickNodeExport)
            {
                var selectNode = firstTickNodeExport.Exporter.Nodes.FirstOrDefault(n => n.Schema is Math_SelectNode);
                if (selectNode != null)
                {
                    firstTickNodeSelectNode = selectNode;
                }
            }
            
            for (int i = 1; i < tickNodes.Length; i++)
            {
                var tickNode = tickNodes[i];
                bool dontDelete = false;
                var flowOut = tickNode.FlowConnections[Event_OnTickNode.IdFlowOut];
                GltfInteractivityNode tickNodeSelectNode = null;
                   
                if (tickNode is GltfInteractivityUnitExporterNode tickNodeExport)
                {
                    if (tickNodeExport.Exporter.exporter is not TimeUnitExports)
                        continue;
                    var selectNode = tickNodeExport.Exporter.Nodes.FirstOrDefault(n => n.Schema is Math_SelectNode);
                    if (selectNode != null)
                    {
                        tickNodeSelectNode = selectNode;
                    }
                }
                
                if (flowOut.Node != null && flowOut.Node.Value != -1)
                {
                    if (firstTickNodeFlowOut.Node == null || firstTickNodeFlowOut.Node.Value == -1)
                    {
                        firstTickNodeFlowOut.Node = flowOut.Node;
                        firstTickNodeFlowOut.Socket = flowOut.Socket;
                    }
                    else
                    {
                        dontDelete = true;
                    }
                }
                
                foreach (var node in nodes)
                {
                    foreach (var socket in node.ValueInConnection)
                    {
                        if (tickNodeSelectNode != null && firstTickNodeSelectNode != null)
                        {
                            if (socket.Value.Node != null && socket.Value.Node.Value == tickNodeSelectNode.Index)
                            {
                                socket.Value.Node = firstTickNodeSelectNode.Index;
                            }
                            
                        }
                        else
                        if (socket.Value.Node != null && socket.Value.Node.Value == tickNode.Index)
                        {
                            socket.Value.Node = firstTickNode.Index;
                        }
                    }
                }

                if (!dontDelete)
                {
                    if (tickNodeSelectNode != null && firstTickNodeSelectNode != null && tickNode is GltfInteractivityUnitExporterNode tickNodeExport2)
                    {
                        // also remove isNaN check
                        var exporterNode = tickNodeExport2.Exporter.Nodes;
                        foreach (var n in exporterNode)
                            n.ValueInConnection.Clear();

                        foreach (var n in exporterNode)
                            task.RemoveNode(n);
                    }
                    else
                        task.RemoveNode(tickNode);
                }
            }
        }
    }
}