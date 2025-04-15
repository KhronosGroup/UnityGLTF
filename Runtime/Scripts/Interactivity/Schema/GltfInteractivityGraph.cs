using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace UnityGLTF.Interactivity.Schema
{
    public class GltfInteractivityGraph
    {
        // The list of nodes in the behavior graph
        public GltfInteractivityNode[] Nodes = { };

        // The variables that are accessible to the nodes in the graph.
        public Variable[] Variables = { };

        // The list of custom events that can be sent/received in the behavior graph.
        public CustomEvent[] CustomEvents = { };

        public Declaration[] Declarations = { };
        
        public GltfTypes.TypeMapping[] Types = GltfTypes.TypesMapping;
        
        public JObject SerializeObject()
        {
            JObject jo = new JObject
            {
                new JProperty("types",
                    new JArray(
                        from type in Types
                        select type.SerializeObject())),
                new JProperty("variables",
                    new JArray(
                        from variable in Variables
                        select variable.SerializeObject())),
                new JProperty("events",
                    new JArray(
                        from customEvent in CustomEvents
                        select customEvent.SerializeObject())),
                new JProperty("declarations",
                    new JArray(
                        from declaration in Declarations
                        select declaration.SerializeObject())),
                new JProperty("nodes",
                    new JArray(
                        from node in Nodes
                        select node.SerializeObject()))
            };

            return jo;
        }

        
        public class Declaration
        {
            public string op = string.Empty;
            public string extension = null;

            public class ValueSocket
            {
                public int type;
            }
            
            public Dictionary<string, ValueSocket> inputValueSockets;
            public Dictionary<string, ValueSocket> outputValueSockets;
            
            public JObject SerializeObject()
            {
                var jObject = new JObject
                {
                    new JProperty("op", op),
                };
                
                if (extension != null)
                {
                    jObject.Add(new JProperty("extension", extension));

                    if (inputValueSockets != null)
                    {
                        var inputSockets = new JObject();
                        foreach (var socket in inputValueSockets)
                        {
                            inputSockets.Add(socket.Key, new JObject
                            {
                                new JProperty("type", socket.Value.type)
                            });
                        }
                        jObject.Add("inputValueSockets", inputSockets);
                    }

                    if (outputValueSockets != null)
                    {
                        var outputSockets = new JObject();
                        foreach (var socket in outputValueSockets)
                        {
                            outputSockets.Add(socket.Key, new JObject
                            {
                                new JProperty("type", socket.Value.type)
                            });
                        }
                        jObject.Add("outputValueSockets", outputSockets);
                    }

                }
                
                return jObject;
            } 
        }
        
        /// <summary> Variables hold data or references accessible to the behavior graph.</summary>
        public class Variable
        {
            public string Id = string.Empty;
            public int Type = -1;
            public object Value;

            public JObject SerializeObject()
            {
                var jObject = new JObject
                {
                    new JProperty("id", Id),
                    new JProperty("type", Type),
                };
                GltfInteractivityNode.ValueSerializer.Serialize(Value, jObject);

                return jObject;
            }
        }

        /// <summary> Defines the Custom Events can be sent or received in the graph.</summary>
        public class CustomEvent
        {
            public string Id = string.Empty;
            public Dictionary<string, GltfInteractivityNode.EventValues> Values = new Dictionary<string, GltfInteractivityNode.EventValues>();

            public JObject SerializeObject()
            {
                var values = new JObject();
                foreach (var value in Values)
                    values.Add(value.Key, value.Value.SerializeObject());

                return new JObject
                {
                    new JProperty("id", Id),
                    new JProperty("values", values)
                };
            }
        }
    }
}