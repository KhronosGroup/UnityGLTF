using System;
using Newtonsoft.Json.Linq;
using GLTF.Math;
using Newtonsoft.Json;
using GLTF.Extensions;

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
