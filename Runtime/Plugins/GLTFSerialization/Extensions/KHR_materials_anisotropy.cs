using System;
using GLTF.Extensions;
using Newtonsoft.Json.Linq;
using Color = GLTF.Math.Color;

namespace GLTF.Schema
{
	// https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_anisotropy/README.md
	[Serializable]
	public class KHR_materials_anisotropy : IExtension
	{
		public float anisotropyStrength = 0f;
		public float anisotropyRotation = 0f;
		public TextureInfo anisotropyTexture; 
	
		public JProperty Serialize()
		{
			var jo = new JObject();
			JProperty jProperty = new JProperty(KHR_materials_anisotropy_Factory.EXTENSION_NAME, jo);

			if (anisotropyRotation != 0f) jo.Add(new JProperty(nameof(anisotropyRotation), anisotropyRotation));
			if (anisotropyStrength != 0f) jo.Add(new JProperty(nameof(anisotropyStrength), anisotropyStrength));
			if (anisotropyTexture != null)
				jo.WriteTexture(nameof(anisotropyTexture), anisotropyTexture);

			return jProperty;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_materials_anisotropy()
			{
				anisotropyRotation = anisotropyRotation, 
				anisotropyStrength = anisotropyStrength,
				anisotropyTexture = anisotropyTexture, 
			};
		}
	}

	public class KHR_materials_anisotropy_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_materials_anisotropy";

		public KHR_materials_anisotropy_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new KHR_materials_anisotropy();
				extension.anisotropyRotation = extensionToken.Value[nameof(KHR_materials_anisotropy.anisotropyRotation)]?.Value<float>() ?? 0f;
				extension.anisotropyStrength = extensionToken.Value[nameof(KHR_materials_anisotropy.anisotropyStrength)]?.Value<float>() ?? 0f;
				extension.anisotropyTexture  = extensionToken.Value[nameof(KHR_materials_anisotropy.anisotropyTexture)]?.DeserializeAsTexture(root);
				return extension;
			}

			return null;
		}
	}
}
