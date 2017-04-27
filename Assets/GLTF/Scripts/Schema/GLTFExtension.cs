using Newtonsoft.Json;

namespace GLTF
{
    public interface GLTFExtension
    {
    }

    public abstract class GLTFExtensionFactory
    {
        public string ExtensionName;
        public abstract GLTFExtension Deserialize(GLTFRoot root, JsonTextReader reader);
    }
}
