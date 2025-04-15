using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class ComposeAndDecomposeCleanUp : ICleanUp
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif

        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new ComposeAndDecomposeCleanUp());
        }
        
        public void OnCleanUp(CleanUpTask task)
        {
            PointerGetDeduplicationCleanUp.MergeSameGetPointersNodes(task, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", PointersHelper.IdPointerNodeIndex);
            PointerGetDeduplicationCleanUp.MergeSameGetPointersNodes(task, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/matrix", PointersHelper.IdPointerNodeIndex);
            
            var decompose = task.context.Nodes.FindAll(node => node.Schema is Math_MatDecomposeNode).ToArray();
            
            foreach (var node1 in decompose)
            {
                if (node1.Index == -1)
                    continue;

                var socket1 = node1.ValueInConnection[Math_MatDecomposeNode.IdInput];

                foreach (var node2 in decompose)
                {
                    if (node2.Index == -1 || node1 == node2)
                        continue;
                    
                    var socket2 = node2.ValueInConnection[Math_MatDecomposeNode.IdInput];
    
                    bool isSameValueInput = false;
                    if (socket1.Node != null
                        && socket2.Node != null
                        && socket1.Node.Value != -1
                        && socket1.Node.Value == socket2.Node.Value
                        && socket1.Socket == socket2.Socket)
                        isSameValueInput = true;
                    else if (socket1.Value != null && socket2.Value != null && (int)socket1.Value == (int)socket2.Value)
                        isSameValueInput = true;

                    if (isSameValueInput)
                    {
                        // Find all nodes which are connected to node2 and connect them to node1
                        foreach (var node in task.context.Nodes)
                        {
                            foreach (var socket in node.ValueInConnection)
                                if (socket.Value.Node == node2.Index)
                                   socket.Value.Node = node1.Index;
                        }
                        
                        task.RemoveNode(node2);
                        // Mark with -1 to avoid processing an already deleted node
                        node2.Index = -1;
                        continue;
                    }
                    
                    
                }
         
            }
        }
    }
}