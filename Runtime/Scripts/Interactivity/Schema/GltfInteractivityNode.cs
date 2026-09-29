using UnityEngine;
using UnityGLTF.Interactivity;

namespace UnityGLTF.Interactivity.Schema
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    public class GltfInteractivityNode
    {
        public int Index;
        
        public int OpDeclaration = -1;
        
        public virtual GltfInteractivityNodeSchema Schema { get; protected set; }
        
        // Data to be serialized into Gltf
        public Dictionary<string, ConfigData> Configuration =
            new Dictionary<string, ConfigData>();
        public Dictionary<string, FlowSocketData> FlowConnections =
            new Dictionary<string, FlowSocketData>();
        public Dictionary<string, ValueSocketData> ValueInConnection =
            new Dictionary<string, ValueSocketData>();
        
        public Dictionary<string, string> MetaData = new Dictionary<string, string>();

        public virtual string AdditionalDebugString
        {
            get => "";
        }
        
        public void RemoveUnconnectedFlows()
        {
            var keys = FlowConnections.Keys.ToList();
            foreach (var key in keys)
            {
                if (FlowConnections[key].Node == null || FlowConnections[key].Node == -1)
                    FlowConnections.Remove(key);
            }
        }
        
        public void SetFlowOut(string socketId, GltfInteractivityNode targetNode, string targetSocketId)
        {
            if (!FlowConnections.TryGetValue(socketId, out var socket))
            {
                socket = new FlowSocketData();
                FlowConnections.Add(socketId, socket);
            }

            socket.Node = targetNode.Index;
            socket.Socket = targetSocketId;
        }
        
        public void SetFlowOut(string socketId, int targetNode, string targetSocketId)
        {
            if (!FlowConnections.TryGetValue(socketId, out var socket))
            {
                socket = new FlowSocketData();
                FlowConnections.Add(socketId, socket);
            }

            socket.Node = targetNode;
            socket.Socket = targetSocketId;
        }

        public virtual void SetSchema(GltfInteractivityNodeSchema schema, bool applySocketDescriptors, bool clearExistingSocketData = true)
        {
            this.Schema = schema;
            if (applySocketDescriptors)
            {
                Schema = schema;

                if (clearExistingSocketData)
                {
                    Configuration.Clear();
                    FlowConnections.Clear();
                    ValueInConnection.Clear();
                    MetaData.Clear();
                }
                
                foreach (var descriptor in Schema.Configuration)
                {
                    Configuration.Add(descriptor.Key, new ConfigData {Value = descriptor.Value.defaultValue});
                }

                foreach (var descriptor in Schema.InputValueSockets)
                {
                    ValueInConnection.Add(descriptor.Key, new ValueSocketData()
                    {
                        Type = -1, // Setting to undefined(-1), so type resolving on later stages can better determine if this type is finally true
                        typeRestriction = descriptor.Value.typeRestriction
                    });
                }
                
                foreach (var descriptor in Schema.OutputFlowSockets)
                {
                    FlowConnections.Add(descriptor.Key, new FlowSocketData());
                }
            
                foreach (GltfInteractivityNodeSchema.MetaDataEntry descriptor in Schema.MetaDatas)
                {
                    MetaData.Add(descriptor.key, descriptor.value);
                }
            }
        }
        
        public void SetValueInSocketSource(string socketId,  GltfInteractivityNode sourceNode, string sourceSocketId, TypeRestriction typeRestriction = null)
        {
            if (!ValueInConnection.TryGetValue(socketId, out var socket))
            {
                socket = new ValueSocketData(); 
                ValueInConnection.Add(socketId, socket);
            }
            
            socket.Node = sourceNode.Index;
            socket.Socket = sourceSocketId;
            socket.Value = null;
            socket.Type = -1;
            if (typeRestriction != null)
                socket.typeRestriction = typeRestriction;
        }
        
        public void SetValueInSocket(string socketId, object value, TypeRestriction typeRestriction = null)
        {
            if (!ValueInConnection.TryGetValue(socketId, out var socket))
            {
                socket = new ValueSocketData(); 
                ValueInConnection.Add(socketId, socket);
            }
            
            socket.Node = null;
            socket.Socket = null;
            socket.Value = value;
            if (value != null)
                socket.Type =  GltfTypes.TypeIndex(value.GetType());
            
            if (typeRestriction != null)
                socket.typeRestriction = typeRestriction;
        }
        
        public GltfInteractivityNode(GltfInteractivityNodeSchema schema)
        {
            SetSchema(schema, true);
        }
        
        public virtual JObject SerializeObject()
        {
            JObject jo = new JObject()
            {
                new JProperty("declaration", OpDeclaration)
            };
            
            // Empty objects are not allowed by the specification, so configuration, values and flows are only
            // written when they have at least one entry.
            var serializedConfigs = Configuration
                .Where(kvp => !string.IsNullOrEmpty(kvp.Key) && kvp.Value != null && kvp.Value.HasValue)
                .ToList();
            if (serializedConfigs.Count > 0)
            {
                var configs = new JObject();
                foreach (var config in serializedConfigs)
                    configs.Add(config.Key, config.Value.SerializeObject());

                jo.Add("configuration", configs);
            }

            if (ValueInConnection.Count > 0)
            {
                var values = new JObject();
                foreach (var value in ValueInConnection)
                    values.Add(value.Key, value.Value.SerializeObject());
                jo.Add("values", values);
            }

            var connectedFlows = FlowConnections.Where(flow => flow.Value.Node != null).ToList();
            if (connectedFlows.Count > 0)
            {
                var flows = new JObject();
                foreach (var flow in connectedFlows)
                    flows.Add(flow.Key, flow.Value.SerializeObject());
                jo.Add("flows", flows);
            }

            return jo;
        }
        
        public class ConfigData
        {
            // data field holds index in list of types supported in the extension
            public object Value = null;

            /// <summary>
            /// False for unset values and empty arrays: configuration values must be non-empty arrays, so these
            /// are omitted, which selects the default configuration of the operation (e.g. no cases for flow/switch).
            /// </summary>
            public bool HasValue => Value != null && !(Value is System.Array array && array.Length == 0);

            public JObject SerializeObject()
            {
                if (Value == null)
                    return null;
                
                var jObject = new JObject
                {
                };
                ValueSerializer.Serialize(Value, jObject);
                return jObject;
            }
        }

        /// <summary>
        /// Describes a socket connection's data.
        ///
        /// Only outgoing connections from this node to the next are required to be serialized.
        /// </summary>
        public abstract class SocketData
        {
            public string Socket = null;
            public int? Node = null;
        
            public override string ToString()
            {
                return $"Node: {(Node.HasValue ? Node.Value.ToString() : "null")}, Socket: \"{Socket}\"";
            }
        }
        
        public class EventValues
        {
            public int Type = -1;
            public object Value = null;
            
            public JObject SerializeObject()
            {
                JObject valueObject = new JObject()
                {
                    new JProperty("type", Type),
                };
                ValueSerializer.Serialize(Value, valueObject);
                
                return valueObject;
            }
            
            public override string ToString()
            {
                return $"{base.ToString()}, Type: {Type}";
            }
        }

        /// <summary>
        /// Describes Flow data for the node.
        ///
        /// Only outgoing connections from this node to the next are required to be serialized.
        /// </summary>
        public class FlowSocketData : SocketData
        {
            public JObject SerializeObject()
            {
                var jObject = new JObject
                {
                    new JProperty("node", Node)
                };
                // Optional, "in" when omitted
                if (Socket != null)
                    jObject.Add(new JProperty("socket", Socket));
                return jObject;
            }
        }
        
        public static class ValueSerializer
        {
            public static void Serialize(object value, JObject valueObject)
            {
                if (value == null)
                    return;

                if (value is StaticRefPointer staticRef)
                {
                    valueObject.Add(new JProperty("value", new JArray(staticRef.pointer)));
                }
                else
                if (value is Color color)
                {
                    valueObject.Add(new JProperty("value", new JArray(color.r, color.g, color.b, color.a)));
                }
                else if (value is Color32 color32)
                {
                    Color col = color32;
                    valueObject.Add(new JProperty("value", new JArray(col.r, col.g, col.b, col.a)));
                }
                else if (value is Matrix4x4 m4)
                {
                    valueObject.Add(new JProperty("value", new JArray(
                        m4.m00, m4.m10, m4.m20, m4.m30,
                        m4.m01, m4.m11, m4.m21, m4.m31,
                        m4.m02, m4.m12, m4.m22, m4.m32,
                        m4.m03, m4.m13, m4.m23, m4.m33)));
                }
                else if (value is GltfFloat2x2 f2x2)
                {
                    valueObject.Add(new JProperty("value", new JArray(
                        f2x2.m0, f2x2.m1, f2x2.m2, f2x2.m3)));
                }
                else if (value is GltfFloat3x3 f3x3)
                {
                    valueObject.Add(new JProperty("value", new JArray(
                        f3x3.m0, f3x3.m1, f3x3.m2,
                        f3x3.m3, f3x3.m4, f3x3.m5,
                        f3x3.m6, f3x3.m7, f3x3.m8)));
                }
                else if (value is Vector4 v4)
                {
                    valueObject.Add(new JProperty("value", new JArray(v4.x, v4.y, v4.z, v4.w)));
                }
                else if (value is Vector3 v3)
                {
                    valueObject.Add(new JProperty("value", new JArray(v3.x, v3.y, v3.z)));
                }
                else if (value is Vector2 v2)
                {
                    valueObject.Add(new JProperty("value", new JArray(v2.x, v2.y)));
                }
                else if (value is Quaternion q)
                {
                    valueObject.Add(new JProperty("value", new JArray(q.x, q.y, q.z, q.w)));
                }
                else if (value is bool b)
                {
                    valueObject.Add(new JProperty("value", new JArray(b)));
                }
                else if (value is string s)
                {
                    valueObject.Add(new JProperty("value", new JArray(s)));
                }
                else if (value is int i)
                {
                    valueObject.Add(new JProperty("value", new JArray(i)));
                }
                else if (value is float f)
                {
                    valueObject.Add(new JProperty("value", new JArray(f)));
                }
                else
                {
                    valueObject.Add(new JProperty("value", new JArray(value)));
                }
            }    
        }

        public class OutputValueSocketData
        {
            public ExpectedType expectedType;
        }
        
        /// <summary>
        /// Describes value data for the node.
        ///
        /// Either the Value field will be used when the socket is defined by a literal in-line
        /// value or the Node and Socket fields will be used when the socket gets/sets the value
        /// through a connection to another Node's value socket.
        /// </summary>
        public class ValueSocketData : SocketData
        {
            public int Type = -1;
            public object Value = null;
            
            public TypeRestriction typeRestriction = null;

            public JObject SerializeObject()
            {
                JObject valueObject = new JObject()
                {
                };

                // Optional fields are only added if non-null
                if (Node != null)
                {
                    valueObject.Add(new JProperty("node", Node));
                }

                if (Socket != null)
                {
                    valueObject.Add(new JProperty("socket", Socket));
                }

                if (Value != null)
                {
                    valueObject.Add(new JProperty("type", Type));

                    ValueSerializer.Serialize(Value, valueObject);
                }
                else if (Node == null && Type != -1)
                {
                    // Type-default value (e.g. NaN for float): the type is required
                    valueObject.Add(new JProperty("type", Type));
                }

                return valueObject;
            }

            public override string ToString()
            {
                return $"{base.ToString()}, Value: {Value}";
            }
        }
    }

}
