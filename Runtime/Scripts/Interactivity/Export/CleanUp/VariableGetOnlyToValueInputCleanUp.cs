using System.Collections.Generic;
using System.Linq;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class VariableGetOnlyToValueInputCleanUp : ICleanUp
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new VariableGetOnlyToValueInputCleanUp());
        }
        
        public void OnCleanUp(CleanUpTask task)
        {
            var varGetNodes = task.context.Nodes.FindAll(node => node.Schema is Variable_GetNode);
            var varSetNodes = task.context.Nodes.FindAll(node => node.Schema is Variable_SetNode);
            var varSetMultiNodes = task.context.Nodes.FindAll(node => node.Schema is Variable_SetMultipleNode);
            var varSetIntNodes = task.context.Nodes.FindAll(node => node.Schema is Variable_InterpolateNode);

            var varSetAndIntNodes = varSetNodes.Concat(varSetIntNodes);
            
            var varIdsToRemove = new HashSet<int>();
            var varGetNodesToRemove = new List<GltfInteractivityExportNode>();
            
            foreach (var variable in task.context.variables)
            {
                var varId = task.context.variables.IndexOf(variable);

                var isInMulti = varSetMultiNodes.Any(n =>
                {
                    var arr = n.Configuration[Variable_SetMultipleNode.IdConfigVarIndices].Value as int[];
                    return arr.Contains(varId);
                });
                if (isInMulti)
                    continue;
                
                if (!varSetAndIntNodes.Any(n => n.Configuration[Variable_SetNode.IdConfigVarIndex].Value.Equals(varId)))
                    varIdsToRemove.Add(varId);
            }

            foreach (var varGetNode in varGetNodes)
            {
                if (varGetNode.Configuration[Variable_GetNode.IdConfigVarIndex].Value is int varId)
                {
                    if (varIdsToRemove.Contains(varId))
                    {
                        varGetNodesToRemove.Add(varGetNode);
                        
                        // Remove all connections to this node and set the default Value of the variable to the socket
                        foreach (var node in task.context.Nodes)
                        {
                            foreach (var socket in node.ValueInConnection)
                            {
                                if (socket.Value.Node == varGetNode.Index && socket.Value.Socket == Variable_GetNode.IdOutputValue)
                                    node.SetValueInSocket(socket.Key, task.context.variables[varId].Value);
                            }
                        }
                        
                    }
                }
            }
            
            foreach (var vargetNode in varGetNodesToRemove)
                task.RemoveNode(vargetNode);
            
            // TODO: remove Variables from Graph
        }
    }
}