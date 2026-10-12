using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public static class EventsSerializer
    {
        public static void WriteJson(JsonWriter writer, List<Customevent> events, Dictionary<Type, int> typeIndexByType)
        {
            if (events.Count == 0)
                return;

            writer.WritePropertyName(ConstStrings.EVENTS);
            writer.WriteStartArray();

            for (int i = 0; i < events.Count; i++)
            {
                WriteEvent(writer, events[i], typeIndexByType, $"events[{i}]");
            }

            writer.WriteEndArray();
        }

        private static void WriteEvent(JsonWriter writer, Customevent customevent, Dictionary<Type, int> typeIndexByType, string context)
        {
            writer.WriteStartObject();

            // Events without an id are internal to the graph.
            if (!string.IsNullOrEmpty(customevent.id))
            {
                writer.WritePropertyName(ConstStrings.ID);
                writer.WriteValue(customevent.id);
            }

            if (!string.IsNullOrEmpty(customevent.name))
            {
                writer.WritePropertyName(ConstStrings.NAME);
                writer.WriteValue(customevent.name);
            }

            if (customevent.values != null && customevent.values.Count > 0)
            {
                writer.WritePropertyName(ConstStrings.VALUES);
                writer.WriteStartObject();

                foreach (var value in customevent.values)
                {
                    writer.WritePropertyName(value.id);
                    writer.WriteStartObject();
                    LiteralSerializer.WriteTypedValueOrDefault(writer, value.property, typeIndexByType, $"{context}.values.{value.id}");
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }
    }
}
