using System.Collections.Generic;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class DeduplicationCleanUp : ICleanUp
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        private static void Register()
        {
          CleanUpRegistry.RegisterCleanUp(new DeduplicationCleanUp());
        }
        
        public void OnCleanUp(CleanUpTask task)
        {
            var nodes = task.context.Nodes.ToArray();
            var removed = new HashSet<GltfInteractivityNode>();
            
            foreach (var n in nodes)
            {
                if (removed.Contains(n))
                    continue;
                
                if (n.ValueInConnection.Count == 0)
                    continue;
                if (n.FlowConnections.Count > 0)
                    continue;
                if (n.Configuration.Count > 0)
                    continue;
                if (n.Schema.InputFlowSockets.Count > 0)
                    continue;

                foreach (var n2 in nodes)
                {
                    if (removed.Contains(n2))
                        continue;

                    if (n2 == n)
                        continue;
                    
                    if (n.Schema.Op != n2.Schema.Op)
                        continue;
                    
                    if (n.ValueInConnection.Count != n2.ValueInConnection.Count)
                        continue;
                    
                    if (n.OutputValueSocket.Count != n2.OutputValueSocket.Count)
                        continue;

                    bool canBeMerged = true;
                    foreach (var nSocket in n.ValueInConnection)
                    {
                        if (n2.ValueInConnection.TryGetValue(nSocket.Key, out var n2Socket))
                        {
                            if (nSocket.Value.Value != n2Socket.Value || nSocket.Value.Node != n2Socket.Node ||
                                nSocket.Value.Socket != n2Socket.Socket)
                            {
                                canBeMerged = false;
                                break;
                            }
                        }
                        else
                        {
                            canBeMerged = false;
                            break;
                        }
                    }

                    if (canBeMerged)
                    {
                        foreach (var node in task.context.Nodes)
                        {
                            foreach (var socket in node.ValueInConnection)
                            {
                                if (socket.Value.Node == n2.Index)
                                    socket.Value.Node = n.Index;
                                
                            }
                        }

                        removed.Add(n2);
                        task.RemoveNode(n2);
                    }
                }
            }
            
            
            
        }
    }
}