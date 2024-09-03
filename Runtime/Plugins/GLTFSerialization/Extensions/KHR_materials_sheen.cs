using System;
using GLTF.Extensions;
using Newtonsoft.Json.Linq;
using Color = GLTF.Math.Color;

namespace GLTF.Schema
{
	// https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_sheen/README.md
	[Serializable]
	public class KHR_materials_sheen : IExtension
	{
		public Color sheenColorFactor = COLOR_DEFAULT;
		public float sheenRoughnessFactor = 0f;
		public TextureInfo sheenColorTexture; 
		public TextureInfo sheenRoughnessTexture; 

		public static readonly Color COLOR_DEFAULT = Color.Black;

		public JProperty Serialize()
		{
			var jo = new JObject();
			JProperty jProperty = new JProperty(KHR_materials_sheen_Factory.EXTENSION_NAME, jo);

			if (sheenRoughnessFactor != 0) jo.Add(new JProperty(nameof(sheenRoughnessFactor), sheenRoughnessFactor));
			if (sheenColorFactor != COLOR_DEFAULT) jo.Add(new JProperty(nameof(sheenColorFactor), new JArray(sheenColorFactor.R, sheenColorFactor.G, sheenColorFactor.B)));
			if (sheenColorTexture != null)
				jo.WriteTexture(nameof(sheenColorTexture), sheenColorTexture);

			if (sheenRoughnessTexture != null)
				jo.WriteTexture(nameof(sheenRoughnessTexture), sheenRoughnessTexture);

			return jProperty;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_materials_sheen()
			{
				sheenRoughnessFactor = sheenRoughnessFactor, sheenColorTexture = sheenColorTexture,
				sheenColorFactor = sheenColorFactor, sheenRoughnessTexture = sheenRoughnessTexture,
			};
		}
	}

	public class KHR_materials_sheen_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_materials_sheen";

		public KHR_materials_sheen_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new KHR_materials_sheen();
				extension.sheenRoughnessFactor       = extensionToken.Value[nameof(KHR_materials_sheen.sheenRoughnessFactor)]?.Value<float>() ?? 1;
				extension.sheenColorFactor  = extensionToken.Value[nameof(KHR_materials_sheen.sheenColorFactor)]?.DeserializeAsColor() ?? Color.White;
				extension.sheenColorTexture      = extensionToken.Value[nameof(KHR_materials_sheen.sheenColorTexture)]?.DeserializeAsTexture(root);
				extension.sheenRoughnessTexture = extensionToken.Value[nameof(KHR_materials_sheen.sheenRoughnessTexture)]?.DeserializeAsTexture(root);
				return extension;
			}

			return null;
		}
	}
}
