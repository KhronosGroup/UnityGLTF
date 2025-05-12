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
        public string id { get; set; }
        public List<EventValue> values { get; set; } = new();

        public void AddValue<T>(string id, T value)
        {
            values.Add(new EventValue(id, new Property<T>(value)));
        }
    }
}