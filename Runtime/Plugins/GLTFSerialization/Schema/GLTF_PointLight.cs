using GLTF.Extensions;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	public class GLTFPointLight : GLTFLight
	{

		public GLTFPointLight()
		{
		}

		public GLTFPointLight(GLTFPointLight node, GLTFRoot gltfRoot) : base(node, gltfRoot)
		{
			if (node == null) return;
		}

		public new static GLTFPointLight Deserialize(GLTFRoot root, JsonReader reader)
		{
			var node = new GLTFPointLight();

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "type":
						node.type = reader.ReadAsString();
						break;
					case "color":
						node.color = reader.ReadAsRGBAColor();
						break;
					case "range":
						node.range = (float)reader.ReadAsDouble();
						break;
					case "intensity":
						node.intensity = (float)reader.ReadAsDouble();
						break;
					case "name":
						node.name = reader.ReadAsString();
						break;
				}
			}

			return node;
		}

		public override void Serialize(JsonWriter writer)
		{
			base.Serialize(writer);
		}
	}
}
