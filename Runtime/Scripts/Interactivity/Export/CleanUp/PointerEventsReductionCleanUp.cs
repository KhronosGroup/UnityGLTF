using System.Linq;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class PointerEventsReductionCleanUp : ICleanUp
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new PointerEventsReductionCleanUp());
        }
        
        public void OnCleanUp(CleanUpTask task)
        {
            var onSelectGroups = task.context.Nodes.FindAll(
                node => node.Schema is Event_OnSelectNode)
                .GroupBy( n => (int)n.Configuration["nodeIndex"].Value).Where( g => g.Count() > 1).ToArray();
            
            Reduce(onSelectGroups);
            
            var onHoverInGroups = task.context.Nodes.FindAll(
                    node => node.Schema is Event_OnHoverInNode)
                .GroupBy( n => (int)n.Configuration["nodeIndex"].Value).Where( g => g.Count() > 1).ToArray();

            Reduce(onHoverInGroups);

            var onHoverOutGroups = task.context.Nodes.FindAll(
                    node => node.Schema is Event_OnHoverOutNode)
                .GroupBy( n => (int)n.Configuration["nodeIndex"].Value).Where( g => g.Count() > 1).ToArray();

            Reduce(onHoverOutGroups);

            void Reduce(IGrouping<int, GltfInteractivityExportNode>[] nodeGroup)
            {
                foreach (var group in nodeGroup)
                {
                    var first = group.First();
                    var firstTargetNode = first.FlowOut().socket.Value.Node;
                    var firstTargetSocket = first.FlowOut().socket.Value.Socket;

                    var sequence = new GltfInteractivityExportNode<Flow_SequenceNode>();
                    sequence.Index = task.context.Nodes.Count;
                    task.context.Nodes.Add(sequence);

                    first.FlowOut().ConnectToFlowDestination(sequence.FlowIn());

                    if (firstTargetNode != null)
                        sequence.SetFlowOut("s" + sequence.FlowConnections.Count.ToString(), firstTargetNode.Value,
                            firstTargetSocket);

                    var groupNodes = group.Skip(1).ToArray();
                    foreach (var node in groupNodes)
                    {
                        var targetNode = node.FlowOut().socket.Value.Node;
                        var targetSocket = node.FlowOut().socket.Value.Socket;
                        if (targetNode != null)
                            sequence.SetFlowOut("s" + sequence.FlowConnections.Count.ToString(), targetNode.Value,
                                targetSocket);
                    }

                    // Rewire all value connections to the first node, because all others in this group will be removed
                    foreach (var n in task.context.Nodes)
                    {
                        foreach (var value in n.ValueInConnection)
                        {
                            if (value.Value.Node != null && groupNodes.Any(gn => gn.Index == value.Value.Node.Value))
                                value.Value.Node = first.Index;
                        }
                    }

                    foreach (var node in groupNodes)
                    {
                        task.RemoveNode(node);
                    }
                    
                }
            }

        }
    }
}