using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public static class Extensions
    {
        public static void AddValue<T>(this Dictionary<string, Value> inputs, string id, T value)
        {
            inputs.Add(id, new Value()
            {
                id = id,
                property = new Property<T>(value)
            });
        }

        public static void AddValue<T>(this Dictionary<string, IProperty> ouputs, string id, T value)
        {
            ouputs.Add(id, new Property<T>(value));
        }
    }
}