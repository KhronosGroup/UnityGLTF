using System.Collections.Generic;
using System.Linq;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class PointerGetDeduplicationCleanUp : ICleanUp
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new PointerGetDeduplicationCleanUp());
        }
        
        
        public static void MergeSameGetPointersNodes(CleanUpTask task, string pointer, string pointerInputName)
        {
            var pointerNodes = task.context.Nodes.FindAll(node => node.Schema is Pointer_GetNode
                                                                       && node.Configuration[
                                                                           Pointer_GetNode.IdPointer].Value.Equals(pointer)
                                                                       && node.ValueInConnection.ContainsKey(pointerInputName)
                                                                       ).ToArray();
            
            foreach (var glNode1 in pointerNodes)
            {
                if (glNode1.Index == -1)
                    continue;
                
                var glNode1Socket = glNode1.ValueInConnection[pointerInputName];
                
                foreach (var glNode2 in pointerNodes)
                {
                    if (glNode2.Index == -1 || glNode1 == glNode2)
                        continue;

                    var glNode2Socket = glNode2.ValueInConnection[pointerInputName];

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
                            foreach (var socket in node.ValueInConnection)
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
            var pointerNodes = task.context.Nodes.FindAll(node => node.Schema is Pointer_GetNode && node.ValueInConnection.Count == 1);

            var pointers = new HashSet<(string template, string valueInput)>();
            
            foreach (var pointerNode in pointerNodes)
            {
                var pointer = (string)pointerNode.Configuration[Pointer_GetNode.IdPointer].Value;
                var pointerInput = pointerNode.ValueInConnection.First().Key;
                pointers.Add((pointer, pointerInput));
            }

            foreach (var pointer in pointers)
            {
                MergeSameGetPointersNodes(task, pointer.template, pointer.valueInput);
            }
        }
    }
}