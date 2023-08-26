using System;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	// https: //github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_ior
	[Serializable]
	public class KHR_materials_ior : IExtension
	{
		public const float DefaultIor = 1.5f;
		public float ior = 1.5f;

		public JProperty Serialize()
		{
			var obj = new JObject();
			if (System.Math.Abs(ior - DefaultIor) > 0.00001f)
				obj.Add(new JProperty(nameof(ior), ior));
			JProperty jProperty = new JProperty(KHR_materials_ior_Factory.EXTENSION_NAME, obj);
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
				var extension = new KHR_materials_ior();
				if (strength != null)
				{
					extension.ior = strength.Value<float>();
				}
				return extension;
			}

			return null;
		}
	}
}
