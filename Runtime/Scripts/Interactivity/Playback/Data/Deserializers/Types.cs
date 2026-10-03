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
            var types = new List<InteractivityType>();
            var seen = new HashSet<string>();
            var index = 0;

            foreach (var jType in GraphJson.OptionalArray(jObj, ConstStrings.TYPES, "graph"))
            {
                var context = $"types[{index}]";
                GraphJson.AsObject(jType, context);
                var signature = GraphJson.RequiredString(jType, ConstStrings.SIGNATURE, context);

                if (signature != "custom" && !Helpers.IsSpecTypeSignature(signature))
                    GraphJson.Reject($"{context}: unknown signature \"{signature}\".");

                if (signature != "custom" && !seen.Add(signature))
                    GraphJson.Reject($"{context}: signature \"{signature}\" is defined more than once.");

                types.Add(new InteractivityType()
                {
                    signature = signature,
                    extensions = jType[Pointers.EXTENSIONS]?.ToObject<TypeExtensions>()
                });

                index++;
            }

            return types;
        }
    }
}
