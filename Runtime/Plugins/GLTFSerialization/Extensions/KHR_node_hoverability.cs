using System;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{

    [Serializable]
    public class KHR_node_hoverability : IExtension
    {
        public bool hoverable = true;

        public JProperty Serialize()
        {
            var obj = new JObject();
            JProperty jProperty = new JProperty(KHR_node_hoverability_Factory.EXTENSION_NAME, obj);
            obj.Add(new JProperty(nameof(hoverable), hoverable));

            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_node_hoverability() { hoverable = hoverable};
        }
    }

    public class KHR_node_hoverability_Factory : ExtensionFactory
    {
        public const string EXTENSION_NAME = "KHR_node_hoverability";

        public KHR_node_hoverability_Factory()
        {
            ExtensionName = EXTENSION_NAME;
        }

        public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            if (extensionToken != null)
            {
                var extension = new KHR_node_hoverability();
				
                JToken hoverable = extensionToken.Value[nameof(KHR_node_hoverability.hoverable)];
                if (hoverable != null)
                    extension.hoverable = hoverable.Value<bool>();
                else
                    extension.hoverable = true;
                return extension;
            }

            return null;
        }
    }
}