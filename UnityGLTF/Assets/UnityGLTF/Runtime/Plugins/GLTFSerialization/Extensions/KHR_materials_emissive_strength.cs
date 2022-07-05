using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	public class KHR_materials_emissive_strength : GLTFProperty, IExtension
	{
		public KHR_materials_emissive_strength() { }

		public KHR_materials_emissive_strength(KHR_materials_emissive_strength ext, GLTFRoot root) : base(ext, root) { }

		public IExtension Clone(GLTFRoot gltfRoot)
		{
			return new KHR_materials_emissive_strength(this, gltfRoot);
		}

		public JProperty Serialize()
		{
			var jo = new JObject();
			JProperty jProperty = new JProperty(KHR_materials_emissive_strength_Factory.EXTENSION_NAME, jo);

			if(emissiveStrength != 1.0f)
				jo.Add(new JProperty(nameof(emissiveStrength), emissiveStrength));

			return jProperty;
		}

		public float emissiveStrength = 1.0f;
	}

	public class KHR_materials_emissive_strength_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_materials_emissive_strength";

		public KHR_materials_emissive_strength_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new KHR_materials_emissive_strength();

				JToken strength = extensionToken.Value[nameof(KHR_materials_emissive_strength.emissiveStrength)];
				if (strength != null)
					extension.emissiveStrength = strength.Value<float>();

				return extension;
			}

			return null;
		}
	}
}
