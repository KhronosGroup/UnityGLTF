using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class GraphConverter : JsonConverter<KHR_interactivity>
    {
        public override void WriteJson(JsonWriter writer, KHR_interactivity value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(ConstStrings.GRAPHS);
            writer.WriteStartArray();

            for (int i = 0; i < value.graphs.Count; i++)
            {
                WriteGraph(writer, value.graphs[i]);
            }

            writer.WriteEndArray();
            writer.WritePropertyName(ConstStrings.GRAPH);
            writer.WriteValue(0); // TODO: Default graph selection for users?
            writer.WriteEndObject();
        }

        private void WriteGraph(JsonWriter writer, Graph graph)
        {
            writer.WriteStartObject();
            var typeIndexByType = TypesSerializer.GetSystemTypeByIndexDictionary(graph);
            var declarations = DeclarationsSerializer.GetDeclarations(graph.nodes, typeIndexByType);
            TypesSerializer.WriteJson(writer, graph.types);
            VariablesSerializer.WriteJson(writer, graph.variables, typeIndexByType);
            EventsSerializer.WriteJson(writer, graph.customEvents, typeIndexByType);
            DeclarationsSerializer.WriteJson(writer, declarations);
            NodesSerializer.WriteJson(writer, graph.nodes, declarations, typeIndexByType);
            writer.WriteEndObject();
        }

        public override KHR_interactivity ReadJson(JsonReader reader, System.Type objectType, KHR_interactivity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObj = JObject.Load(reader);

            var jGraphs = jObj[ConstStrings.GRAPHS];

            var interactivity = new KHR_interactivity();

            foreach (JObject jGraph in jGraphs)
            {
                interactivity.graphs.Add(GenerateGraph(jGraph));
            }

            interactivity.defaultGraphIndex = jObj[ConstStrings.GRAPH].Value<int>();

            return interactivity;
        }

        private static Graph GenerateGraph(JObject jObj)
        {
            var types = TypesDeserializer.GetTypes(jObj);
            var systemTypes = TypesDeserializer.GetSystemTypes(types);
            var variables = VariablesDeserializer.GetVariables(jObj, systemTypes);
            var events = EventsDeserializer.GetEvents(jObj, systemTypes);
            var declarations = DeclarationsDeserializer.GetDeclarations(jObj, systemTypes);
            var nodes = NodesDeserializer.GetNodes(jObj, systemTypes, declarations);

            return new Graph()
            {
                types = types,
                variables = variables,
                customEvents = events,
                nodes = nodes,
                declarations = declarations,
                systemTypes = systemTypes
            };
        }
    }
}