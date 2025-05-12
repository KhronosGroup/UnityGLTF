using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public static class DeclarationsSerializer
    {
        public struct DeclarationData
        {
            public int index;
            public Declaration declaration;
        }

        public static Dictionary<string, DeclarationData> GetDeclarations(List<Node> nodes, Dictionary<Type, int> typeIndexByType)
        {
            var declarations = new Dictionary<string, DeclarationData>();

            string nodeName;
            var declarationCount = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                nodeName = nodes[i].type;

                if (declarations.ContainsKey(nodeName))
                    continue;

                var dec = new DeclarationData()
                {
                    index = declarationCount++,
                    declaration = GetDeclaration(nodes[i].type, typeIndexByType)
                };

                declarations.Add(nodeName, dec);
            }

            return declarations;
        }

        internal static void WriteJson(JsonWriter writer, Dictionary<string, DeclarationData> declarations)
        {
            writer.WritePropertyName(ConstStrings.DECLARATIONS);
            writer.WriteStartArray();

            var orderedDeclarations = new Declaration[declarations.Count];

            foreach (var kvp in declarations)
            {
                orderedDeclarations[kvp.Value.index] = kvp.Value.declaration;
            }

            for (int i = 0; i < orderedDeclarations.Length; i++)
            {
                WriteDeclaration(writer, orderedDeclarations[i]);
            }

            writer.WriteEndArray();
        }

        private static void WriteDeclaration(JsonWriter writer, Declaration declaration)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(ConstStrings.OP);
            writer.WriteValue(declaration.op);

            if (!string.IsNullOrWhiteSpace(declaration.extension))
            {
                writer.WritePropertyName(ConstStrings.EXTENSION);
                writer.WriteValue(declaration.extension);
            }

            if (!declaration.outputValueSockets.IsNullOrEmpty())
                WriteValueSockets(writer, ConstStrings.OUTPUT_VALUE_SOCKETS, declaration.outputValueSockets);

            if (!declaration.inputValueSockets.IsNullOrEmpty())
                WriteValueSockets(writer, ConstStrings.INPUT_VALUE_SOCKETS, declaration.inputValueSockets);

            writer.WriteEndObject();
        }

        private static void WriteValueSockets(JsonWriter writer, string propertyName, List<ValueSocket> valueSockets)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStartObject();
            for (int i = 0; i < valueSockets.Count; i++)
            {
                writer.WritePropertyName(valueSockets[i].name);
                writer.WriteStartObject();
                writer.WritePropertyName(ConstStrings.TYPE);
                writer.WriteValue(valueSockets[i].type);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        private static Declaration GetDeclaration(string id, Dictionary<Type, int> typeIndexByType)
        {
            // Only extension nodes need fancy declarations.
            return id switch
            {
                "event/onSelect" => new Declaration()
                {
                    op = "event/onSelect",
                    extension = GLTF.Schema.KHR_node_selectability_Factory.EXTENSION_NAME,
                    outputValueSockets = new List<ValueSocket>()
                    {
                        new ValueSocket(ConstStrings.SELECTED_NODE_INDEX, typeIndexByType[typeof(int)]),
                        new ValueSocket(ConstStrings.CONTROLLER_INDEX, typeIndexByType[typeof(int)]),
                        new ValueSocket(ConstStrings.SELECTION_POINT, typeIndexByType[typeof(float3)]),
                        new ValueSocket(ConstStrings.SELECTION_RAY_ORIGIN, typeIndexByType[typeof(float3)]),
                    }
                },
                "event/onHoverIn" => new Declaration()
                {
                    op = "event/onHoverIn",
                    extension = GLTF.Schema.KHR_node_hoverability_Factory.EXTENSION_NAME,
                    outputValueSockets = new List<ValueSocket>()
                    {
                        new ValueSocket(ConstStrings.HOVER_NODE_INDEX, typeIndexByType[typeof(int)]),
                        new ValueSocket(ConstStrings.CONTROLLER_INDEX, typeIndexByType[typeof(int)]),
                    }
                },
                "event/onHoverOut" => new Declaration()
                {
                    op = "event/onHoverOut",
                    extension = GLTF.Schema.KHR_node_hoverability_Factory.EXTENSION_NAME,
                    outputValueSockets = new List<ValueSocket>()
                    {
                        new ValueSocket(ConstStrings.HOVER_NODE_INDEX, typeIndexByType[typeof(int)]),
                        new ValueSocket(ConstStrings.CONTROLLER_INDEX, typeIndexByType[typeof(int)]),
                    }
                },
                _ => new Declaration() { op = id }
            };
        }
    }
}