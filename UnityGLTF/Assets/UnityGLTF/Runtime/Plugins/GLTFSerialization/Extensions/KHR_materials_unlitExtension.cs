using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	/// <summary>
	/// This extension defines an unlit shading model for use in glTF 2.0 materials, as an alternative to the Physically Based Rendering (PBR) shading models provided by the core specification.
	///
	/// Spec can be found here:
	/// https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_materials_unlit
	/// </summary>
	public class KHR_MaterialsUnlitExtension : GLTFProperty, IExtension
	{
		public KHR_MaterialsUnlitExtension() { }

		public KHR_MaterialsUnlitExtension(KHR_MaterialsUnlitExtension ext, GLTFRoot root) : base(ext, root) { }

		public IExtension Clone(GLTFRoot gltfRoot)
		{
			return new KHR_MaterialsUnlitExtension(this, gltfRoot);
		}

		override public void Serialize(JsonWriter writer)
		{
			writer.WritePropertyName(KHR_MaterialsUnlitExtensionFactory.EXTENSION_NAME);
			writer.WriteStartObject();
			base.Serialize(writer);
			writer.WriteEndObject();
		}

		public JProperty Serialize()
		{
			JTokenWriter writer = new JTokenWriter();
			Serialize(writer);
			return (JProperty)writer.Token;
		}
	}
}
