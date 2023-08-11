using System;
using GLTF.Extensions;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
// https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_transmission
	[Serializable]
	public class KHR_materials_transmission : IExtension
	{
		public float transmissionFactor = 0.0f;
		public TextureInfo transmissionTexture; // transmissionTexture // R channel

		public JProperty Serialize()
		{
			var jo = new JObject();
			if (transmissionFactor != 0) jo.Add(new JProperty(nameof(transmissionFactor), transmissionFactor));
			if (transmissionTexture != null)
				jo.WriteTexture(nameof(transmissionTexture), transmissionTexture);

			JProperty jProperty = new JProperty(KHR_materials_transmission_Factory.EXTENSION_NAME, jo);
			return jProperty;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_materials_transmission() { transmissionFactor = transmissionFactor, transmissionTexture = transmissionTexture };
		}
	}

	public class KHR_materials_transmission_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_materials_transmission";

		public KHR_materials_transmission_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new KHR_materials_transmission();
				extension.transmissionFactor = extensionToken.Value[nameof(KHR_materials_transmission.transmissionFactor)]?.Value<float>() ?? 0;
				extension.transmissionTexture = extensionToken.Value[nameof(KHR_materials_transmission.transmissionTexture)]?.DeserializeAsTexture(root);
				return extension;
			}

			return null;
		}
	}
}
