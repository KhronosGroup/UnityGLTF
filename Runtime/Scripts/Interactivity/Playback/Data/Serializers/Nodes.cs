using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityGLTF.Interactivity.Playback
{
    public static class NodesSerializer
    {
        public static void WriteJson(JsonWriter writer, List<Node> nodes, Dictionary<string, DeclarationsSerializer.DeclarationData> declarations, Dictionary<Type, int> typeIndexByType)
        {
            writer.WritePropertyName(ConstStrings.NODES);
            writer.WriteStartArray();

            for (int i = 0; i < nodes.Count; i++)
            {
                WriteNode(writer, nodes, i, declarations, typeIndexByType);
            }

            writer.WriteEndArray();
        }

        private static void WriteNode(JsonWriter writer, List<Node> nodes, int nodeIndex, Dictionary<string, DeclarationsSerializer.DeclarationData> declarations, Dictionary<Type, int> typeIndexByType)
        {
            var node = nodes[nodeIndex];

            writer.WriteStartObject();

            writer.WritePropertyName(ConstStrings.DECLARATION);
            writer.WriteValue(declarations[node.type].index);

            WriteConfiguration(writer, node.configuration);
            WriteValues(writer, nodes, nodeIndex, typeIndexByType);
            WriteFlows(writer, nodes, nodeIndex);
            WriteMetadata(writer, node.metadata);

            writer.WriteEndObject();
        }

        private static void WriteMetadata(JsonWriter writer, Metadata metadata)
        {
            writer.WritePropertyName(ConstStrings.METADATA);
            writer.WriteStartObject();

            writer.WritePropertyName("positionX");
            writer.WriteValue(metadata.positionX.ToString());

            writer.WritePropertyName("positionY");
            writer.WriteValue(metadata.positionY.ToString());

            writer.WriteEndObject();
        }

        private static void WriteFlows(JsonWriter writer, List<Node> nodes, int nodeIndex)
        {
            var flows = nodes[nodeIndex].flows;

            writer.WritePropertyName(ConstStrings.FLOWS);
            writer.WriteStartObject();

            for (int i = 0; i < flows.Count; i++)
            {
                WriteFlow(writer, nodes, flows[i]);
            }

            writer.WriteEndObject();
        }

        private static void WriteFlow(JsonWriter writer, List<Node> nodes, Flow flow)
        {
            writer.WritePropertyName(flow.fromSocket);
            writer.WriteStartObject();

            writer.WritePropertyName(ConstStrings.NODE);

            var targetNodeIndex = nodes.IndexOf(flow.toNode);
            Assert.AreNotEqual(targetNodeIndex, -1);
            writer.WriteValue(targetNodeIndex);

            writer.WritePropertyName(ConstStrings.SOCKET);
            writer.WriteValue(flow.toSocket);

            writer.WriteEndObject();
        }

        private static void WriteConfiguration(JsonWriter writer, List<Configuration> configuration)
        {
            writer.WritePropertyName(ConstStrings.CONFIGURATION);
            writer.WriteStartObject();

            for (int i = 0; i < configuration.Count; i++)
            {
                WriteConfigurationEntry(writer, configuration[i]);
            }

            writer.WriteEndObject();
        }

        private static void WriteConfigurationEntry(JsonWriter writer, Configuration configuration)
        {
            writer.WritePropertyName(configuration.id);

            writer.WriteStartObject();
            writer.WritePropertyName(ConstStrings.VALUE);

            if (configuration.value is JArray array)
                array.WriteTo(writer);
            else
                writer.WriteValue(configuration.value);

            writer.WriteEndObject();
        }

        private static void WriteValues(JsonWriter writer, List<Node> nodes, int nodeIndex, Dictionary<Type, int> typeIndexByType)
        {
            var values = nodes[nodeIndex].values;

            writer.WritePropertyName(ConstStrings.VALUES);
            writer.WriteStartObject();

            for (int i = 0; i < values.Count; i++)
            {
                WriteValue(writer, nodes, values[i], typeIndexByType);
            }

            writer.WriteEndObject();
        }

        private static void WriteValue(JsonWriter writer, List<Node> nodes, Value value, Dictionary<Type, int> typeIndexByType)
        {
            writer.WritePropertyName(value.id);
            writer.WriteStartObject();

            if (value.node != null)
            {
                writer.WritePropertyName(ConstStrings.NODE);
                var targetNodeIndex = nodes.IndexOf(value.node);
                Assert.AreNotEqual(targetNodeIndex, -1);
                writer.WriteValue(targetNodeIndex);

                writer.WritePropertyName(ConstStrings.SOCKET);
                writer.WriteValue(value.socket);
            }
            else
            {
                WriteValueLiteral(writer, value.property, typeIndexByType);
            }

            writer.WriteEndObject();
        }

        public static void WriteValueLiteral(JsonWriter writer, IProperty property, Dictionary<Type, int> typeIndexByType)
        {
            writer.WritePropertyName(ConstStrings.VALUE);
            writer.WriteStartArray();

            var type = Constants.INVALID_TYPE_INDEX;

            switch (property)
            {
                case Property<int> iProp:
                    writer.WriteValue(iProp.value);
                    type = typeIndexByType[typeof(int)];
                    break;
                case Property<float> fProp:
                    writer.WriteValue(fProp.value);
                    type = typeIndexByType[typeof(float)];
                    break;
                case Property<bool> bProp:
                    writer.WriteValue(bProp.value);
                    type = typeIndexByType[typeof(bool)];
                    break;
                case Property<float2> f2Prop:
                    writer.WriteValue(f2Prop.value.x);
                    writer.WriteValue(f2Prop.value.y);
                    type = typeIndexByType[typeof(float2)];
                    break;
                case Property<float3> f3Prop:
                    writer.WriteValue(f3Prop.value.x);
                    writer.WriteValue(f3Prop.value.y);
                    writer.WriteValue(f3Prop.value.z);
                    type = typeIndexByType[typeof(float3)];
                    break;
                case Property<float4> f4Prop:
                    writer.WriteValue(f4Prop.value.x);
                    writer.WriteValue(f4Prop.value.y);
                    writer.WriteValue(f4Prop.value.z);
                    writer.WriteValue(f4Prop.value.w);
                    type = typeIndexByType[typeof(float4)];
                    break;
                case Property<float2x2> p:
                    writer.WriteValue(p.value.c0.x);
                    writer.WriteValue(p.value.c0.y);
                    writer.WriteValue(p.value.c1.x);
                    writer.WriteValue(p.value.c0.y);
                    type = typeIndexByType[typeof(float2x2)];
                    break;
                case Property<float3x3> p:
                    writer.WriteValue(p.value.c0.x);
                    writer.WriteValue(p.value.c0.y);
                    writer.WriteValue(p.value.c0.z);
                    writer.WriteValue(p.value.c1.x);
                    writer.WriteValue(p.value.c1.y);
                    writer.WriteValue(p.value.c1.z);
                    writer.WriteValue(p.value.c2.x);
                    writer.WriteValue(p.value.c2.y);
                    writer.WriteValue(p.value.c2.z);
                    type = typeIndexByType[typeof(float3x3)];
                    break;
                case Property<float4x4> p:
                    writer.WriteValue(p.value.c0.x);
                    writer.WriteValue(p.value.c0.y);
                    writer.WriteValue(p.value.c0.z);
                    writer.WriteValue(p.value.c0.w);
                    writer.WriteValue(p.value.c1.x);
                    writer.WriteValue(p.value.c1.y);
                    writer.WriteValue(p.value.c1.z);
                    writer.WriteValue(p.value.c1.w);
                    writer.WriteValue(p.value.c2.x);
                    writer.WriteValue(p.value.c2.y);
                    writer.WriteValue(p.value.c2.z);
                    writer.WriteValue(p.value.c2.w);
                    writer.WriteValue(p.value.c3.x);
                    writer.WriteValue(p.value.c3.y);
                    writer.WriteValue(p.value.c3.z);
                    writer.WriteValue(p.value.c3.w);
                    type = typeIndexByType[typeof(float4x4)];
                    break;
                case Property<string> p:
                    writer.WriteValue(p.value);
                    type = typeIndexByType[typeof(string)];
                    break;
                default:
                    throw new NotImplementedException();
            }

            writer.WriteEndArray();

            writer.WritePropertyName(ConstStrings.TYPE);
            writer.WriteValue(type);
        }
    }
}