using System.Collections.Generic;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
 public class ValueInRef : SocketRef<GltfInteractivityNode.ValueSocketData>
    {
        public ValueInRef(GltfInteractivityExportNode node, KeyValuePair<string, GltfInteractivityNode.ValueSocketData> socket) : base(node, socket)
        {
        }
            
        public virtual LinkedValueInputRef Link(ValueInRef other)
        {
            var multi = new LinkedValueInputRef(node, socket);
            return multi.Link(other);
        }
            
        public virtual ValueInRef ConnectToSource(ValueOutRef other)
        {
            // Add null check to prevent NullReferenceException
            if (other == null)
            {
                UnityEngine.Debug.LogWarning($"Attempted to connect null ValueOutRef to ValueInRef on node {node}");
                return this;
            }

            SocketConnector.ConnectValueIn(other.node, other.socket.Key, node, socket.Key);
            return this;
        }
            
        public virtual ValueInRef SetType(TypeRestriction typeRestriction)
        {
            socket.Value.typeRestriction = typeRestriction;
            return this;
        }

        public virtual ValueInRef SetValue(object value)
        {
            node.SetValueInSocket(socket.Key, value);
            return this;
        }
    }

    public class LinkedValueInputRef : ValueInRef
    {
        public List<ValueInRef> links = new List<ValueInRef>();

        public LinkedValueInputRef(GltfInteractivityExportNode node, KeyValuePair<string, GltfInteractivityNode.ValueSocketData> socket) : base(node, socket)
        {
            links.Add(new(node, socket));
        }

        public override LinkedValueInputRef Link(ValueInRef other)
        {
            if (other is LinkedValueInputRef linkedOther)
                links.AddRange(linkedOther.links);
            links.Add(other);
            return this;
        }

        public override ValueInRef ConnectToSource(ValueOutRef other)
        {
            foreach (var n in links)
                n.ConnectToSource(other);
            return this;
        }
            
        public override ValueInRef SetType(TypeRestriction typeRestriction)
        {
            foreach (var n in links)
                n.SetType(typeRestriction);
            return this;
        }

        public override ValueInRef SetValue(object value)
        {
            foreach (var n in links)
                n.SetValue(value);
            return this;
        }
    }

    public class FlowOutRef : SocketRef<GltfInteractivityNode.FlowSocketData>
    {
        public FlowOutRef(GltfInteractivityExportNode node, KeyValuePair<string, GltfInteractivityNode.FlowSocketData> socket) : base(node, socket)
        {
        }
            
        public FlowOutRef ConnectToFlowDestination(FlowInRef other)
        {
            SocketConnector.ConnectOutFlow(other.node, other.socket.Key, node, socket.Key);
            return this;
        }
    }

    public class FlowInRef : SocketRef<GltfInteractivityNode.FlowSocketData>
    {
        public FlowInRef(GltfInteractivityExportNode node, KeyValuePair<string, GltfInteractivityNode.FlowSocketData> socket) : base(node, socket)
        {
        }
    }

    public class ValueOutRef
    {
        public ISocketConnector SocketConnector => node.GetSocketConnector();
        public KeyValuePair<string, GltfInteractivityNode.OutputValueSocketData> socket { get; private set; }
        public GltfInteractivityExportNode node { get; protected set; }
            
        public ValueOutRef(GltfInteractivityExportNode node, KeyValuePair<string, GltfInteractivityNode.OutputValueSocketData> socket)
        {
            this.socket = socket;
            this.node = node;    
        }
            
        public ValueOutRef ExpectedType(ExpectedType expectedType)
        {
            socket.Value.expectedType = expectedType;
            return this;
        }
    }

    public class SocketRef<T> where T : GltfInteractivityNode.SocketData
    {
        public ISocketConnector SocketConnector => node.GetSocketConnector();
        public GltfInteractivityExportNode node { get; private set;}
        public KeyValuePair<string, T> socket { get; private set; }
            
        public SocketRef(GltfInteractivityExportNode node, KeyValuePair<string,T> socket)
        {
            this.socket = socket;
            this.node = node;
        }
    }
}