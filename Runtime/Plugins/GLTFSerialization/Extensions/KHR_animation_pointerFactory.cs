using Newtonsoft.Json.Linq;
using UnityGLTF.Extensions;

namespace GLTF.Schema
{
    public class KHR_animation_pointerExtensionFactory : ExtensionFactory
    {
        public const string EXTENSION_NAME = "KHR_animation_pointer";


        public KHR_animation_pointerExtensionFactory()
        {
            ExtensionName = EXTENSION_NAME;
        }

        public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            if (extensionToken != null)
            {
                var extensionObject = (JObject) extensionToken.Value;
                string path = (string) extensionObject["pointer"];
                return new KHR_animation_pointer
                {
                    path = path
                };
            }

            return null;
        }
    }
}