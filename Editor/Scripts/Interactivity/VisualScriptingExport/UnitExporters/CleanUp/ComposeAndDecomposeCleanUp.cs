using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public class ComposeAndDecomposeCleanUp : ICleanUp
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new ComposeAndDecomposeCleanUp());
        }

        private void MergeSameGetPointersNodes(CleanUpTask task, string pointer)
        {
            var pointerNodes = task.context.Nodes.FindAll(node => node.Schema is Pointer_GetNode
                                                                       && node.ConfigurationData[
                                                                           Pointer_GetNode.IdPointer].Value.Equals(pointer)).ToArray();
        
            // Find globalMatrix - Pointer/GetNodes which access the same globalMatrix

            foreach (var glNode1 in pointerNodes)
            {
                if (glNode1.Index == -1)
                    continue;

                var glNode1Socket = glNode1.ValueSocketConnectionData[UnitsHelper.IdPointerNodeIndex];
                
                foreach (var glNode2 in pointerNodes)
                {
                    if (glNode2.Index == -1 || glNode1 == glNode2)
                        continue;

                    var glNode2Socket = glNode2.ValueSocketConnectionData[UnitsHelper.IdPointerNodeIndex];

                    bool isSameValueInput = false;
                    if (glNode1Socket.Node != null
                        && glNode2Socket.Node != null
                        && glNode1Socket.Node.Value != -1
                        && glNode1Socket.Node.Value == glNode2Socket.Node.Value
                        && glNode1Socket.Socket == glNode2Socket.Socket)
                        isSameValueInput = true;
                    else if (glNode1Socket.Value != null && glNode2Socket.Value != null && (int)glNode1Socket.Value == (int)glNode2Socket.Value)
                        isSameValueInput = true;

                    if (isSameValueInput)
                    {
                        // Find all nodes which are connected to glNode2 and connect them to glNode1
                        foreach (var node in task.context.Nodes)
                        {
                            foreach (var socket in node.ValueSocketConnectionData)
                                if (socket.Value.Node == glNode2.Index && socket.Value.Socket == Pointer_GetNode.IdValue)
                                    socket.Value.Node = glNode1.Index;
                        }
                        
                        
                        task.RemoveNode(glNode2);
                        // Mark with -1 to avoid processing an already deleted node
                        glNode2.Index = -1;
                        continue;
                    }
                }
            }          
        }
        
        public void OnCleanUp(CleanUpTask task)
        {
            MergeSameGetPointersNodes(task, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/globalMatrix");
            MergeSameGetPointersNodes(task, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/matrix");
            
            var decompose = task.context.Nodes.FindAll(node => node.Schema is Math_MatDecomposeNode).ToArray();
            
            foreach (var node1 in decompose)
            {
                if (node1.Index == -1)
                    continue;

                var socket1 = node1.ValueSocketConnectionData[Math_MatDecomposeNode.IdInput];

                foreach (var node2 in decompose)
                {
                    if (node2.Index == -1 || node1 == node2)
                        continue;
                    
                    var socket2 = node2.ValueSocketConnectionData[Math_MatDecomposeNode.IdInput];
    
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
                            foreach (var socket in node.ValueSocketConnectionData)
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