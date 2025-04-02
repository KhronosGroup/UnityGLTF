using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public class SequenceNodeCleanUp : ICleanUp
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new SequenceNodeCleanUp());
        }

        public void OnCleanUp(CleanUpTask task)
        {
            var nodes = task.context.Nodes;
            
            
            // Remove SequenceNode if it has only one connection
            var sequenceNodes = nodes.FindAll(node => node.Schema is Flow_SequenceNode).ToArray();
            
            foreach (var sequenceNode in sequenceNodes)
            {
                sequenceNode.RemoveUnconnectedFlows();
                
                bool canRemove = sequenceNode.FlowConnections.Count <= 1;
                
                if (canRemove)
                {
                    if (sequenceNode.FlowConnections.Count > 0)
                        task.ByPassFlow(sequenceNode, Flow_SequenceNode.IdFlowIn, sequenceNode.FlowConnections.First().Key);
                    
                    task.RemoveNode(sequenceNode);
                }
            }
        }
    }
}