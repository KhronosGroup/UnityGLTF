using System.Collections.Generic;
using System.Linq;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class GltfInteractivityExportNode<TSchema> : GltfInteractivityExportNode
        where TSchema : GltfInteractivityNodeSchema, new()
    {
        
        public GltfInteractivityExportNode() : base(GltfInteractivityNodeSchema.GetSchema<TSchema>())
        {
        }
    }
    
    public class GltfInteractivityExportNode : GltfInteractivityNode, ISocketConnectorProvider
    {
        // This data will not be serialized
        public Dictionary<string, OutputValueSocketData> OutputValueSocket =
            new Dictionary<string, OutputValueSocketData>();

        public ISocketConnector SocketConnector = new DirectSocketConnector();
        
        public GltfInteractivityExportNode(GltfInteractivityNodeSchema schema) : base(schema)
        {
        }
        
        public override void SetSchema(GltfInteractivityNodeSchema schema, bool applySocketDescriptors,
            bool clearExistingSocketData = true)
        {
            base.SetSchema(schema, applySocketDescriptors, clearExistingSocketData);

            if (applySocketDescriptors)
            {
                if (clearExistingSocketData)
                    OutputValueSocket.Clear();
                
                foreach (var descriptor in Schema.OutputValueSockets)
                {
                    if (descriptor.Value.SupportedTypes.Length == 1 && descriptor.Value.expectedType == null)
                        OutputValueSocket.Add(descriptor.Key,
                            new OutputValueSocketData {expectedType = ExpectedType.GtlfType(descriptor.Value.SupportedTypes[0])});
                    else
                        OutputValueSocket.Add(descriptor.Key,
                            new OutputValueSocketData {expectedType = descriptor.Value.expectedType });
                }
            }
        }
        
        public ISocketConnector GetSocketConnector()
        {
            return SocketConnector;
        }

        public ValueInRef ValueIn(string socketName)
        {
            if (!ValueInConnection.ContainsKey(socketName))
            {
               ValueInConnection.Add(socketName, new ValueSocketData {});
            }
            
            var socket = new ValueInRef(this, new KeyValuePair<string, ValueSocketData>(socketName, ValueInConnection[socketName]));
            return socket;
        }
        
        public FlowOutRef FlowOut(string socketName)
        {
            if (!FlowConnections.ContainsKey(socketName))
            {
                FlowConnections.Add(socketName, new FlowSocketData {});
            }
            var socket = new FlowOutRef(this, new KeyValuePair<string, FlowSocketData>(socketName, FlowConnections[socketName]));
            return socket;
        }

        public FlowInRef FlowIn(string socketName)
        {
            var socket = new FlowInRef(this, new KeyValuePair<string, FlowSocketData>(socketName, new FlowSocketData()));
            return socket;
        }

        public FlowInRef FlowIn()
        {
            if (Schema.InputFlowSockets.ContainsKey("in"))
                return FlowIn("in");
            
            throw new System.Exception("No default 'in' socket found in input flow sockets. Schema: " + Schema.Op);
        }
        
        public ValueOutRef ValueOut(string socket)
        {
            if (!OutputValueSocket.ContainsKey(socket))
            {
                OutputValueSocket.Add(socket, new OutputValueSocketData());
            }

            return new ValueOutRef(this, new KeyValuePair<string, OutputValueSocketData>(socket, OutputValueSocket[socket]));
        }
        public FlowOutRef FlowOut()
        {
            if (Schema.OutputFlowSockets.ContainsKey("out"))
                return FlowOut("out");
            
            throw new System.Exception("No default 'out' socket found in output flow sockets. Schema: " + Schema.Op);
        }
        
        
        public ValueOutRef FirstValueOut()
        {
            var firstItem = OutputValueSocket.FirstOrDefault();
            if (firstItem.Value == null)
                return null;
            
            return new ValueOutRef(this,  firstItem);
        }
        
    }
}