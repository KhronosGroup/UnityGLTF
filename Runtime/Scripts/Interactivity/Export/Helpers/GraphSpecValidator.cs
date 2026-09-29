using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace UnityGLTF.Interactivity.Export
{
    /// <summary>
    /// Checks the serialized KHR_interactivity extension object against the structural rules of the specification
    /// (JSON schema, "Validation (Informative)" section and the JSON Syntax chapter). Operation specific rules
    /// (socket names, supported types per operation) are not covered.
    /// Works on JSON only, so it also catches problems introduced by the serialization itself.
    /// </summary>
    public static class GraphSpecValidator
    {
        public enum Severity
        {
            /// <summary> Structural assert / JSON schema violation: the whole extension is rejected. </summary>
            RejectExtension,
            /// <summary> The graph is invalid and rejected. </summary>
            RejectGraph,
        }

        public struct Issue
        {
            public Severity Severity;
            /// <summary> Short, stable rule description used for grouping. </summary>
            public string Rule;
            public string Location;

            public override string ToString() => $"{Location}: {Rule}";
        }

        private static readonly HashSet<string> Signatures = new HashSet<string>
        {
            "bool", "custom", "float", "float2", "float3", "float4", "float2x2", "float3x3", "float4x4", "int", "ref"
        };

        private static readonly Dictionary<string, int> ValueLength = new Dictionary<string, int>
        {
            { "bool", 1 }, { "int", 1 }, { "float", 1 }, { "ref", 1 }, { "float2", 2 }, { "float3", 3 }, { "float4", 4 },
            { "float2x2", 4 }, { "float3x3", 9 }, { "float4x4", 16 },
        };

        private static readonly Regex InvalidPointerEscape = new Regex("~(?![01])");

        public static List<Issue> Validate(JObject extension)
        {
            var issues = new List<Issue>();
            void Assert(bool condition, string rule, string location)
            {
                if (!condition) issues.Add(new Issue { Severity = Severity.RejectExtension, Rule = rule, Location = location });
            }
            void Reject(bool condition, string rule, string location)
            {
                if (!condition) issues.Add(new Issue { Severity = Severity.RejectGraph, Rule = rule, Location = location });
            }

            var graphs = extension["graphs"] as JArray;
            Assert(graphs != null && graphs.Count > 0, "graphs must be a non-empty array", "extension");
            if (graphs == null)
                return issues;

            if (extension.TryGetValue("graph", out var defaultGraph))
            {
                Assert(IsIndex(defaultGraph), "graph must be a non-negative integer", "extension");
                Assert(!IsIndex(defaultGraph) || (int)defaultGraph < graphs.Count, "graph must be less than the graphs length", "extension");
            }

            for (int g = 0; g < graphs.Count; g++)
            {
                if (graphs[g] is JObject graph)
                    ValidateGraph(graph, $"graphs[{g}]", Assert, Reject);
                else
                    Assert(false, "graph must be an object", $"graphs[{g}]");
            }

            return issues;
        }

        private delegate void Check(bool condition, string rule, string location);

        private static void ValidateGraph(JObject graph, string at, Check assert, Check reject)
        {
            var types = new List<string>();
            var typeCount = 0;

            var typesArray = NonEmptyArray(graph, "types", at, assert);
            if (typesArray != null)
            {
                typeCount = typesArray.Count;
                for (int i = 0; i < typesArray.Count; i++)
                {
                    var signature = typesArray[i] is JObject t ? t["signature"] : null;
                    assert(signature != null && signature.Type == JTokenType.String, "type signature must be a string", $"{at}.types[{i}]");
                    var s = signature != null && signature.Type == JTokenType.String ? (string)signature : null;
                    types.Add(s);
                    reject(s == null || Signatures.Contains(s), $"unknown type signature \"{s}\"", $"{at}.types[{i}]");
                }
                foreach (var duplicate in types.Where(s => s != null && s != "custom").GroupBy(s => s).Where(d => d.Count() > 1))
                    reject(false, $"type signature \"{duplicate.Key}\" defined more than once", $"{at}.types");
            }

            string TypeAt(JToken index) => IsIndex(index) && (int)index < types.Count ? types[(int)index] : null;

            void CheckTypeIndex(JToken index, string location)
            {
                assert(IsIndex(index), "type must be a non-negative integer", location);
                reject(!IsIndex(index) || (int)index < typeCount, "type index out of range", location);
            }

            void CheckInlineValue(JToken value, JToken type, string location)
            {
                var array = value as JArray;
                assert(array != null && array.Count > 0, "value must be a non-empty array", location);
                var signature = TypeAt(type);
                if (array == null || array.Count == 0 || signature == null || !ValueLength.TryGetValue(signature, out var length))
                    return;

                reject(array.Count == length, $"{signature} value must have {length} element(s), has {array.Count}", location);
                if (signature == "bool")
                    reject(array[0].Type == JTokenType.Boolean, "bool value must be a JSON boolean", location);
                else if (signature == "int")
                    reject(IsInt32(array[0]), "int value must be a 32-bit integer", location);
                else if (signature == "ref")
                    reject(array[0].Type == JTokenType.String && IsJsonPointer((string)array[0]), "ref value must be a JSON pointer string", location);
                else
                    reject(array.All(IsNumber), "float value elements must be numbers", location);
            }

            var variables = NonEmptyArray(graph, "variables", at, assert);
            if (variables != null)
            {
                assert(graph["types"] != null, "variables require the types array", at);
                for (int i = 0; i < variables.Count; i++)
                {
                    var location = $"{at}.variables[{i}]";
                    if (!(variables[i] is JObject variable)) { assert(false, "variable must be an object", location); continue; }
                    CheckTypeIndex(variable["type"], location);
                    if (variable.TryGetValue("value", out var value))
                        CheckInlineValue(value, variable["type"], location);
                    if (variable.TryGetValue("name", out var name))
                        assert(name.Type == JTokenType.String, "variable name must be a string", location);
                }
            }

            var events = NonEmptyArray(graph, "events", at, assert);
            if (events != null)
            {
                var ids = new List<string>();
                for (int i = 0; i < events.Count; i++)
                {
                    var location = $"{at}.events[{i}]";
                    if (!(events[i] is JObject ev)) { assert(false, "event must be an object", location); continue; }
                    if (ev.TryGetValue("id", out var id))
                    {
                        assert(id.Type == JTokenType.String, "event id must be a string", location);
                        if (id.Type == JTokenType.String) ids.Add((string)id);
                    }
                    if (ev.TryGetValue("values", out var valuesToken))
                    {
                        var values = valuesToken as JObject;
                        assert(values != null && values.Count > 0, "event values must be a non-empty object", location);
                        foreach (var property in values?.Properties() ?? Enumerable.Empty<JProperty>())
                        {
                            var valueLocation = $"{location}.values.{property.Name}";
                            assert(property.Name != "event", "event value socket must not be named \"event\"", valueLocation);
                            if (!(property.Value is JObject eventValue)) { assert(false, "event value must be an object", valueLocation); continue; }
                            CheckTypeIndex(eventValue["type"], valueLocation);
                            if (eventValue.TryGetValue("value", out var value))
                                CheckInlineValue(value, eventValue["type"], valueLocation);
                        }
                    }
                    if (ev.TryGetValue("name", out var name))
                        assert(name.Type == JTokenType.String, "event name must be a string", location);
                }
                foreach (var duplicate in ids.GroupBy(id => id).Where(d => d.Count() > 1))
                    reject(false, $"event id \"{duplicate.Key}\" defined more than once", $"{at}.events");
            }

            var declarations = NonEmptyArray(graph, "declarations", at, assert);
            var declarationOps = new List<string>();
            if (declarations != null)
            {
                var keys = new List<string>();
                for (int i = 0; i < declarations.Count; i++)
                {
                    var location = $"{at}.declarations[{i}]";
                    var declaration = declarations[i] as JObject;
                    var op = declaration?["op"];
                    assert(op != null && op.Type == JTokenType.String, "declaration op must be a string", location);
                    declarationOps.Add(op?.ToString());
                    if (declaration == null) continue;

                    if (!declaration.TryGetValue("extension", out var extension))
                    {
                        reject(declaration["inputValueSockets"] == null && declaration["outputValueSockets"] == null,
                            "declaration without extension must not define value sockets", location);
                    }
                    else
                    {
                        assert(extension.Type == JTokenType.String, "declaration extension must be a string", location);
                        foreach (var socketsProperty in new[] { "inputValueSockets", "outputValueSockets" })
                        {
                            if (!declaration.TryGetValue(socketsProperty, out var socketsToken)) continue;
                            var sockets = socketsToken as JObject;
                            assert(sockets != null && sockets.Count > 0, $"{socketsProperty} must be a non-empty object", location);
                            foreach (var socket in sockets?.Properties() ?? Enumerable.Empty<JProperty>())
                                CheckTypeIndex((socket.Value as JObject)?["type"], $"{location}.{socketsProperty}.{socket.Name}");
                        }
                    }

                    var inputs = declaration["inputValueSockets"] as JObject;
                    keys.Add($"{op}|{declaration["extension"]}|" + string.Join(",",
                        (inputs?.Properties() ?? Enumerable.Empty<JProperty>()).OrderBy(p => p.Name, System.StringComparer.Ordinal)
                        .Select(p => p.Name + ":" + (p.Value as JObject)?["type"])));
                }
                foreach (var duplicate in keys.GroupBy(k => k).Where(d => d.Count() > 1))
                    reject(false, $"equal declarations for op \"{duplicate.Key.Split('|')[0]}\"", $"{at}.declarations");
            }

            var nodes = NonEmptyArray(graph, "nodes", at, assert);
            if (nodes == null)
                return;
            assert(graph["declarations"] != null, "nodes require the declarations array", at);

            for (int i = 0; i < nodes.Count; i++)
            {
                if (!(nodes[i] is JObject node)) { assert(false, "node must be an object", $"{at}.nodes[{i}]"); continue; }
                var declaration = node["declaration"];
                var op = IsIndex(declaration) && (int)declaration < declarationOps.Count ? declarationOps[(int)declaration] : null;
                var location = $"{at}.nodes[{i}]" + (op != null ? $" ({op})" : "");

                assert(IsIndex(declaration), "node declaration must be a non-negative integer", location);
                reject(!IsIndex(declaration) || (int)declaration < declarationOps.Count, "node declaration index out of range", location);

                if (node.TryGetValue("configuration", out var configurationToken))
                {
                    var configuration = configurationToken as JObject;
                    assert(configuration != null && configuration.Count > 0, "configuration must be a non-empty object", location);
                    foreach (var property in configuration?.Properties() ?? Enumerable.Empty<JProperty>())
                    {
                        var value = (property.Value as JObject)?["value"] as JArray;
                        assert(value != null && value.Count > 0, "configuration property must be { \"value\": [ ... ] } with a non-empty array",
                            $"{location}.configuration.{property.Name}");
                    }
                }

                if (node.TryGetValue("values", out var valuesToken))
                {
                    var values = valuesToken as JObject;
                    assert(values != null && values.Count > 0, "values must be a non-empty object", location);
                    foreach (var property in values?.Properties() ?? Enumerable.Empty<JProperty>())
                    {
                        var socketLocation = $"{location}.values.{property.Name}";
                        if (!(property.Value is JObject socket)) { assert(false, "input value socket must be an object", socketLocation); continue; }
                        if (socket.TryGetValue("node", out var sourceNode))
                        {
                            assert(socket["value"] == null, "input value socket must not define both node and value", socketLocation);
                            assert(IsIndex(sourceNode), "input value socket node must be a non-negative integer", socketLocation);
                            reject(!IsIndex(sourceNode) || (int)sourceNode < i,
                                "input value socket must reference a node with a lower index", socketLocation);
                            if (socket.TryGetValue("socket", out var socketId))
                                assert(socketId.Type == JTokenType.String, "input value socket id must be a string", socketLocation);
                            if (socket.TryGetValue("type", out var type))
                                CheckTypeIndex(type, socketLocation);
                        }
                        else
                        {
                            CheckTypeIndex(socket["type"], socketLocation);
                            if (socket.TryGetValue("value", out var value))
                                CheckInlineValue(value, socket["type"], socketLocation);
                        }
                    }
                }

                if (op != null)
                    ValidateOperationConfiguration(op, node, variables, events, types, location, reject);

                if (node.TryGetValue("flows", out var flowsToken))
                {
                    var flows = flowsToken as JObject;
                    assert(flows != null && flows.Count > 0, "flows must be a non-empty object", location);
                    foreach (var property in flows?.Properties() ?? Enumerable.Empty<JProperty>())
                    {
                        var flowLocation = $"{location}.flows.{property.Name}";
                        var flow = property.Value as JObject;
                        var target = flow?["node"];
                        assert(IsIndex(target), "output flow node must be a non-negative integer", flowLocation);
                        reject(!IsIndex(target) || ((int)target > i && (int)target < nodes.Count),
                            "output flow must point to a node with a higher index", flowLocation);
                        if (flow != null && flow.TryGetValue("socket", out var socketId))
                            assert(socketId.Type == JTokenType.String, "output flow socket must be a string", flowLocation);
                    }
                }
            }
        }

        /// <summary>
        /// Rules of operations without a default configuration (variable/*, pointer/*, event/receive, event/send), whose
        /// configuration also defines input value sockets. Only the presence of these sockets is checked, not their types.
        /// </summary>
        private static void ValidateOperationConfiguration(string op, JObject node, JArray variables, JArray events, List<string> types,
            string location, Check reject)
        {
            var configuration = node["configuration"] as JObject;
            var values = node["values"] as JObject;
            JArray Config(string id) => (configuration?[id] as JObject)?["value"] as JArray;

            bool IsSingleIndex(JArray value, int count) => value != null && value.Count == 1 && IsIndex(value[0]) && (int)value[0] < count;
            string VariableType(JToken index) => (variables?[(int)index] as JObject)?["type"] is JToken t && IsIndex(t) && (int)t < types.Count
                ? types[(int)t] : null;

            void RequireInputs(IEnumerable<string> ids)
            {
                foreach (var id in ids)
                    reject(values != null && values[id] != null, $"input value socket \"{id}\" defined by the configuration is missing", location);
            }

            var variableCount = variables?.Count ?? 0;
            switch (op)
            {
                case "variable/get":
                    reject(IsSingleIndex(Config("variable"), variableCount), "variable/get: variable must be a valid variable index", location);
                    break;

                case "variable/set":
                {
                    var indices = Config("variables");
                    var valid = indices != null && indices.Count > 0 && indices.All(v => IsIndex(v) && (int)v < variableCount);
                    reject(valid, "variable/set: variables must be a non-empty array of valid variable indices", location);
                    if (valid)
                        RequireInputs(indices.Select(v => ((int)v).ToString()));
                    break;
                }

                case "variable/interpolate":
                {
                    var index = Config("variable");
                    if (!IsSingleIndex(index, variableCount))
                    {
                        reject(false, "variable/interpolate: variable must be a valid variable index", location);
                        break;
                    }
                    var type = VariableType(index[0]);
                    reject(type != "int" && type != "bool", "variable/interpolate: int and bool variables cannot be interpolated", location);
                    var useSlerp = Config("useSlerp");
                    var slerpValid = useSlerp != null && useSlerp.Count == 1 && useSlerp[0].Type == JTokenType.Boolean;
                    reject(slerpValid, "variable/interpolate: useSlerp must be a boolean", location);
                    reject(!slerpValid || !(bool)useSlerp[0] || type == "float4", "variable/interpolate: useSlerp requires a float4 variable", location);
                    RequireInputs(new[] { "value", "duration", "p1", "p2" });
                    break;
                }

                case "pointer/get":
                case "pointer/set":
                case "pointer/interpolate":
                {
                    var pointer = Config("pointer");
                    var pointerValid = pointer != null && pointer.Count == 1 && pointer[0].Type == JTokenType.String;
                    var parameters = pointerValid ? ParsePointerTemplate((string)pointer[0]) : null;
                    reject(parameters != null, $"{op}: pointer must be a valid JSON pointer template", location);

                    var type = Config("type");
                    var typeValid = IsSingleIndex(type, types.Count);
                    reject(typeValid, $"{op}: type must be a valid type index", location);
                    if (parameters == null)
                        break;

                    var reserved = op == "pointer/set" ? new[] { "value" }
                        : op == "pointer/interpolate" ? new[] { "value", "duration", "p1", "p2" }
                        : new string[0];
                    foreach (var name in reserved.Where(parameters.Contains))
                        reject(false, $"{op}: pointer template must not use the parameter \"{name}\"", location);
                    if (op == "pointer/interpolate" && typeValid)
                        reject(types[(int)type[0]] != "int" && types[(int)type[0]] != "bool", "pointer/interpolate: int and bool cannot be interpolated", location);

                    RequireInputs(parameters.Concat(reserved));
                    break;
                }

                case "event/receive":
                case "event/send":
                {
                    var index = Config("event");
                    var valid = IsSingleIndex(index, events?.Count ?? 0);
                    reject(valid, $"{op}: event must be a valid event index", location);
                    if (valid && op == "event/send" && events[(int)index[0]]?["values"] is JObject eventValues)
                        RequireInputs(eventValues.Properties().Select(p => p.Name));
                    break;
                }
            }
        }

        /// <summary>
        /// JSON Pointer Template parsing as defined by the specification.
        /// Returns the template parameter ids, or null if the template is invalid.
        /// </summary>
        private static List<string> ParsePointerTemplate(string template)
        {
            if (!IsJsonPointer(template))
                return null;

            var parameters = new List<string>();
            foreach (var segment in template.Split('/').Skip(1))
            {
                if (segment == "[" || segment == "{")
                    return null;

                var open = segment.Length > 0 ? segment[0] : '\0';
                var isParameter = (open == '[' || open == '{') && !(segment.Length > 1 && segment[1] == open);
                if (isParameter)
                {
                    var close = open == '[' ? ']' : '}';
                    var inner = segment.Length >= 2 ? segment.Substring(1, segment.Length - 2) : "";
                    if (segment.Length <= 2 || segment[segment.Length - 1] != close || inner.IndexOfAny(new[] { '[', ']', '{', '}' }) >= 0)
                        return null;
                    var id = inner.Replace("~1", "/").Replace("~0", "~");
                    if (parameters.Contains(id))
                        return null;
                    parameters.Add(id);
                }
                else
                {
                    // Literal segment: brackets must be doubled.
                    foreach (Match run in Regex.Matches(segment, @"\[+|\]+|\{+|\}+"))
                        if (run.Length % 2 != 0)
                            return null;
                }
            }
            return parameters;
        }

        /// <summary> Returns the array if present; empty arrays must be omitted. </summary>
        private static JArray NonEmptyArray(JObject graph, string property, string at, Check assert)
        {
            if (!graph.TryGetValue(property, out var token))
                return null;
            var array = token as JArray;
            assert(array != null && array.Count > 0, $"{property} must be a non-empty array (empty arrays must be omitted)", at);
            return array;
        }

        private static bool IsNumber(JToken token) => token != null && (token.Type == JTokenType.Integer || token.Type == JTokenType.Float);

        private static bool IsIntegral(JToken token, double min, double max)
        {
            if (!IsNumber(token)) return false;
            var d = (double)token;
            return d == System.Math.Floor(d) && d >= min && d <= max;
        }

        private static bool IsIndex(JToken token) => IsIntegral(token, 0, int.MaxValue);
        private static bool IsInt32(JToken token) => IsIntegral(token, int.MinValue, int.MaxValue);

        private static bool IsJsonPointer(string pointer)
            => pointer.Length == 0 || (pointer[0] == '/' && !InvalidPointerEscape.IsMatch(pointer));
    }
}
