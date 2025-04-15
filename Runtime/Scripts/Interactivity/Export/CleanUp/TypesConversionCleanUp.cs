using System;
using System.Collections.Generic;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class TypesConversionCleanUp : ICleanUp
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new TypesConversionCleanUp());
        }

        public void OnCleanUp(CleanUpTask task)
        {
            var nodes = task.context.Nodes;

            var typesToByPass = new Dictionary<Type, Type>();
            typesToByPass.Add(typeof(Type_BoolToFloatNode), typeof(Type_FloatToBoolNode));
            typesToByPass.Add(typeof(Type_FloatToBoolNode), typeof(Type_BoolToFloatNode));
            
            typesToByPass.Add(typeof(Type_BoolToIntNode), typeof(Type_IntToBoolNode));
            typesToByPass.Add(typeof(Type_IntToBoolNode), typeof(Type_BoolToIntNode));
            
            typesToByPass.Add(typeof(Type_FloatToIntNode), typeof(Type_IntToFloatNode));
            typesToByPass.Add(typeof(Type_IntToFloatNode), typeof(Type_FloatToIntNode));
            
            // Find all nodes, that are connected from a typesToByPass.key to a typesToByPass.value
            foreach (var typeToByPass in typesToByPass)
            {
                var nodesWithKeyType = nodes.FindAll(node => node.Schema.GetType() == typeToByPass.Key).ToArray();
                foreach (var nodeWithKeyType in nodesWithKeyType)
                {
                   
                    foreach (var valueSocket in nodeWithKeyType.ValueInConnection)
                    {
                        if (valueSocket.Value.Node == null || valueSocket.Value.Node == -1)
                            continue;

                        var otherNode = nodes[valueSocket.Value.Node.Value];
                        
                        if (otherNode.Schema.GetType() == typeToByPass.Value)
                        {
                            
                            task.ByPassValue(otherNode, Type_BoolToFloatNode.IdInputA, nodeWithKeyType, Type_BoolToFloatNode.IdValueResult);
                            
                            task.RemoveNode(nodeWithKeyType);
                            task.RemoveNode(otherNode);
                        }
                    }
                }
            }
        }
    }
}