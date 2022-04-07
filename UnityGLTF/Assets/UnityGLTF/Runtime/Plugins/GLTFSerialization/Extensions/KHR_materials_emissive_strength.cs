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

		public override void Serialize(JsonWriter writer)
		{
			writer.WritePropertyName(KHR_materials_emissive_strength_Factory.EXTENSION_NAME);
			writer.WriteStartObject();
			writer.WritePropertyName(nameof(emissiveStrength));
			writer.WriteValue(emissiveStrength);
			writer.WriteEndObject();
		}

		public JProperty Serialize()
		{
			JTokenWriter writer = new JTokenWriter();
			Serialize(writer);
			return (JProperty)writer.Token;
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
				JToken strength = extensionToken.Value[nameof(KHR_materials_emissive_strength.emissiveStrength)];

				if (strength != null)
				{
					var extension = new KHR_materials_emissive_strength();
					extension.emissiveStrength = strength.Value<float>();
					return extension;
				}
			}

			return null;
		}
	}
}
