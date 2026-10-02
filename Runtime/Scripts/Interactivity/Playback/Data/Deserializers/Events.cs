using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public static class EventsDeserializer
    {
        public static List<Customevent> GetEvents(JObject jObj, List<System.Type> systemTypes)
        {
            var events = new List<Customevent>();
            var externalIds = new HashSet<string>();
            var index = 0;

            foreach (var v in GraphJson.OptionalArray(jObj, ConstStrings.EVENTS, "graph"))
            {
                var context = $"events[{index}]";
                GraphJson.AsObject(v, context);

                var id = GraphJson.OptionalString(v, ConstStrings.ID, context);

                if (id != null && !externalIds.Add(id))
                    GraphJson.Reject($"{context}: event id \"{id}\" is used by more than one event.");

                events.Add(new Customevent()
                {
                    id = id,
                    name = GraphJson.OptionalString(v, ConstStrings.NAME, context),
                    values = GetEventValues(GraphJson.OptionalObject(v, ConstStrings.VALUES, context), systemTypes, context)
                });

                index++;
            }

            return events;
        }

        private static List<EventValue> GetEventValues(JObject jValues, List<System.Type> systemTypes, string context)
        {
            var values = new List<EventValue>();

            if (jValues == null)
                return values;

            foreach (var kvp in jValues)
            {
                var valueContext = $"{context}.values.{kvp.Key}";

                if (kvp.Key == ConstStrings.EVENT)
                    GraphJson.Reject($"{context}: custom events cannot define a value socket named \"event\".");

                GraphJson.AsObject(kvp.Value, valueContext);
                var typeIndex = GraphJson.RequiredIndex(kvp.Value, ConstStrings.TYPE, systemTypes.Count, valueContext);
                var initial = GraphJson.ParseValue(systemTypes[typeIndex], GraphJson.OptionalValueArray(kvp.Value, valueContext), valueContext);

                values.Add(new EventValue(kvp.Key, initial));
            }

            return values;
        }
    }
}
