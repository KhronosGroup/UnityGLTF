using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public static class TypesSerializer
    {
        public static Dictionary<Type, int> GetSystemTypeByIndexDictionary(Graph value)
        {
            return GetSystemTypeByIndexDictionary(value.types);
        }

        public static Dictionary<Type, int> GetSystemTypeByIndexDictionary(List<InteractivityType> types)
        {
            var typeIndexByType = new Dictionary<Type, int>();

            for (int i = 0; i < types.Count; i++)
            {
                var systemType = Helpers.GetSystemType(types[i]);

                if (!typeIndexByType.ContainsKey(systemType))
                    typeIndexByType.Add(systemType, i);
            }

            return typeIndexByType;
        }

        /// <summary>
        /// Returns the graph's types with any missing required types appended. Existing indices are preserved
        /// because configurations such as pointer/get "type" refer to them.
        /// </summary>
        public static List<InteractivityType> WithRequiredTypes(List<InteractivityType> types, IEnumerable<Type> required)
        {
            var result = new List<InteractivityType>(types);
            var present = GetSystemTypeByIndexDictionary(types);

            foreach (var type in required)
            {
                if (present.ContainsKey(type))
                    continue;

                present.Add(type, result.Count);
                result.Add(new InteractivityType() { signature = Helpers.GetSignatureBySystemType(type) });
            }

            return result;
        }

        public static void WriteJson(JsonWriter writer, List<InteractivityType> types)
        {
            if (types.Count == 0)
                return;

            writer.WritePropertyName(ConstStrings.TYPES);
            writer.WriteStartArray();

            for (int i = 0; i < types.Count; i++)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(ConstStrings.SIGNATURE);
                writer.WriteValue(types[i].signature);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}
