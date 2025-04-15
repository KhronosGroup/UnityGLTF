using System.Collections.Generic;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class VariableGetDeduplicationCleanUp : ICleanUp
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new VariableGetDeduplicationCleanUp());
        }
        
        
        public static void MergeSameGetVariableNodes(CleanUpTask task, int varId)
        {
            var varNodes = task.context.Nodes.FindAll(node => node.Schema is Variable_GetNode
                                                                       && node.Configuration[
                                                                           Variable_GetNode.IdConfigVarIndex].Value.Equals(varId)).ToArray();
            
            foreach (var varNode1 in varNodes)
            {
                if (varNode1.Index == -1)
                    continue;

           
                foreach (var varNode2 in varNodes)
                {
                    if (varNode2.Index == -1 || varNode1 == varNode2)
                        continue;

                    // Find all nodes which are connected to glNode2 and connect them to glNode1
                    foreach (var node in task.context.Nodes)
                    {
                        foreach (var socket in node.ValueInConnection)
                            if (socket.Value.Node == varNode2.Index && socket.Value.Socket == Variable_GetNode.IdOutputValue)
                                socket.Value.Node = varNode1.Index;
                    }
                    
                    task.RemoveNode(varNode2);
                    // Mark with -1 to avoid processing an already deleted node
                    varNode2.Index = -1;
                }
            }          
        }
        
        public void OnCleanUp(CleanUpTask task)
        {
            var varNodes = task.context.Nodes.FindAll(node => node.Schema is Variable_GetNode);

            var varIds = new HashSet<int>();
            
            foreach (var varNode in varNodes)
            {
                var varId = (int)varNode.Configuration[Variable_GetNode.IdConfigVarIndex].Value;
                varIds.Add(varId);
            }
            
            foreach (var varId in varIds)
                MergeSameGetVariableNodes(task, varId);
        }
    }
}