using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityGLTF.Interactivity.Playback
{
    public static class VariablesDeserializer
    {
        public static List<Variable> GetVariables(JObject jObj, List<Type> types)
        {
            var jVariables = jObj[ConstStrings.VARIABLES].Children();

            var variables = new List<Variable>(jVariables.Count());

            foreach (var v in jVariables)
            {
                variables.Add(CreateVariable(v, types));
            }

            return variables;
        }

        private static Variable CreateVariable(JToken token, List<Type> types)
        {

            // ID is not part of the spec but it's a nice to have.
            // Needle exporter uses id for this field.
            var id = "";
            JToken jId = token[ConstStrings.ID];

            // React app uses name for this field.
            if (jId == null)
                jId = token[ConstStrings.NAME];

            if(jId != null)
                id = jId.Value<string>();

            var typeIndex = token[ConstStrings.TYPE].Value<int>();
            var valueArray = token[ConstStrings.VALUE] as JArray;

            return new Variable()
            {
                id = id,
                property = Helpers.CreateProperty(types[typeIndex], valueArray),
                initialValue = Helpers.CreateProperty(types[typeIndex], valueArray),
            };
        }
    }
}