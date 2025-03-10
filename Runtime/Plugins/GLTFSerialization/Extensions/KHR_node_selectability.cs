using System;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{

    [Serializable]
    public class KHR_node_selectability : IExtension
    {
        public bool selectable = true;

        public JProperty Serialize()
        {
            var obj = new JObject();
            JProperty jProperty = new JProperty(KHR_node_selectability_Factory.EXTENSION_NAME, obj);
            obj.Add(new JProperty(nameof(selectable), selectable));

            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_node_selectability() { selectable = selectable};
        }
    }

    public class KHR_node_selectability_Factory : ExtensionFactory
    {
        public const string EXTENSION_NAME = "KHR_node_selectability";

        public KHR_node_selectability_Factory()
        {
            ExtensionName = EXTENSION_NAME;
        }

        public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            if (extensionToken != null)
            {
                var extension = new KHR_node_selectability();
				
                JToken visible = extensionToken.Value[nameof(KHR_node_selectability.selectable)];
                if (visible != null)
                    extension.selectable = visible.Value<bool>();
                else
                    extension.selectable = true;
                return extension;
            }

            return null;
        }
    }
}