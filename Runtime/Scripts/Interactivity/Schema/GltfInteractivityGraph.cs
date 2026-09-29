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
            JObject jo = new JObject();

            // Empty arrays must be omitted
            void AddArray(string name, IEnumerable<JObject> items)
            {
                var array = new JArray(items);
                if (array.Count > 0)
                    jo.Add(name, array);
            }

            AddArray("types", from type in Types select type.SerializeObject());
            AddArray("variables", from variable in Variables select variable.SerializeObject());
            AddArray("events", from customEvent in CustomEvents select customEvent.SerializeObject());
            AddArray("declarations", from declaration in Declarations select declaration.SerializeObject());
            AddArray("nodes", from node in Nodes select node.SerializeObject());

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

                    // Empty socket objects must be omitted
                    if (inputValueSockets != null && inputValueSockets.Count > 0)
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

                    if (outputValueSockets != null && outputValueSockets.Count > 0)
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
            public string Name = string.Empty;
            public int Type = -1;
            public object Value;

            public JObject SerializeObject()
            {
                var jObject = new JObject
                {
                    new JProperty("type", Type),
                };
                if (!string.IsNullOrEmpty(Name))
                    jObject.Add(new JProperty("name", Name));
                
                GltfInteractivityNode.ValueSerializer.Serialize(Value, jObject);

                return jObject;
            }
        }

        /// <summary> Defines the Custom Events can be sent or received in the graph.</summary>
        public class CustomEvent
        {
            public string Id = string.Empty;
            public string Name = string.Empty;
            public Dictionary<string, GltfInteractivityNode.EventValues> Values = new Dictionary<string, GltfInteractivityNode.EventValues>();

            public JObject SerializeObject()
            {
                var jObject = new JObject();

                // Without id, the event is internal to the graph
                if (!string.IsNullOrEmpty(Id))
                    jObject.Add(new JProperty("id", Id));
                
                if (!string.IsNullOrEmpty(Name))
                    jObject.Add(new JProperty("name", Name));


                // Empty objects must be omitted
                if (Values != null && Values.Count > 0)
                {
                    var values = new JObject();
                    foreach (var value in Values)
                        values.Add(value.Key, value.Value.SerializeObject());
                    jObject.Add(new JProperty("values", values));
                }

                return jObject;
            }
        }
    }
}