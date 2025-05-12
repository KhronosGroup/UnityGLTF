using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class Value
    {
        public IProperty property;
        public string id { get; set; }
        public Node node { get; set; } = null;
        public string socket { get; set; } = Constants.EMPTY_SOCKET_STRING;

        public event Action<Value> onConnectionChanged;

        public T GetValue<T>()
        {
            return Helpers.GetPropertyValue<T>(property);
        }

        public bool TryDisconnect()
        {
            if (node == null && socket == Constants.EMPTY_SOCKET_STRING)
            {
                Util.LogWarning($"Value {id} is already disconnected from any nodes.");
                return false;
            }

            node = null;
            socket = Constants.EMPTY_SOCKET_STRING;

            onConnectionChanged?.Invoke(this);

            return true;
        }

        public bool TryConnectToSocket(Node node, string socket)
        {
            if (this.node == node && this.socket == socket)
            {
                Util.LogWarning($"Value {id} is already connected to node {node.type} on socket {socket}.");
                return false;
            }

            this.node = node;
            this.socket = socket;

            node.onRemovedFromGraph += OnConnectedNodeRemovedFromGraph;
            onConnectionChanged?.Invoke(this);

            return true;
        }

        private void OnConnectedNodeRemovedFromGraph()
        {
            node.onRemovedFromGraph -= OnConnectedNodeRemovedFromGraph;

            TryDisconnect();
        }

        public void ChangeType<T>(T value)
        {
            property = new Property<T>(value);
        }
    }
}