using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public static class VariablesDeserializer
    {
        public static List<Variable> GetVariables(JObject jObj, List<Type> types)
        {
            var variables = new List<Variable>();
            var index = 0;

            foreach (var v in GraphJson.OptionalArray(jObj, ConstStrings.VARIABLES, "graph"))
            {
                variables.Add(CreateVariable(v, types, $"variables[{index}]"));
                index++;
            }

            return variables;
        }

        private static Variable CreateVariable(JToken token, List<Type> types, string context)
        {
            GraphJson.AsObject(token, context);

            // "name" is the spec property. "id" is accepted for graphs written by older exporters.
            var name = GraphJson.OptionalString(token, ConstStrings.NAME, context);

            if (name == null && token[ConstStrings.ID]?.Type == JTokenType.String)
                name = token[ConstStrings.ID].Value<string>();

            var typeIndex = GraphJson.RequiredIndex(token, ConstStrings.TYPE, types.Count, context);
            var valueArray = GraphJson.OptionalValueArray(token, context);
            var type = types[typeIndex];

            return new Variable()
            {
                id = name ?? string.Empty,
                property = GraphJson.ParseValue(type, valueArray, context),
                initialValue = GraphJson.ParseValue(type, valueArray, context),
            };
        }
    }
}
