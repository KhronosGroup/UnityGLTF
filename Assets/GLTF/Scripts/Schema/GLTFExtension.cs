using Newtonsoft.Json;

namespace GLTF
{
    public interface GLTFExtension
    {
        void Serialize(JsonWriter writer);
    }

    public abstract class GLTFExtensionFactory
    {
        public string ExtensionName;
        public abstract GLTFExtension Deserialize(GLTFRoot root, JsonTextReader reader);
    }
}
