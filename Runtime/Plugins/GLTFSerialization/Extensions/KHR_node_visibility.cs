using System;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{

	[Serializable]
	public class KHR_node_visbility : IExtension
	{
		public bool visible = true;

		public JProperty Serialize()
		{
			var obj = new JObject();
			JProperty jProperty = new JProperty(KHR_node_visbility_Factory.EXTENSION_NAME, obj);
			return jProperty;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_node_visbility() { visible = visible};
		}
	}

	public class KHR_node_visbility_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_node_visibility";

		public KHR_node_visbility_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new KHR_node_visbility();
				
				JToken visible = extensionToken.Value[nameof(KHR_node_visbility.visible)];
				if (visible != null)
					extension.visible = visible.Value<bool>();
				else
					extension.visible = true;
				return extension;
			}

			return null;
		}
	}
}

