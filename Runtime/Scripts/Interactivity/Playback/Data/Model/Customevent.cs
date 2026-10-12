using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventValue
    {
        public string id { get; set; }
        public IProperty property { get; set; }

        public EventValue(string id, IProperty property)
        {
            this.id = id;
            this.property = property;
        }
    }

    public class Customevent
    {
        /// <summary>External event id; null or empty when the event is internal to the graph.</summary>
        public string id { get; set; }
        /// <summary>Optional user-facing name, not used for execution.</summary>
        public string name { get; set; }
        public List<EventValue> values { get; set; } = new();

        public void AddValue<T>(string id, T value)
        {
            values.Add(new EventValue(id, new Property<T>(value)));
        }
    }
}