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

            GltfInteractivityUnitExporterNode firstDeltaTimeNode = null;
            GltfInteractivityUnitExporterNode firstTimeSinceStartNode = null;
            
            // Ensure the first OnTickNode is from a TimeUnitExports, so we have access to additional helper nodes, like isNaN check
            for (int i = 0; i < tickNodes.Length; i++)
            {
                if (tickNodes[i] is GltfInteractivityUnitExporterNode exporterNode)
                {
                    if (exporterNode.Exporter.exporter is TimeUnitExports timeUnitExport)
                    {
                        if (timeUnitExport.ValueOption == TimeHelpers.GetTimeValueOption.DeltaTime
                            && firstDeltaTimeNode == null)
                            firstDeltaTimeNode = exporterNode;
                        if (timeUnitExport.ValueOption == TimeHelpers.GetTimeValueOption.TimeSinceStartup
                            && firstTimeSinceStartNode == null)
                            firstTimeSinceStartNode = exporterNode;
                            
                    }
                }
            }
            
            GltfInteractivityExportNode firstDeltaTimeSelectNode = null;
            GltfInteractivityExportNode firstTimeSinceStartSelectNode = null;
            if (firstDeltaTimeNode != null)
            {
                firstDeltaTimeSelectNode = firstDeltaTimeNode.Exporter.Nodes.FirstOrDefault(n => n.Schema is Math_SelectNode);
            }
            if (firstTimeSinceStartNode != null)
            {
                firstTimeSinceStartSelectNode = firstTimeSinceStartNode.Exporter.Nodes.FirstOrDefault(n => n.Schema is Math_SelectNode);
            }

            for (int i = 0; i < tickNodes.Length; i++)
            {
                if (tickNodes[i] == firstDeltaTimeNode || tickNodes[i] == firstTimeSinceStartNode)
                    continue; // skip the first tick node, which is used for delta time or time since startup

                TimeHelpers.GetTimeValueOption timeValue = TimeHelpers.GetTimeValueOption.DeltaTime;

                var tickNode = tickNodes[i];
                GltfInteractivityNode tickNodeSelectNode = null;

                if (tickNode is not GltfInteractivityUnitExporterNode tickNodeExport)
                    continue;

                if (tickNodeExport.Exporter.exporter is not TimeUnitExports timeUnitExport)
                    continue;

                timeValue = timeUnitExport.ValueOption;

                var selectNode = tickNodeExport.Exporter.Nodes.FirstOrDefault(n => n.Schema is Math_SelectNode);
                if (selectNode == null)
                    continue;

                tickNodeSelectNode = selectNode;

                foreach (var node in nodes)
                {
                    foreach (var socket in node.ValueInConnection)
                    {
                        if (timeValue == TimeHelpers.GetTimeValueOption.DeltaTime && firstDeltaTimeNode != null && firstDeltaTimeSelectNode != null)
                        {
                            if (socket.Value.Node != null && socket.Value.Node.Value == tickNodeSelectNode.Index)
                            {
                                socket.Value.Node = firstDeltaTimeSelectNode.Index;
                            }
                        }

                        if (timeValue == TimeHelpers.GetTimeValueOption.TimeSinceStartup && firstTimeSinceStartNode != null && firstTimeSinceStartSelectNode != null)
                        {
                            if (socket.Value.Node != null && socket.Value.Node.Value == tickNodeSelectNode.Index)
                            {
                                socket.Value.Node = firstTimeSinceStartSelectNode.Index;
                            }
                        }
                    }
                }

                // also remove isNaN check
                var exporterNode = tickNodeExport.Exporter.Nodes;
                foreach (var n in exporterNode)
                    n.ValueInConnection.Clear();

                foreach (var n in exporterNode)
                    task.RemoveNode(n);
            }
        }
    }
}