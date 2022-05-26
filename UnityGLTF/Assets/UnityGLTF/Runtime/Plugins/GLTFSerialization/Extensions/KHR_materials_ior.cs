using System;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	// https: //github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_ior
	[Serializable]
	public class KHR_materials_ior : IExtension
	{
		public float ior = 1.5f;

		public JProperty Serialize()
		{
			JProperty jProperty = new JProperty(KHR_materials_ior_Factory.EXTENSION_NAME,
				new JObject(
					new JProperty(nameof(ior), ior)
				)
			);
			return jProperty;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_materials_ior() { ior = ior };
		}
	}

	public class KHR_materials_ior_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_materials_ior";

		public KHR_materials_ior_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				JToken strength = extensionToken.Value[nameof(KHR_materials_ior.ior)];

				if (strength != null)
				{
					var extension = new KHR_materials_ior();
					extension.ior = strength.Value<float>();
					return extension;
				}
			}

			return null;
		}
	}
}
