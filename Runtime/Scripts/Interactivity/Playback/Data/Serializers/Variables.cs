using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public static class VariablesSerializer
    {
        public static void WriteJson(JsonWriter writer, List<Variable> variables, Dictionary<Type, int> typeIndexByType)
        {
            if (variables.Count == 0)
                return;

            writer.WritePropertyName(ConstStrings.VARIABLES);
            writer.WriteStartArray();

            for (int i = 0; i < variables.Count; i++)
            {
                writer.WriteStartObject();

                if (!string.IsNullOrEmpty(variables[i].id))
                {
                    writer.WritePropertyName(ConstStrings.NAME);
                    writer.WriteValue(variables[i].id);
                }

                LiteralSerializer.WriteTypedValueOrDefault(writer, variables[i].initialValue, typeIndexByType, $"variables[{i}]");

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}
