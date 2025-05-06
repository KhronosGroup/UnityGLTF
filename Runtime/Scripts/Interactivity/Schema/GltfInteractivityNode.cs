using UnityEngine;

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
                        Type = GltfTypes.TypeIndexByGltfSignature(descriptor.Value.SupportedTypes[0]),
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
            var configs = new JObject();
            foreach (var config in Configuration)
                configs.Add(config.Key, config.Value.SerializeObject());
            
            var values = new JObject();
            foreach (var value in ValueInConnection)
                values.Add(value.Key, value.Value.SerializeObject());

            var flows = new JObject();
            foreach (var flow in FlowConnections)
                if (flow.Value.Node != null)
                    flows.Add(flow.Key, flow.Value.SerializeObject());

            
            JObject jo = new JObject
            {
                new JProperty("declaration", OpDeclaration),
                new JProperty("configuration",configs),
                new JProperty("values", values),
                new JProperty("flows", flows),
            };

            // Remove all empty arrays in the first level of the JSON Object
            jo.SelectTokens("$.*")
              .OfType<JArray>()
              .Where(x => x.Type == JTokenType.Array && !x.HasValues)
              .Select(a => a.Parent)
              .ToList()
              .ForEach(a => a.Remove());

            return jo;
        }
        
        public class ConfigData
        {
            // data field holds index in list of types supported in the extension
            public object Value = null;

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
                return new JObject
                {
                    new JProperty("node", Node),
                    new JProperty("socket", Socket)
                };
            }
        }
        
        public static class ValueSerializer
        {
            public static void Serialize(object value, JObject valueObject)
            {
                if (value == null)
                    return;
                
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
                    // TODO check if this is the correct row-column order
                    valueObject.Add(new JProperty("value", new JArray(
                        m4.m00, m4.m01, m4.m02, m4.m03,
                        m4.m10, m4.m11, m4.m12, m4.m13,
                        m4.m20, m4.m21, m4.m22, m4.m23,
                        m4.m30, m4.m31, m4.m32, m4.m33)));
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

                return valueObject;
            }

            public override string ToString()
            {
                return $"{base.ToString()}, Value: {Value}";
            }
        }
    }

}
