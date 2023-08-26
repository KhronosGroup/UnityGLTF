using System;
using GLTF.Extensions;
using GLTF.Math;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	// https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_clearcoat/README.md
	[Serializable]
	public class KHR_materials_clearcoat : IExtension
	{
		public float clearcoatFactor = 0f;
		public TextureInfo clearcoatTexture; // R channel
		public float clearcoatRoughnessFactor = 0f;
		public TextureInfo clearcoatRoughnessTexture; // G channel
		public TextureInfo clearcoatNormalTexture;

		public JProperty Serialize()
		{
			var jo = new JObject();
			JProperty jProperty = new JProperty(KHR_materials_clearcoat_Factory.EXTENSION_NAME, jo);

			// TODO all these properties should only be added if non-default

			if (clearcoatFactor != 0)
				jo.Add(new JProperty(nameof(clearcoatFactor), clearcoatFactor));
			if (clearcoatTexture != null)
				jo.WriteTexture(nameof(clearcoatTexture), clearcoatTexture);

			if (clearcoatRoughnessFactor != 0)
				jo.Add(new JProperty(nameof(clearcoatRoughnessFactor), clearcoatRoughnessFactor));
			if (clearcoatRoughnessTexture != null)
				jo.WriteTexture(nameof(clearcoatRoughnessTexture), clearcoatRoughnessTexture);

			if (clearcoatNormalTexture != null)
				jo.WriteTexture(nameof(clearcoatNormalTexture), clearcoatNormalTexture);

			return jProperty;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_materials_clearcoat()
			{
				clearcoatFactor = clearcoatFactor, clearcoatTexture = clearcoatTexture,
				clearcoatRoughnessFactor = clearcoatRoughnessFactor, clearcoatRoughnessTexture = clearcoatRoughnessTexture,
				clearcoatNormalTexture = clearcoatNormalTexture,
			};
		}
	}

	public class KHR_materials_clearcoat_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_materials_clearcoat";

		public KHR_materials_clearcoat_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new KHR_materials_clearcoat();
				extension.clearcoatFactor = extensionToken.Value[nameof(KHR_materials_clearcoat.clearcoatFactor)]?.Value<float>() ?? 0;
				extension.clearcoatTexture = extensionToken.Value[nameof(KHR_materials_clearcoat.clearcoatTexture)]?.DeserializeAsTexture(root);
				extension.clearcoatRoughnessFactor = extensionToken.Value[nameof(KHR_materials_clearcoat.clearcoatRoughnessFactor)]?.Value<float>() ?? 0;
				extension.clearcoatRoughnessTexture = extensionToken.Value[nameof(KHR_materials_clearcoat.clearcoatRoughnessTexture)]?.DeserializeAsTexture(root);
				extension.clearcoatNormalTexture = extensionToken.Value[nameof(KHR_materials_clearcoat.clearcoatNormalTexture)]?.DeserializeAsTexture(root);
				return extension;
			}

			return null;
		}
	}
}
