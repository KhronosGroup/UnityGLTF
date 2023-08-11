using System;
using GLTF.Extensions;
using Newtonsoft.Json.Linq;
using Color = GLTF.Math.Color;

namespace GLTF.Schema
{
	// https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_specular/README.md
	[Serializable]
	public class KHR_materials_specular : IExtension
	{
		public float specularFactor = 1f;
		public TextureInfo specularTexture; // A channel
		public Color specularColorFactor = COLOR_DEFAULT;
		public TextureInfo specularColorTexture; // RGB channel

		public static readonly Color COLOR_DEFAULT = Color.White;

		public JProperty Serialize()
		{
			var jo = new JObject();
			JProperty jProperty = new JProperty(KHR_materials_specular_Factory.EXTENSION_NAME, jo);

			if (specularFactor != 1) jo.Add(new JProperty(nameof(specularFactor), specularFactor));
			if (specularColorFactor != COLOR_DEFAULT) jo.Add(new JProperty(nameof(specularColorFactor), new JArray(specularColorFactor.R, specularColorFactor.G, specularColorFactor.B)));
			if (specularTexture != null)
				jo.WriteTexture(nameof(specularTexture), specularTexture);

			if (specularColorTexture != null)
				jo.WriteTexture(nameof(specularColorTexture), specularColorTexture);

			return jProperty;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_materials_specular()
			{
				specularFactor = specularFactor, specularTexture = specularTexture,
				specularColorFactor = specularColorFactor, specularColorTexture = specularColorTexture,
			};
		}
	}

	public class KHR_materials_specular_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_materials_specular";

		public KHR_materials_specular_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new KHR_materials_specular();
				extension.specularFactor       = extensionToken.Value[nameof(KHR_materials_specular.specularFactor)]?.Value<float>() ?? 1;
				extension.specularColorFactor  = extensionToken.Value[nameof(KHR_materials_specular.specularColorFactor)]?.DeserializeAsColor() ?? Color.White;
				extension.specularTexture      = extensionToken.Value[nameof(KHR_materials_specular.specularTexture)]?.DeserializeAsTexture(root);
				extension.specularColorTexture = extensionToken.Value[nameof(KHR_materials_specular.specularColorTexture)]?.DeserializeAsTexture(root);
				return extension;
			}

			return null;
		}
	}
}
