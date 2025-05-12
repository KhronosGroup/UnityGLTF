using Newtonsoft.Json;

namespace UnityGLTF.Interactivity.Playback
{
    public class GraphSerializer
    {
        private readonly JsonSerializerSettings _serializationSettings;
        private readonly JsonSerializerSettings _deserializerSettings;

        public GraphSerializer(Formatting formatting = Formatting.None)
        {
            _serializationSettings = new JsonSerializerSettings
            {
                Formatting = formatting,
                Converters =
                {
                    new GraphConverter(),
                },
            };

            _deserializerSettings = new JsonSerializerSettings
            {
                Converters =
                {
                    new GraphConverter(),
                },
            };
        }

        public string Serialize(KHR_interactivity extensionData)
        {
            return JsonConvert.SerializeObject(extensionData, _serializationSettings);
        }

        public KHR_interactivity Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<KHR_interactivity>(json, _deserializerSettings);
        }
    }
}