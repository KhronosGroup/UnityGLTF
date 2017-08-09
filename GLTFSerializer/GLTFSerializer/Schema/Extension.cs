using Newtonsoft.Json;

namespace GLTFSerializer
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
