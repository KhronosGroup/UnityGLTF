using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>
    /// Load-time validation of a behavior graph. JSON syntax rules are enforced by the deserializers;
    /// this class checks the rules that need operation knowledge and also applies to graphs built in code.
    /// Failures are recorded with <see cref="Graph.Reject"/>.
    /// </summary>
    public static class GraphValidator
    {
        public static bool Validate(Graph graph)
        {
            var errorsBefore = graph.errors.Count;

            CheckAcyclic(graph);

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                ValidateNode(graph, graph.nodes[i], i);
            }

            if (graph.errors.Count > errorsBefore)
                graph.isValid = false;

            return graph.isValid;
        }

        /// <summary>
        /// Value references and flows together must form a directed acyclic graph. JSON graphs satisfy
        /// this by index ordering; graphs built in code are checked explicitly.
        /// </summary>
        private static void CheckAcyclic(Graph graph)
        {
            var indexOf = new Dictionary<Node, int>(graph.nodes.Count);

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                indexOf[graph.nodes[i]] = i;
            }

            // Edges point from producer to consumer: value source -> reader, flow source -> target.
            var edges = new List<int>[graph.nodes.Count];

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                edges[i] = new List<int>();
            }

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                var node = graph.nodes[i];

                foreach (var v in node.values)
                {
                    if (v.node == null)
                        continue;

                    if (!indexOf.TryGetValue(v.node, out var source))
                    {
                        graph.Reject($"nodes[{i}] ({node.type}): value \"{v.id}\" refers to a node outside the graph.");
                        continue;
                    }

                    edges[source].Add(i);
                }

                foreach (var f in node.flows)
                {
                    if (f.toNode == null || !indexOf.TryGetValue(f.toNode, out var target))
                    {
                        graph.Reject($"nodes[{i}] ({node.type}): flow \"{f.fromSocket}\" targets a node outside the graph.");
                        continue;
                    }

                    edges[i].Add(target);
                }
            }

            // Iterative three-color DFS.
            var state = new byte[graph.nodes.Count];
            var stack = new Stack<(int node, int edge)>();

            for (int start = 0; start < graph.nodes.Count; start++)
            {
                if (state[start] != 0)
                    continue;

                stack.Push((start, 0));
                state[start] = 1;

                while (stack.Count > 0)
                {
                    var (n, e) = stack.Pop();

                    if (e < edges[n].Count)
                    {
                        stack.Push((n, e + 1));
                        var next = edges[n][e];

                        if (state[next] == 1)
                        {
                            graph.Reject($"nodes[{next}] ({graph.nodes[next].type}) is part of a cycle of value references and flows.");
                            return;
                        }

                        if (state[next] == 0)
                        {
                            state[next] = 1;
                            stack.Push((next, 0));
                        }
                    }
                    else
                    {
                        state[n] = 2;
                    }
                }
            }
        }

        private static void ValidateNode(Graph graph, Node node, int index)
        {
            var context = $"nodes[{index}] ({node.type})";

            // Unsupported declarations are demoted to no-ops and are not validated further.
            if (node.declaration != null && !SpecOperations.IsSupported(node.declaration))
                return;

            if (SpecOperations.StaticInputValueSockets.TryGetValue(node.type, out var required))
            {
                foreach (var id in required)
                {
                    if (!node.TryGetValueById(id, out _))
                        graph.Reject($"{context}: missing input value socket \"{id}\".");
                }
            }

            switch (node.type)
            {
                case "variable/get":
                    RequireVariable(graph, node, ConstStrings.VARIABLE, context, out _);
                    break;

                case "variable/set":
                    ValidateVariableSet(graph, node, context);
                    break;

                case "variable/interpolate":
                    ValidateVariableInterpolate(graph, node, context);
                    break;

                case "pointer/get":
                    ValidatePointer(graph, node, context, Array.Empty<string>(), allowBoolOrInt: true);
                    break;

                case "pointer/set":
                    ValidatePointer(graph, node, context, new[] { ConstStrings.VALUE }, allowBoolOrInt: true);
                    break;

                case "pointer/interpolate":
                    ValidatePointer(graph, node, context, new[] { ConstStrings.VALUE, ConstStrings.DURATION, ConstStrings.P1, ConstStrings.P2 }, allowBoolOrInt: false);
                    break;

                case "event/receive":
                    RequireEvent(graph, node, context, out _);
                    break;

                case "event/send":
                    if (RequireEvent(graph, node, context, out var customEvent))
                    {
                        foreach (var value in customEvent.values)
                        {
                            RequireValue(graph, node, value.id, value.property.GetSystemType(), context);
                        }
                    }
                    break;

                case "math/switch":
                    RequireValue(graph, node, ConstStrings.SELECTION, typeof(int), context);

                    // Case sockets must have the same type as "default" when it is known statically.
                    Type defaultType = null;
                    if (node.TryGetValueById(ConstStrings.DEFAULT, out var defaultValue))
                        defaultType = defaultValue.node == null ? defaultValue.property?.GetSystemType() : defaultValue.declaredType;

                    foreach (var c in GetSwitchCases(node))
                    {
                        RequireValue(graph, node, c.ToString(System.Globalization.CultureInfo.InvariantCulture), defaultType, context);
                    }
                    break;

                case "debug/log":
                    if (node.TryGetConfiguration(ConstStrings.MESSAGE, out string message) && DebugLog.TryParseMessageTemplate(message, out var parameters))
                    {
                        foreach (var p in parameters)
                        {
                            RequireValue(graph, node, p.id, null, context);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Cases of flow/switch and math/switch. An absent or invalid "cases" array means the default configuration (no cases).
        /// Duplicates are ignored.
        /// </summary>
        public static List<int> GetSwitchCases(Node node)
        {
            var result = new List<int>();

            if (!node.TryGetConfiguration(ConstStrings.CASES, out int[] cases) || cases == null)
                return result;

            foreach (var c in cases)
            {
                if (!result.Contains(c))
                    result.Add(c);
            }

            return result;
        }

        private static bool RequireVariable(Graph graph, Node node, string configId, string context, out Variable variable)
        {
            variable = null;

            if (!node.TryGetConfiguration(configId, out int variableIndex))
            {
                graph.Reject($"{context}: configuration \"{configId}\" is required.");
                return false;
            }

            if (variableIndex < 0 || variableIndex >= graph.variables.Count)
            {
                graph.Reject($"{context}: variable index {variableIndex} is out of range.");
                return false;
            }

            variable = graph.variables[variableIndex];
            return true;
        }

        private static void ValidateVariableSet(Graph graph, Node node, string context)
        {
            if (!node.TryGetConfiguration(ConstStrings.VARIABLES, out int[] indices) || indices == null || indices.Length == 0)
            {
                graph.Reject($"{context}: configuration \"variables\" must be a non-empty array.");
                return;
            }

            foreach (var variableIndex in indices)
            {
                if (variableIndex < 0 || variableIndex >= graph.variables.Count)
                {
                    graph.Reject($"{context}: variable index {variableIndex} is out of range.");
                    continue;
                }

                RequireValue(graph, node, variableIndex.ToString(System.Globalization.CultureInfo.InvariantCulture), graph.variables[variableIndex].property.GetSystemType(), context);
            }
        }

        private static void ValidateVariableInterpolate(Graph graph, Node node, string context)
        {
            if (!RequireVariable(graph, node, ConstStrings.VARIABLE, context, out var variable))
                return;

            var type = variable.property.GetSystemType();

            if (type == typeof(int) || type == typeof(bool) || type == typeof(Ref))
                graph.Reject($"{context}: variables of type {Helpers.GetSignatureBySystemType(type)} cannot be interpolated.");

            if (!node.TryGetConfiguration(ConstStrings.USE_SLERP, out bool useSlerp))
                graph.Reject($"{context}: configuration \"useSlerp\" must be a boolean.");
            else if (useSlerp && type != typeof(float4))
                graph.Reject($"{context}: \"useSlerp\" may only be true for float4 variables.");

            RequireValue(graph, node, ConstStrings.VALUE, type, context);
            RequireValue(graph, node, ConstStrings.DURATION, typeof(float), context);
            RequireValue(graph, node, ConstStrings.P1, typeof(float2), context);
            RequireValue(graph, node, ConstStrings.P2, typeof(float2), context);
        }

        private static void ValidatePointer(Graph graph, Node node, string context, string[] reservedIds, bool allowBoolOrInt)
        {
            if (!node.TryGetConfiguration(ConstStrings.POINTER, out string pointer))
            {
                graph.Reject($"{context}: configuration \"pointer\" must be a string.");
                return;
            }

            if (!PointerTemplate.TryParse(pointer, out var template, out var error))
            {
                graph.Reject($"{context}: pointer template \"{pointer}\" is invalid: {error}.");
                return;
            }

            if (!node.TryGetConfiguration(ConstStrings.TYPE, out int typeIndex) || typeIndex < 0 || typeIndex >= graph.types.Count)
            {
                graph.Reject($"{context}: configuration \"type\" must index the types array.");
                return;
            }

            var type = Helpers.GetSystemType(graph.types[typeIndex]);

            if (!allowBoolOrInt && (type == typeof(bool) || type == typeof(int)))
                graph.Reject($"{context}: type {graph.types[typeIndex].signature} cannot be interpolated.");

            foreach (var p in template.parameters)
            {
                if (Array.IndexOf(reservedIds, p.id) >= 0)
                {
                    graph.Reject($"{context}: pointer template parameter \"{p.id}\" collides with an operation input socket.");
                    continue;
                }

                RequireValue(graph, node, p.id, p.isReference ? typeof(Ref) : typeof(int), context);
            }

            if (Array.IndexOf(reservedIds, ConstStrings.VALUE) >= 0)
                RequireValue(graph, node, ConstStrings.VALUE, type, context);
        }

        private static bool RequireEvent(Graph graph, Node node, string context, out Customevent customEvent)
        {
            customEvent = null;

            if (!node.TryGetConfiguration(ConstStrings.EVENT, out int eventIndex) || eventIndex < 0 || eventIndex >= graph.customEvents.Count)
            {
                graph.Reject($"{context}: configuration \"event\" must index the events array.");
                return false;
            }

            customEvent = graph.customEvents[eventIndex];
            return true;
        }

        /// <summary>
        /// Requires an input value socket. When <paramref name="type"/> is given, an inline value or an
        /// explicit "type" on a node reference must match it.
        /// </summary>
        private static void RequireValue(Graph graph, Node node, string id, Type type, string context)
        {
            if (!node.TryGetValueById(id, out var value))
            {
                graph.Reject($"{context}: missing input value socket \"{id}\".");
                return;
            }

            if (type == null)
                return;

            var actual = value.node == null ? value.property?.GetSystemType() : value.declaredType;

            if (actual != null && actual != type)
                graph.Reject($"{context}: input value socket \"{id}\" must be {Helpers.GetSignatureBySystemType(type)} but is {Helpers.GetSignatureBySystemType(actual)}.");
        }
    }
}
