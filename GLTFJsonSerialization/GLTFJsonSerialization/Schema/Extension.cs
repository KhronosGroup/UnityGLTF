using Newtonsoft.Json;

namespace GLTFJsonSerialization
{
    public interface Extension
    {
        void Serialize(JsonWriter writer);
    }

    public abstract class ExtensionFactory
    {
        public string ExtensionName;
        public abstract Extension Deserialize(GLTFRoot root, JsonReader reader);
    }
}
