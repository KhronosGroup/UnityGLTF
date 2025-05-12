using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public static class TypesDeserializer
    {
        public static List<System.Type> GetSystemTypes(List<InteractivityType> types)
        {
            var systemTypes = new List<System.Type>(types.Count);

            for (int i = 0; i < types.Count; i++)
            {
                systemTypes.Add(Helpers.GetSystemType(types[i]));
            }

            return systemTypes;
        }

        public static List<InteractivityType> GetTypes(JObject jObj)
        {
            return jObj["types"].ToObject<List<InteractivityType>>();
        }
    }
}