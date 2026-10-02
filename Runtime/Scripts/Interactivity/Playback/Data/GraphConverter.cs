using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

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
            writer.WriteValue(value.defaultGraphIndex);
            writer.WriteEndObject();
        }

        private void WriteGraph(JsonWriter writer, Graph graph)
        {
            // Order nodes and synthesize constants first, since that determines which types are needed.
            var plan = NodesSerializer.BuildPlan(graph.nodes);
            var types = TypesSerializer.WithRequiredTypes(graph.types, GetRequiredTypes(graph, plan));
            var typeIndexByType = TypesSerializer.GetSystemTypeByIndexDictionary(types);
            var declarations = DeclarationsSerializer.GetDeclarations(plan, typeIndexByType);

            // Empty arrays are omitted, as in the core glTF specification.
            writer.WriteStartObject();
            TypesSerializer.WriteJson(writer, types);
            VariablesSerializer.WriteJson(writer, graph.variables, typeIndexByType);
            EventsSerializer.WriteJson(writer, graph.customEvents, typeIndexByType);
            DeclarationsSerializer.WriteJson(writer, declarations);
            NodesSerializer.WriteJson(writer, plan, declarations, typeIndexByType);
            writer.WriteEndObject();
        }

        private static IEnumerable<System.Type> GetRequiredTypes(Graph graph, NodesSerializer.Plan plan)
        {
            foreach (var v in graph.variables)
                yield return v.initialValue.GetSystemType();

            foreach (var e in graph.customEvents)
            {
                if (e.values == null)
                    continue;

                foreach (var v in e.values)
                    yield return v.property.GetSystemType();
            }

            foreach (var t in NodesSerializer.GetInlineTypes(plan))
                yield return t;

            foreach (var e in plan.nodes)
            {
                foreach (var t in DeclarationsSerializer.GetRequiredTypes(e.op))
                    yield return t;
            }
        }

        public override KHR_interactivity ReadJson(JsonReader reader, System.Type objectType, KHR_interactivity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObj = JObject.Load(reader);

            var interactivity = new KHR_interactivity();

            if (jObj[ConstStrings.GRAPHS] is not JArray jGraphs)
            {
                interactivity.isValid = false;
                interactivity.errors.Add("\"graphs\" must be an array.");
                return interactivity;
            }

            for (int i = 0; i < jGraphs.Count; i++)
            {
                // Graphs are isolated: one invalid graph does not invalidate the others.
                interactivity.graphs.Add(GenerateGraphOrRejected(jGraphs[i], i));
            }

            var jGraph = jObj[ConstStrings.GRAPH];

            if (jGraph == null)
            {
                interactivity.defaultGraphIndex = 0;
            }
            else if (Helpers.IsExactInt32(jGraph) && jGraph.Value<double>() >= 0 && jGraph.Value<double>() < jGraphs.Count)
            {
                interactivity.defaultGraphIndex = (int)jGraph.Value<double>();
            }
            else
            {
                interactivity.isValid = false;
                interactivity.errors.Add($"\"graph\" must be a non-negative integer less than {jGraphs.Count}.");
            }

            if (interactivity.isValid && interactivity.graphs.Count == 0)
            {
                interactivity.isValid = false;
                interactivity.errors.Add("\"graphs\" is empty.");
            }

            return interactivity;
        }

        private static Graph GenerateGraphOrRejected(JToken jGraph, int index)
        {
            try
            {
                if (jGraph is not JObject jObj)
                    throw new InteractivityGraphException("A graph must be a JSON object.");

                return GenerateGraph(jObj);
            }
            catch (Exception e) when (e is InteractivityGraphException || e is FormatException || e is InvalidCastException || e is OverflowException)
            {
                var graph = new Graph();
                graph.Reject($"graphs[{index}]: {e.Message}");
                UnityEngine.Debug.LogWarning($"KHR_interactivity graph {index} was rejected: {e.Message}");
                return graph;
            }
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