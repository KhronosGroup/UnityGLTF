using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity
{
    /// <summary>Value type checks of <see cref="GraphSpecValidator"/> on hand-written graphs.</summary>
    public class GraphSpecValidatorTypeTests
    {
        // types: 0 = int, 1 = float, 2 = float3
        private static JObject Extension(string declarations, string nodes, string variables = null)
        {
            var graph = $@"{{
                ""types"": [ {{ ""signature"": ""int"" }}, {{ ""signature"": ""float"" }}, {{ ""signature"": ""float3"" }} ],
                {(variables != null ? $@"""variables"": {variables}," : "")}
                ""declarations"": {declarations},
                ""nodes"": {nodes}
            }}";
            return JObject.Parse($@"{{ ""graphs"": [ {graph} ], ""graph"": 0 }}");
        }

        private static string[] TypeIssues(JObject extension) => GraphSpecValidator.Validate(extension)
            .Select(i => i.Rule)
            .Where(r => r.Contains("accept") || r.Contains("same type") || r.Contains(", but "))
            .ToArray();

        [Test]
        public void InlineIntIntoSin_IsRejected()
        {
            var issues = TypeIssues(Extension(@"[ { ""op"": ""math/sin"" } ]",
                @"[ { ""declaration"": 0, ""values"": { ""a"": { ""type"": 0, ""value"": [ 3 ] } } } ]"));
            Assert.That(issues, Has.Some.Contains("math/sin: input \"a\" does not accept int"));
        }

        [Test]
        public void InlineFloatIntoSin_IsAccepted()
        {
            var issues = TypeIssues(Extension(@"[ { ""op"": ""math/sin"" } ]",
                @"[ { ""declaration"": 0, ""values"": { ""a"": { ""type"": 1, ""value"": [ 3.0 ] } } } ]"));
            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void IntVariableIntoSin_IsRejected()
        {
            var issues = TypeIssues(Extension(@"[ { ""op"": ""variable/get"" }, { ""op"": ""math/sin"" } ]",
                @"[ { ""declaration"": 0, ""configuration"": { ""variable"": { ""value"": [ 0 ] } } },
                    { ""declaration"": 1, ""values"": { ""a"": { ""node"": 0, ""socket"": ""value"" } } } ]",
                @"[ { ""type"": 0, ""value"": [ 7 ] } ]"));
            Assert.That(issues, Has.Some.Contains("math/sin: input \"a\" does not accept int"));
        }

        [Test]
        public void IntVariableConvertedToFloatIntoSin_IsAccepted()
        {
            var issues = TypeIssues(Extension(@"[ { ""op"": ""variable/get"" }, { ""op"": ""type/intToFloat"" }, { ""op"": ""math/sin"" } ]",
                @"[ { ""declaration"": 0, ""configuration"": { ""variable"": { ""value"": [ 0 ] } } },
                    { ""declaration"": 1, ""values"": { ""a"": { ""node"": 0, ""socket"": ""value"" } } },
                    { ""declaration"": 2, ""values"": { ""a"": { ""node"": 1, ""socket"": ""value"" } } } ]",
                @"[ { ""type"": 0, ""value"": [ 7 ] } ]"));
            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void AddWithMixedInputTypes_IsRejected()
        {
            var issues = TypeIssues(Extension(@"[ { ""op"": ""math/add"" } ]",
                @"[ { ""declaration"": 0, ""values"": { ""a"": { ""type"": 0, ""value"": [ 1 ] }, ""b"": { ""type"": 1, ""value"": [ 2.0 ] } } } ]"));
            Assert.That(issues, Has.Some.Contains("must have the same type"));
        }

        [Test]
        public void VariableSetWithWrongType_IsRejected()
        {
            var issues = TypeIssues(Extension(@"[ { ""op"": ""variable/set"" } ]",
                @"[ { ""declaration"": 0, ""configuration"": { ""variables"": { ""value"": [ 0 ] } },
                      ""values"": { ""0"": { ""type"": 2, ""value"": [ 1.0, 2.0, 3.0 ] } } } ]",
                @"[ { ""type"": 1, ""value"": [ 0.0 ] } ]"));
            Assert.That(issues, Has.Some.Contains("variable/set: input \"0\" is float3, but the variable is float"));
        }

        // math/transform has one schema per overload (float2/float3/float4), the validator must pick the matching one.
        // types: 0 = float3, 1 = float4, 2 = float4x4
        private static JObject TransformExtension(int aType, string aValue)
        {
            var identity = "[ 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 ]";
            return JObject.Parse($@"{{ ""graphs"": [ {{
                ""types"": [ {{ ""signature"": ""float3"" }}, {{ ""signature"": ""float4"" }}, {{ ""signature"": ""float4x4"" }} ],
                ""variables"": [ {{ ""type"": 1, ""value"": [ 0.0, 0.0, 0.0, 0.0 ] }} ],
                ""declarations"": [ {{ ""op"": ""math/transform"" }}, {{ ""op"": ""math/dot"" }}, {{ ""op"": ""variable/set"" }} ],
                ""nodes"": [
                    {{ ""declaration"": 0, ""values"": {{ ""a"": {{ ""type"": {aType}, ""value"": {aValue} }}, ""b"": {{ ""type"": 2, ""value"": {identity} }} }} }},
                    {{ ""declaration"": 1, ""values"": {{ ""a"": {{ ""node"": 0, ""socket"": ""value"" }}, ""b"": {{ ""type"": 1, ""value"": [ 1.0, 2.0, 3.0, 4.0 ] }} }} }},
                    {{ ""declaration"": 2, ""configuration"": {{ ""variables"": {{ ""value"": [ 0 ] }} }},
                       ""values"": {{ ""0"": {{ ""node"": 0, ""socket"": ""value"" }} }} }} ]
            }} ], ""graph"": 0 }}");
        }

        [Test]
        public void TransformFloat4_IsAccepted()
        {
            var issues = TypeIssues(TransformExtension(1, "[ 1.0, 2.0, 3.0, 4.0 ]"));
            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void TransformFloat3WithFloat4x4_IsRejected()
        {
            var issues = TypeIssues(TransformExtension(0, "[ 1.0, 2.0, 3.0 ]"));
            Assert.That(issues, Has.Some.Contains("math/transform: input"));
        }
    }
}
