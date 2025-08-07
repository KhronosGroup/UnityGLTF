using System;
using System.Linq;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class TickNodeDeduplicationCleanUp : ICleanUp
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new TickNodeDeduplicationCleanUp());
        }
        
        public void OnCleanUp(CleanUpTask task)
        {
            var nodes = task.context.Nodes;
            var tickNodes = nodes.FindAll(node => node.Schema is Event_OnTickNode).ToArray();


            var firstTickNodeWithFlow = tickNodes.FirstOrDefault( n => n.FlowOut().socket.Value.Node != null);
            if (firstTickNodeWithFlow != null)
            {
                var index = Array.IndexOf(tickNodes, firstTickNodeWithFlow);
                if (index > 0)
                {
                    // Move the first tick node to the first position
                    var temp = tickNodes[0];
                    tickNodes[0] = tickNodes[index];
                    tickNodes[index] = temp;
                }
            }
            
            for (int i = 1; i < tickNodes.Length; i++)
            {
                if (tickNodes[i].FlowOut().socket.Value.Node != null)
                    continue;
                
                foreach (var node in nodes)
                {
                    foreach (var valueSocket in node.ValueInConnection)
                    {
                        if (valueSocket.Value.Node != null && valueSocket.Value.Node == tickNodes[i].Index)
                            valueSocket.Value.Node = tickNodes[0].Index;
                    }
                }

                task.RemoveNode(tickNodes[i]);

            }
        }
    }
}