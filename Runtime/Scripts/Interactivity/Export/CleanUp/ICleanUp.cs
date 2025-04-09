namespace UnityGLTF.Interactivity.Export
{
    public class CleanUpTask
    {
  
        public InteractivityExportContext context { get; }
        
        private bool hasChanges = false;
        
        public CleanUpTask(InteractivityExportContext context)
        {
            this.context = context;
        }
        
        public void RemoveNode(GltfInteractivityExportNode node)
        {
            context.RemoveNode(node);
            hasChanges = true;
        }
        
        public void ByPassFlow(GltfInteractivityExportNode node, string flowIn, string flowOut)
        {
            var flowSocket = node.FlowConnections[flowOut];
            foreach (var n in context.Nodes)
            {
                foreach (var flow in n.FlowConnections)
                {
                    if (flow.Value.Node == node.Index && flow.Value.Socket == flowIn)
                    {
                        flow.Value.Node = flowSocket.Node;
                        flow.Value.Socket = flowSocket.Socket;
                    }
                }
            }
            hasChanges = true;
        }
        
        public void ByPassValue(GltfInteractivityExportNode node, string valueIn, string valueOut)
        {
            var flowSocket = node.ValueInConnection[valueIn];

            foreach (var n in context.Nodes)
            {
                foreach (var valueSocket in n.ValueInConnection)
                {
                    if (valueSocket.Value.Node == node.Index &&
                        valueSocket.Value.Socket == valueOut)
                    {
                        valueSocket.Value.Node = flowSocket.Node;
                        valueSocket.Value.Socket = flowSocket.Socket;
                    }
                }
            }
            hasChanges = true;
        }
        
        public void ByPassValue(GltfInteractivityExportNode nodeA, string valueAIn, GltfInteractivityExportNode nodeB, string valueBOut)
        {
            var socketA = nodeA.ValueInConnection[valueAIn];


            foreach (var node in context.Nodes)
            {
                foreach (var valueSocket in node.ValueInConnection)
                {
                    if (valueSocket.Value.Node != null && nodeB.Index == valueSocket.Value.Node.Value && valueBOut == valueSocket.Value.Socket)
                    {
                        valueSocket.Value.Node = socketA.Node;
                        valueSocket.Value.Socket = socketA.Socket;
                    }
                }
            }
            hasChanges = true;
        }
        
        public bool HasChanges
        {
            get => hasChanges;
        }
        
    }
    
    public interface ICleanUp
    {

        public void OnCleanUp(CleanUpTask task);
    }
    
    
}