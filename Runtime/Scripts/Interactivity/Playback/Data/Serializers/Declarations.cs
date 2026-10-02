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

        /// <summary>One declaration per distinct operation, in order of first use.</summary>
        public static Dictionary<string, DeclarationData> GetDeclarations(List<Node> nodes, Dictionary<Type, int> typeIndexByType)
        {
            var declarations = new Dictionary<string, DeclarationData>();

            foreach (var node in nodes)
            {
                Add(declarations, node.type, node.declaration, typeIndexByType);
            }

            return declarations;
        }

        public static Dictionary<string, DeclarationData> GetDeclarations(NodesSerializer.Plan plan, Dictionary<Type, int> typeIndexByType)
        {
            var declarations = new Dictionary<string, DeclarationData>();

            foreach (var e in plan.nodes)
            {
                Add(declarations, e.op, e.source?.declaration, typeIndexByType);
            }

            return declarations;
        }

        private static void Add(Dictionary<string, DeclarationData> declarations, string op, Declaration existing, Dictionary<Type, int> typeIndexByType)
        {
            if (declarations.ContainsKey(op))
                return;

            declarations.Add(op, new DeclarationData()
            {
                index = declarations.Count,
                // Declarations read from JSON keep their extension and socket types as written.
                declaration = existing ?? GetDeclaration(op, typeIndexByType)
            });
        }

        /// <summary>Types referenced by the extension declarations this serializer generates for an operation.</summary>
        public static IEnumerable<Type> GetRequiredTypes(string op)
        {
            switch (op)
            {
                case "event/onSelect":
                    return new[] { typeof(Ref), typeof(int), typeof(float3) };
                case "event/onHoverIn":
                case "event/onHoverOut":
                    return new[] { typeof(Ref), typeof(int) };
                default:
                    return Array.Empty<Type>();
            }
        }

        internal static void WriteJson(JsonWriter writer, Dictionary<string, DeclarationData> declarations)
        {
            if (declarations.Count == 0)
                return;

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

            // Value sockets may only be declared for extension operations.
            if (!string.IsNullOrWhiteSpace(declaration.extension))
            {
                writer.WritePropertyName(ConstStrings.EXTENSION);
                writer.WriteValue(declaration.extension);

                if (!declaration.inputValueSockets.IsNullOrEmpty())
                    WriteValueSockets(writer, ConstStrings.INPUT_VALUE_SOCKETS, declaration.inputValueSockets);

                if (!declaration.outputValueSockets.IsNullOrEmpty())
                    WriteValueSockets(writer, ConstStrings.OUTPUT_VALUE_SOCKETS, declaration.outputValueSockets);
            }

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
                        new ValueSocket(ConstStrings.SELECTED_NODE, typeIndexByType[typeof(Ref)]),
                        new ValueSocket(ConstStrings.CONTROLLER_INDEX, typeIndexByType[typeof(int)]),
                        new ValueSocket(ConstStrings.SELECTION_POINT, typeIndexByType[typeof(float3)]),
                    }
                },
                "event/onHoverIn" => new Declaration()
                {
                    op = "event/onHoverIn",
                    extension = GLTF.Schema.KHR_node_hoverability_Factory.EXTENSION_NAME,
                    outputValueSockets = new List<ValueSocket>()
                    {
                        new ValueSocket(ConstStrings.HOVER_NODE, typeIndexByType[typeof(Ref)]),
                        new ValueSocket(ConstStrings.CONTROLLER_INDEX, typeIndexByType[typeof(int)]),
                    }
                },
                "event/onHoverOut" => new Declaration()
                {
                    op = "event/onHoverOut",
                    extension = GLTF.Schema.KHR_node_hoverability_Factory.EXTENSION_NAME,
                    outputValueSockets = new List<ValueSocket>()
                    {
                        new ValueSocket(ConstStrings.HOVER_NODE, typeIndexByType[typeof(Ref)]),
                        new ValueSocket(ConstStrings.CONTROLLER_INDEX, typeIndexByType[typeof(int)]),
                    }
                },
                _ => new Declaration() { op = id }
            };
        }
    }
}
