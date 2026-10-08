using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace UnityGLTF.Interactivity.Playback
{
    public static class NodesDeserializer
    {
        public static List<Node> GetNodes(JObject jObj, List<Type> types, List<Declaration> declarations)
        {
            var jNodes = new List<JToken>(GraphJson.OptionalArray(jObj, ConstStrings.NODES, "graph"));
            var nodes = new List<Node>(jNodes.Count);

            for (int i = 0; i < jNodes.Count; i++)
            {
                var context = $"nodes[{i}]";
                var jNode = GraphJson.AsObject(jNodes[i], context);
                var declaration = declarations[GraphJson.RequiredIndex(jNode, ConstStrings.DECLARATION, declarations.Count, context)];

                nodes.Add(new Node()
                {
                    type = declaration.op,
                    declaration = declaration,
                    metadata = GetMetadata(jNode),
                    configuration = GetConfiguration(GraphJson.OptionalObject(jNode, ConstStrings.CONFIGURATION, context), context)
                });
            }

            // Values and flows refer to other nodes so they're resolved once every node exists.
            for (int i = 0; i < jNodes.Count; i++)
            {
                var context = $"nodes[{i}]";
                nodes[i].values = GetValues(GraphJson.OptionalObject(jNodes[i], ConstStrings.VALUES, context), nodes, i, types, context);
                nodes[i].flows = GetFlows(nodes[i], GraphJson.OptionalObject(jNodes[i], ConstStrings.FLOWS, context), nodes, i, context);
            }

            return nodes;
        }

        private static List<Flow> GetFlows(Node fromNode, JObject jFlows, List<Node> nodes, int nodeIndex, string context)
        {
            var flows = new List<Flow>();

            if (jFlows == null)
                return flows;

            foreach (var v in jFlows)
            {
                var flowContext = $"{context}.flows.{v.Key}";
                GraphJson.AsObject(v.Value, flowContext);

                var targetIndex = GraphJson.RequiredIndex(v.Value, ConstStrings.NODE, nodes.Count, flowContext);

                // Output flows may only point forward, which guarantees flow sockets do not form loops.
                if (targetIndex <= nodeIndex)
                    GraphJson.Reject($"{flowContext}: node {targetIndex} must be greater than the current node index {nodeIndex}.");

                var toSocket = GraphJson.OptionalString(v.Value, ConstStrings.SOCKET, flowContext) ?? ConstStrings.IN;

                flows.Add(new Flow(fromNode, v.Key, nodes[targetIndex], toSocket));
            }

            return flows;
        }

        private static List<Value> GetValues(JObject jValues, List<Node> nodes, int nodeIndex, List<Type> types, string context)
        {
            var values = new List<Value>();

            if (jValues == null)
                return values;

            foreach (var kvp in jValues)
            {
                var valueContext = $"{context}.values.{kvp.Key}";
                var jSocket = GraphJson.AsObject(kvp.Value, valueContext);

                var jNode = jSocket[ConstStrings.NODE];
                var jValue = jSocket[ConstStrings.VALUE];
                var jType = jSocket[ConstStrings.TYPE];

                if (jNode != null && jValue != null)
                    GraphJson.Reject($"{valueContext}: \"node\" and \"value\" cannot both be defined.");

                Type declaredType = null;

                if (jType != null)
                    declaredType = types[GraphJson.Index(jType, types.Count, $"{valueContext}: \"type\"")];

                if (jNode != null)
                {
                    // Value references may only point backward, which guarantees value sockets do not form loops.
                    var sourceIndex = GraphJson.Index(jNode, nodeIndex, $"{valueContext}: \"node\"");

                    values.Add(new Value()
                    {
                        id = kvp.Key,
                        node = nodes[sourceIndex],
                        socket = GraphJson.OptionalString(jSocket, ConstStrings.SOCKET, valueContext) ?? ConstStrings.VALUE,
                        declaredType = declaredType
                    });

                    continue;
                }

                if (declaredType == null)
                    GraphJson.Reject($"{valueContext}: \"type\" is required for inline and type-default values.");

                // A missing "value" array means the type-default value.
                values.Add(new Value()
                {
                    id = kvp.Key,
                    property = GraphJson.ParseValue(declaredType, GraphJson.OptionalValueArray(jSocket, valueContext), valueContext),
                    declaredType = declaredType
                });
            }

            return values;
        }

        private static List<Configuration> GetConfiguration(JObject jConfiguration, string context)
        {
            var configuration = new List<Configuration>();

            if (jConfiguration == null)
                return configuration;

            foreach (var v in jConfiguration)
            {
                var raw = (v.Value as JObject)?[ConstStrings.VALUE] as JArray;
                var parsedSuccessfully = TryGetPropertyFromConfigEntry(v.Key, raw, out IProperty property);

                if (!parsedSuccessfully)
                    Util.LogWarning($"{context}: configuration \"{v.Key}\" is missing, unknown or invalid.");

                configuration.Add(new Configuration()
                {
                    id = v.Key,
                    property = property,
                    parsedSuccessfully = parsedSuccessfully,
                    raw = raw
                });
            }

            return configuration;
        }

        private static Metadata GetMetadata(JToken jNode)
        {
            // Older exports wrote a top-level "metadata" object; newer ones keep it in "extras".
            var jToken = jNode[ConstStrings.METADATA] ?? jNode["extras"]?[ConstStrings.METADATA];

            if (jToken == null)
                return new Metadata();

            return new Metadata()
            {
                positionX = ReadDouble(jToken["positionX"]),
                positionY = ReadDouble(jToken["positionY"]),
            };

            static double ReadDouble(JToken t)
            {
                if (t == null)
                    return 0;

                if (t.Type == JTokenType.String)
                    return double.TryParse(t.Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;

                return t.Type == JTokenType.Float || t.Type == JTokenType.Integer ? t.Value<double>() : 0;
            }
        }

        /// <summary>
        /// Configuration values are implicitly typed by the operation; the type is looked up by property name.
        /// Returns false when the property is unknown or its value does not match the expected configuration type.
        /// </summary>
        private static bool TryGetPropertyFromConfigEntry(string id, JArray value, out IProperty property)
        {
            property = null;

            if (value == null)
                return false;

            var type = GetConfigurationType(id);

            if (type == null)
                return false;

            if (type == typeof(int))
            {
                if (value.Count != 1 || !Helpers.IsExactInt32(value[0]))
                    return false;

                property = new Property<int>((int)value[0].Value<double>());
                return true;
            }

            if (type == typeof(int[]))
            {
                if (value.Count < 1)
                    return false;

                var arr = new int[value.Count];

                for (int i = 0; i < arr.Length; i++)
                {
                    if (!Helpers.IsExactInt32(value[i]))
                        return false;

                    arr[i] = (int)value[i].Value<double>();
                }

                property = new Property<int[]>(arr);
                return true;
            }

            if (type == typeof(bool))
            {
                if (value.Count != 1 || value[0].Type != JTokenType.Boolean)
                    return false;

                property = new Property<bool>(value[0].Value<bool>());
                return true;
            }

            if (value.Count != 1 || value[0].Type != JTokenType.String)
                return false;

            property = new Property<string>(value[0].Value<string>());
            return true;
        }

        /// <summary>Configuration type for each configuration property name used by supported operations.</summary>
        public static Type GetConfigurationType(string id)
        {
            return id switch
            {
                ConstStrings.POINTER => typeof(string),
                ConstStrings.MESSAGE => typeof(string),
                ConstStrings.ORDER => typeof(string),
                ConstStrings.VARIABLE => typeof(int),
                ConstStrings.TYPE => typeof(int),
                ConstStrings.EVENT => typeof(int),
                ConstStrings.SEVERITY => typeof(int),
                ConstStrings.INPUT_FLOWS => typeof(int),
                ConstStrings.INITIAL_INDEX => typeof(int),
                ConstStrings.NODE_INDEX => typeof(int),
                ConstStrings.CASES => typeof(int[]),
                ConstStrings.VARIABLES => typeof(int[]),
                ConstStrings.USE_SLERP => typeof(bool),
                ConstStrings.IS_LOOP => typeof(bool),
                ConstStrings.IS_RANDOM => typeof(bool),
                ConstStrings.STOP_PROPAGATION => typeof(bool),
                _ => null,
            };
        }
    }
}
