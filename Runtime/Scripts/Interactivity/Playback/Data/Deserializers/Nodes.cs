using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace UnityGLTF.Interactivity.Playback
{
    public static class NodesDeserializer
    {
        private struct NodePair
        {
            public Node node;
            public JToken jToken;
        }

        public static List<Node> GetNodes(JObject jObj, List<Type> types, List<Declaration> declarations)
        {
            var jNodes = jObj[ConstStrings.NODES].Children();

            var nodeCount = jNodes.Count();

            var nodes = new List<Node>(nodeCount);

            var nodePairs = ListPool<NodePair>.Get();

            Node node;

            try
            {
                foreach (var jToken in jNodes)
                {
                    var declarationIndex = jToken[ConstStrings.DECLARATION].Value<int>();

                    node = new Node()
                    {
                        type = declarations[declarationIndex].op,
                        metadata = GetMetadata(jToken[ConstStrings.METADATA]),
                        configuration = GetConfiguration(jToken[ConstStrings.CONFIGURATION])
                    };

                    nodes.Add(node);
                    nodePairs.Add(new NodePair()
                    {
                        node = node,
                        jToken = jToken
                    });
                }

                foreach (var nodePair in nodePairs)
                {
                    nodePair.node.values = GetValues(nodePair.jToken[ConstStrings.VALUES], nodes, types);
                    nodePair.node.flows = GetFlows(nodePair.node, nodePair.jToken[ConstStrings.FLOWS], nodes);
                }
            }
            finally
            {
                ListPool<NodePair>.Release(nodePairs);
            }

            return nodes;
        }

        private static List<Flow> GetFlows(Node fromNode, JToken jToken, List<Node> nodes)
        {
            var count = jToken.Count();
            var flows = new List<Flow>(count);
            var jFlows = jToken as JObject;

            foreach (var v in jFlows)
            {
                var jToNode = v.Value[ConstStrings.NODE];
                var jToSocket = v.Value[ConstStrings.SOCKET];

                // Ignore this flow if it's empty/not connected.
                if (jToNode == null || jToSocket == null)
                    continue;

                var fromSocket = v.Key;
                var toNode = nodes[v.Value[ConstStrings.NODE].Value<int>()];
                var toSocket = v.Value[ConstStrings.SOCKET].Value<string>();

                flows.Add(new Flow(
                    fromNode,
                    fromSocket,
                    toNode,
                    toSocket));
            }

            return flows;
        }

        private static List<Value> GetValues(JToken jToken, List<Node> nodes, List<Type> types)
        {
            var count = jToken.Count();
            var values = new List<Value>(count);
            var jValues = jToken as JObject;

            foreach (var kvp in jValues)
            {
                var jType = kvp.Value[ConstStrings.TYPE];
                var jNode = kvp.Value[ConstStrings.NODE];
                var jSocket = kvp.Value[ConstStrings.SOCKET];
                var jValue = kvp.Value[ConstStrings.VALUE];

                int type = Constants.INVALID_TYPE_INDEX;
                Node node = null;
                var socket = Constants.EMPTY_SOCKET_STRING;
                IProperty value = null;

                if (jType != null)
                {
                    type = jType.Value<int>();

                    Assert.IsNotNull(jValue);

                    value = Helpers.CreateProperty(types[type], jValue as JArray);
                }

                if (jNode != null)
                    node = nodes[jNode.Value<int>()];

                if (jSocket != null)
                    socket = jSocket.Value<string>();

                values.Add(new Value()
                {
                    id = kvp.Key,
                    property = value,
                    node = node,
                    socket = socket
                });

                Util.Log($"Created property {kvp.Key} connected to node {node?.type} and socket {socket} with type {type}");
            }

            return values;
        }

        private static List<Configuration> GetConfiguration(JToken jToken)
        {
            var jConfiguration = jToken as JObject;

            var count = jConfiguration.Count;
            var configuration = new List<Configuration>(count);

            foreach (var v in jConfiguration)
            {
                configuration.Add(new Configuration()
                {
                    id = v.Key,
                    value = v.Value[ConstStrings.VALUE] as JArray
                });
            }

            return configuration;
        }

        private static Metadata GetMetadata(JToken jToken)
        {
            if (jToken == null)
                return new Metadata();

            return new Metadata()
            {
                positionX = double.Parse(jToken["positionX"].Value<string>()),
                positionY = double.Parse(jToken["positionY"].Value<string>()),
            };
        }
    }
}