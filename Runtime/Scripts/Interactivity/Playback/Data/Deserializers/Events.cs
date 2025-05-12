using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace UnityGLTF.Interactivity.Playback
{
    public static class EventsDeserializer
    {
        public static List<Customevent> GetEvents(JObject jObj, List<System.Type> systemTypes)
        {
            var jEvents = jObj[ConstStrings.EVENTS].Children();
            var events = new List<Customevent>(jEvents.Count());

            foreach (var v in jEvents)
            {
                events.Add(new Customevent()
                {
                    id = v[ConstStrings.ID].Value<string>(),
                    values = GetEventValues(v[ConstStrings.VALUES] as JObject, systemTypes)
                });
            }

            return events;
        }

        private static List<EventValue> GetEventValues(JObject jValues, List<System.Type> systemTypes)
        {
            var valueCount = jValues.Count;
            var values = new List<EventValue>(valueCount);

            foreach (var kvp in jValues)
            {
                var typeIndex = kvp.Value[ConstStrings.TYPE].Value<int>();
                values.Add(new EventValue(kvp.Key, Helpers.GetDefaultProperty(typeIndex, systemTypes)));
            }

            return values;
        }
    }
}