using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public interface ISocketConnector
    {
        public void ConnectOutFlow(GltfInteractivityExportNode otherNode, string otherSocketId, GltfInteractivityExportNode fromNode, string fromSocketId);
        public void ConnectValueIn(GltfInteractivityExportNode otherNode, string otherSocketId, GltfInteractivityExportNode fromNode, string fromSocketId);
    }
    
    public interface ISocketConnectorProvider
    {
        public ISocketConnector GetSocketConnector();
    }

    public class DirectSocketConnector : ISocketConnector
    {
        public void ConnectOutFlow(GltfInteractivityExportNode otherNode, string otherSocketId, GltfInteractivityExportNode fromNode, string fromSocketId)
        {
            if (fromNode.FlowConnections.TryGetValue(fromSocketId, out var flowConnection))
            {
                flowConnection.Node = otherNode.Index;
                flowConnection.Socket = otherSocketId;
            }
            else
            {
                fromNode.FlowConnections.Add(fromSocketId, new GltfInteractivityNode.FlowSocketData
                {
                    Node = otherNode.Index,
                    Socket = otherSocketId
                });
            }
        }

        public void ConnectValueIn(GltfInteractivityExportNode otherNode, string otherSocketId, GltfInteractivityExportNode fromNode, string fromSocketId)
        {
            if (fromNode.ValueInConnection.TryGetValue(fromSocketId, out var valueInConnection))
            {
                valueInConnection.Node = otherNode.Index;
                valueInConnection.Socket = otherSocketId;
            }
            else
            {
                fromNode.ValueInConnection.Add(fromSocketId, new GltfInteractivityNode.ValueSocketData
                {
                    Node = otherNode.Index,
                    Socket = otherSocketId
                });
            }
        }    
    }
}