using System;
using GLTF.Extensions;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
// https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_dispersion
	[Serializable]
	public class KHR_materials_dispersion : IExtension
	{
		public float dispersion = 0.0f;

		public JProperty Serialize()
		{
			var jo = new JObject();
			if (dispersion != 0) jo.Add(new JProperty(nameof(dispersion), dispersion));

			JProperty jProperty = new JProperty(KHR_materials_dispersion_Factory.EXTENSION_NAME, jo);
			return jProperty;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_materials_dispersion() { dispersion =  dispersion};
		}
	}

	public class KHR_materials_dispersion_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_materials_dispersion";

		public KHR_materials_dispersion_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new KHR_materials_dispersion();
				extension.dispersion = extensionToken.Value[nameof(KHR_materials_dispersion.dispersion)]?.Value<float>() ?? 0;
				return extension;
			}

			return null;
		}
	}
}
    
