using System;
using GLTF.Extensions;
using Newtonsoft.Json.Linq;
using Color = GLTF.Math.Color;

namespace GLTF.Schema
{
    // https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_volume
	[Serializable]
	public class KHR_materials_volume : IExtension
	{
		public float thicknessFactor = 0f;
		public TextureInfo thicknessTexture;// thicknessTexture // G channel
		public float attenuationDistance = float.PositiveInfinity;
		public Color attenuationColor = new Color(1, 1, 1, 1);

		public static readonly Color COLOR_DEFAULT = Color.White;

		public JProperty Serialize()
		{
			var jo = new JObject();
			JProperty jProperty = new JProperty(KHR_materials_volume_Factory.EXTENSION_NAME, jo);

			if (thicknessFactor != 0) jo.Add(new JProperty(nameof(thicknessFactor), thicknessFactor));
			if (!float.IsPositiveInfinity(attenuationDistance))
			{
				// 0 not allowed
				if (!(attenuationDistance > 0))
					attenuationDistance = 0.0001f;

				jo.Add(new JProperty(nameof(attenuationDistance), attenuationDistance));
			}
			if (attenuationColor != COLOR_DEFAULT) jo.Add(new JProperty(nameof(attenuationColor), new JArray(attenuationColor.R, attenuationColor.G, attenuationColor.B)));
			if (thicknessTexture != null)
				jo.WriteTexture(nameof(thicknessTexture), thicknessTexture);

			return jProperty;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_materials_volume()
			{
				thicknessFactor = thicknessFactor, attenuationDistance = attenuationDistance,
				attenuationColor = attenuationColor, thicknessTexture = thicknessTexture,
			};
		}
	}

	public class KHR_materials_volume_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_materials_volume";

		public KHR_materials_volume_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new KHR_materials_volume();
				extension.thicknessFactor     = extensionToken.Value[nameof(KHR_materials_volume.thicknessFactor)]?.Value<float>() ?? 0;
				extension.attenuationColor    = extensionToken.Value[nameof(KHR_materials_volume.attenuationColor)]?.DeserializeAsColor() ?? Color.White;
				extension.attenuationDistance = extensionToken.Value[nameof(KHR_materials_volume.attenuationDistance)]?.Value<float>() ?? float.PositiveInfinity;
				extension.thicknessTexture    = extensionToken.Value[nameof(KHR_materials_volume.thicknessTexture)]?.DeserializeAsTexture(root);
				return extension;
			}

			return null;
		}
	}
}
