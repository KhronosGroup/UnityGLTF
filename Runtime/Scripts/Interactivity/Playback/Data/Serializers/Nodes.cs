using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public static class NodesSerializer
    {
        /// <summary>A node as it will be written, either from the graph or synthesized for a non-finite constant.</summary>
        public sealed class EmitNode
        {
            public string op;
            public Node source;
            public readonly List<(string id, EmitValue value)> values = new();
            public double rank;
            internal int index;
        }

        /// <summary>Nodes in write order plus the mapping from graph nodes to their emitted entries.</summary>
        public sealed class Plan
        {
            public readonly List<EmitNode> nodes;
            public readonly Dictionary<Node, EmitNode> byNode;

            public Plan(List<EmitNode> nodes, Dictionary<Node, EmitNode> byNode)
            {
                this.nodes = nodes;
                this.byNode = byNode;
            }
        }

        public sealed class EmitValue
        {
            public IProperty inline;
            public EmitNode node;
            public string socket;
            public Type declaredType;
        }

        /// <summary>
        /// Builds the list of nodes in an order valid for JSON: value sources come before their readers and
        /// flow targets come after their sources. Inline values JSON can't represent (infinities, partial NaNs)
        /// are replaced by synthesized math/Inf, math/NaN, math/neg and math/combine* nodes.
        /// </summary>
        public static Plan BuildPlan(List<Node> nodes)
        {
            var emitted = new List<EmitNode>();
            var byNode = new Dictionary<Node, EmitNode>(nodes.Count);

            for (int i = 0; i < nodes.Count; i++)
            {
                var e = new EmitNode { op = nodes[i].type, source = nodes[i], rank = i };
                byNode[nodes[i]] = e;
                emitted.Add(e);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                var e = byNode[nodes[i]];

                foreach (var v in nodes[i].values)
                {
                    if (v.node != null)
                    {
                        e.values.Add((v.id, new EmitValue { node = byNode[v.node], socket = v.socket, declaredType = v.declaredType }));
                        continue;
                    }

                    if (LiteralSerializer.TryGetFloatComponents(v.property, out var components) &&
                        !LiteralSerializer.AllFinite(components) && !LiteralSerializer.AllNaN(components))
                    {
                        var constant = SynthesizeConstant(v.property, components, e.rank, emitted);
                        e.values.Add((v.id, new EmitValue { node = constant, socket = ConstStrings.VALUE }));
                        continue;
                    }

                    e.values.Add((v.id, new EmitValue { inline = v.property }));
                }
            }

            return new Plan(TopologicalOrder(emitted, byNode), byNode);
        }

        private static EmitNode SynthesizeConstant(IProperty property, float[] components, double consumerRank, List<EmitNode> emitted)
        {
            var rank = consumerRank - 0.5;

            if (property is Property<float>)
                return SynthesizeScalar(components[0], rank, emitted);

            var op = property switch
            {
                Property<float2> => "math/combine2",
                Property<float3> => "math/combine3",
                Property<float4> => "math/combine4",
                Property<float2x2> => "math/combine2x2",
                Property<float3x3> => "math/combine3x3",
                _ => "math/combine4x4",
            };

            var combine = new EmitNode { op = op, rank = rank };

            // Combine inputs a, b, c... take components in JSON order (XYZW / column-major).
            for (int i = 0; i < components.Length; i++)
            {
                var c = components[i];
                var value = float.IsNaN(c) || float.IsInfinity(c)
                    ? new EmitValue { node = SynthesizeScalar(c, rank - 0.25, emitted), socket = ConstStrings.VALUE }
                    : new EmitValue { inline = new Property<float>(c) };

                combine.values.Add((ConstStrings.Letters[i], value));
            }

            emitted.Add(combine);
            return combine;
        }

        private static EmitNode SynthesizeScalar(float value, double rank, List<EmitNode> emitted)
        {
            if (float.IsNaN(value))
                return Add(new EmitNode { op = "math/NaN", rank = rank });

            var inf = Add(new EmitNode { op = "math/Inf", rank = rank - 0.01 });

            if (value > 0)
                return inf;

            var neg = new EmitNode { op = "math/neg", rank = rank };
            neg.values.Add((ConstStrings.A, new EmitValue { node = inf, socket = ConstStrings.VALUE }));
            return Add(neg);

            EmitNode Add(EmitNode n)
            {
                emitted.Add(n);
                return n;
            }
        }

        private static List<EmitNode> TopologicalOrder(List<EmitNode> emitted, Dictionary<Node, EmitNode> byNode)
        {
            var successors = new Dictionary<EmitNode, List<EmitNode>>();
            var inDegree = new Dictionary<EmitNode, int>();

            foreach (var e in emitted)
            {
                successors[e] = new List<EmitNode>();
                inDegree[e] = 0;
            }

            void AddEdge(EmitNode from, EmitNode to)
            {
                successors[from].Add(to);
                inDegree[to]++;
            }

            foreach (var e in emitted)
            {
                foreach (var (_, v) in e.values)
                {
                    if (v.node != null)
                        AddEdge(v.node, e);
                }

                if (e.source == null)
                    continue;

                foreach (var f in e.source.flows)
                {
                    AddEdge(e, byNode[f.toNode]);
                }
            }

            // Kahn's algorithm, preferring the original node order among ready nodes.
            var ready = new List<EmitNode>();
            foreach (var e in emitted)
            {
                if (inDegree[e] == 0)
                    ready.Add(e);
            }

            var ordered = new List<EmitNode>(emitted.Count);

            while (ready.Count > 0)
            {
                var best = 0;
                for (int i = 1; i < ready.Count; i++)
                {
                    if (ready[i].rank < ready[best].rank)
                        best = i;
                }

                var next = ready[best];
                ready.RemoveAt(best);
                next.index = ordered.Count;
                ordered.Add(next);

                foreach (var s in successors[next])
                {
                    if (--inDegree[s] == 0)
                        ready.Add(s);
                }
            }

            if (ordered.Count != emitted.Count)
                throw new InvalidOperationException("The graph contains a cycle of value references and flows and cannot be serialized.");

            return ordered;
        }

        public static void WriteJson(JsonWriter writer, Plan plan, Dictionary<string, DeclarationsSerializer.DeclarationData> declarations, Dictionary<Type, int> typeIndexByType)
        {
            if (plan.nodes.Count == 0)
                return;

            writer.WritePropertyName(ConstStrings.NODES);
            writer.WriteStartArray();

            foreach (var e in plan.nodes)
            {
                WriteNode(writer, e, plan, declarations, typeIndexByType);
            }

            writer.WriteEndArray();
        }

        private static void WriteNode(JsonWriter writer, EmitNode e, Plan plan, Dictionary<string, DeclarationsSerializer.DeclarationData> declarations, Dictionary<Type, int> typeIndexByType)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(ConstStrings.DECLARATION);
            writer.WriteValue(declarations[e.op].index);

            if (e.source != null)
                WriteConfiguration(writer, e.source.configuration);

            WriteValues(writer, e, typeIndexByType);

            if (e.source != null)
            {
                WriteFlows(writer, e.source.flows, plan);
                WriteMetadata(writer, e.source.metadata);
            }

            writer.WriteEndObject();
        }

        private static void WriteMetadata(JsonWriter writer, Metadata metadata)
        {
            if (metadata == null || (metadata.positionX == 0 && metadata.positionY == 0))
                return;

            // Editor-only data lives in "extras" so it doesn't add non-spec properties.
            writer.WritePropertyName("extras");
            writer.WriteStartObject();
            writer.WritePropertyName(ConstStrings.METADATA);
            writer.WriteStartObject();
            writer.WritePropertyName("positionX");
            writer.WriteValue(metadata.positionX);
            writer.WritePropertyName("positionY");
            writer.WriteValue(metadata.positionY);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private static void WriteFlows(JsonWriter writer, List<Flow> flows, Plan plan)
        {
            if (flows.Count == 0)
                return;

            writer.WritePropertyName(ConstStrings.FLOWS);
            writer.WriteStartObject();

            foreach (var flow in flows)
            {
                writer.WritePropertyName(flow.fromSocket);
                writer.WriteStartObject();
                writer.WritePropertyName(ConstStrings.NODE);
                writer.WriteValue(plan.byNode[flow.toNode].index);
                writer.WritePropertyName(ConstStrings.SOCKET);
                writer.WriteValue(flow.toSocket);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }


        private static void WriteConfiguration(JsonWriter writer, List<Configuration> configuration)
        {
            var written = false;

            foreach (var config in configuration)
            {
                if (!TryGetWritableConfig(config, out var property, out var raw))
                    continue;

                if (!written)
                {
                    writer.WritePropertyName(ConstStrings.CONFIGURATION);
                    writer.WriteStartObject();
                    written = true;
                }

                writer.WritePropertyName(config.id);
                writer.WriteStartObject();

                if (property != null)
                {
                    LiteralSerializer.WriteConfigLiteral(writer, property);
                }
                else
                {
                    writer.WritePropertyName(ConstStrings.VALUE);
                    raw.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            if (written)
                writer.WriteEndObject();
        }

        private static bool TryGetWritableConfig(Configuration config, out IProperty property, out JArray raw)
        {
            property = null;
            raw = null;

            switch (config.property)
            {
                case Property<int>:
                case Property<bool>:
                case Property<string>:
                case Property<int[]>:
                    property = config.property;
                    return true;
                // Placeholder created by Node.AddDefaultData, or an unparsed value read from JSON.
                case Property<JArray> placeholder when placeholder.value != null && placeholder.value.Count > 0:
                    raw = placeholder.value;
                    return true;
            }

            if (config.raw != null)
            {
                raw = config.raw;
                return true;
            }

            return false;
        }

        private static void WriteValues(JsonWriter writer, EmitNode e, Dictionary<Type, int> typeIndexByType)
        {
            if (e.values.Count == 0)
                return;

            writer.WritePropertyName(ConstStrings.VALUES);
            writer.WriteStartObject();

            foreach (var (id, v) in e.values)
            {
                writer.WritePropertyName(id);
                writer.WriteStartObject();

                if (v.node != null)
                {
                    writer.WritePropertyName(ConstStrings.NODE);
                    writer.WriteValue(v.node.index);

                    if (v.socket != ConstStrings.VALUE)
                    {
                        writer.WritePropertyName(ConstStrings.SOCKET);
                        writer.WriteValue(v.socket);
                    }

                    if (v.declaredType != null && typeIndexByType.TryGetValue(v.declaredType, out var t))
                    {
                        writer.WritePropertyName(ConstStrings.TYPE);
                        writer.WriteValue(t);
                    }
                }
                else
                {
                    LiteralSerializer.WriteTypedValueOrDefault(writer, v.inline, typeIndexByType, $"node {e.index} ({e.op}) value \"{id}\"");
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        /// <summary>Value types used by inline values in the plan, for building the types array.</summary>
        public static IEnumerable<Type> GetInlineTypes(Plan plan)
        {
            foreach (var e in plan.nodes)
            {
                foreach (var (_, v) in e.values)
                {
                    if (v.inline != null)
                        yield return v.inline.GetSystemType();
                }
            }
        }
    }
}
