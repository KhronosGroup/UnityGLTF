using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public static class VariablesSerializer
    {
        public static void WriteJson(JsonWriter writer, List<Variable> variables, Dictionary<Type, int> typeIndexByType)
        {
            writer.WritePropertyName("variables");
            writer.WriteStartArray();

            for (int i = 0; i < variables.Count; i++)
            {
                WriteVariable(writer, variables[i], typeIndexByType);
            }

            writer.WriteEndArray();
        }

        private static void WriteVariable(JsonWriter writer, Variable variable, Dictionary<Type, int> typeIndexByType)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(ConstStrings.ID);
            writer.WriteValue(variable.id);

            NodesSerializer.WriteValueLiteral(writer, variable.initialValue, typeIndexByType);

            writer.WriteEndObject();
        }
    }
}