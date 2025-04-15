using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class WaitAllNodeCleanUp : ICleanUp
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new WaitAllNodeCleanUp());
        }

        public void OnCleanUp(CleanUpTask task)
        {
            var nodes = task.context.Nodes;
            
            var waitAllNodes = nodes.FindAll(node => node.Schema is Flow_WaitAllNode).ToArray();
            
            foreach (var waitAllNode in waitAllNodes)
            {
                int flowInCount = (int)waitAllNode.Configuration[Flow_WaitAllNode.IdConfigInputFlows].Value;
                bool canRemove = flowInCount <= 1;
                
                if (canRemove)
                {
                    if (flowInCount > 0)
                        task.ByPassFlow(waitAllNode, "0", Flow_WaitAllNode.IdFlowOutCompleted);
                    
                    task.RemoveNode(waitAllNode);
                }
            }
        }
    }
}