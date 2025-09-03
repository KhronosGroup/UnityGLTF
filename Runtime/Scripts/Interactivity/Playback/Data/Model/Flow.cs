namespace UnityGLTF.Interactivity.Playback
{
    public class Flow
    {
        public Node fromNode { get; private set; }
        public string fromSocket { get; private set; }
        public Node toNode { get; private set; }
        public string toSocket { get; private set; }

        public Flow(Node fromNode, string fromSocket, Node toNode, string toSocket)
        {
            this.fromNode = fromNode;
            this.fromSocket = fromSocket;
            this.toNode = toNode;
            this.toSocket = toSocket;

            fromNode.onRemovedFromGraph += OnFromNodeRemovedFromGraph;
            toNode.onRemovedFromGraph += OnToNodeRemovedFromGraph;
        }

        private void OnFromNodeRemovedFromGraph()
        {
            UnsubscribeFromNodes();
        }

        private void OnToNodeRemovedFromGraph()
        {
            UnsubscribeFromNodes();
            fromNode.RemoveFlow(this);
        }

        private void UnsubscribeFromNodes()
        {
            toNode.onRemovedFromGraph -= OnToNodeRemovedFromGraph;
            fromNode.onRemovedFromGraph -= OnFromNodeRemovedFromGraph;
        }
    }
}