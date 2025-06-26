using System.Linq;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class CombineCleanUp : ICleanUp
    {
        private static readonly string[] InputNames = new string[] {"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p"};

        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new CombineCleanUp());
        }
        
        ValueInRef[] FindNodesWithConnectionTo(CleanUpTask task, GltfInteractivityExportNode node)
        {

            return task.context.Nodes.SelectMany(n =>
                n.ValueInConnection.Where(v => v.Value.Node == node.Index).Select( kvp => n.ValueIn(kvp.Key))).ToArray();
        }


        public void OnCleanUp(CleanUpTask task)
        {
            var combines = task.context.Nodes.FindAll(
                node => 
                    node.Schema is Math_Combine2Node
                    || node.Schema is Math_Combine3Node
                    || node.Schema is Math_Combine4Node
                    ).ToArray();

            object GetValueFromCombineNode(GltfInteractivityExportNode node)
            {
                float Gv(int socketNr)
                {
                    if (node.ValueInConnection[InputNames[socketNr]].Value is float)
                        return (float)node.ValueInConnection[InputNames[socketNr]].Value;

                    Debug.LogError("Value is not float: "+ node.ValueInConnection[InputNames[socketNr]].Value);
                    return 0;
                }
                
                if (node.Schema is Math_Combine2Node)
                    return new Vector2(Gv(0), Gv(1));
                if (node.Schema is Math_Combine3Node)
                    return new Vector3(Gv(0), Gv(1), Gv(2));
                if (node.Schema is Math_Combine4Node)
                    return new Vector4(Gv(0), Gv(1), Gv(2), Gv(3));
                return null;
            }
            
            foreach (var combine in combines)
            {
                var anyConnection = combine.ValueInConnection.Any(c => c.Value.Node != null);
                if (anyConnection)
                    continue;
                
                var combineValue = GetValueFromCombineNode(combine);
                if (combineValue == null)
                    continue;
                var sockets = FindNodesWithConnectionTo(task, combine);
                foreach (var socket in sockets)
                    socket.SetValue(combineValue);
      
                task.RemoveNode(combine);
            }
            
        }
    }
}