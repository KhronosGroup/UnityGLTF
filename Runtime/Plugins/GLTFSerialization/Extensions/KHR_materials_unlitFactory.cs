using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
    public class KHR_MaterialsUnlitExtensionFactory : ExtensionFactory
    {
        public const string EXTENSION_NAME = "KHR_materials_unlit";

        public KHR_MaterialsUnlitExtensionFactory()
        {
            ExtensionName = EXTENSION_NAME;
        }

        public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            return new KHR_MaterialsUnlitExtension();
        }
    }
}
