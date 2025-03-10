using GLTF.Extensions;
using GLTF.Math;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	public class GLTFLight : GLTFChildOfRootProperty
	{
		public string name;
		public Color color;
		public string type;
		public float intensity;
		public float range;

		public GLTFLight()
		{
		}

		public GLTFLight(GLTFLight light, GLTFRoot gltfRoot) : base(light, gltfRoot)
		{
			if (light == null) return;

			color = light.color;
			type = light.type;
			name = "";
			intensity = 1.0f;
		}

		public static GLTFLight Deserialize(GLTFRoot root, JsonReader reader)
		{
			var node = new GLTFLight();

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
			writer.WriteStartObject();

			writer.WritePropertyName("type");
			writer.WriteValue(type);

			if (range > 0)
			{
				writer.WritePropertyName("range");
				writer.WriteValue(range);
			}

			if (intensity != 1.0f)
			{
				writer.WritePropertyName("intensity");
				writer.WriteValue(intensity);
			}
			if (!string.IsNullOrEmpty(name))
			{
				writer.WritePropertyName("name");
				writer.WriteValue(name);
			}

			writer.WritePropertyName("color");
			writer.WriteStartArray();
			writer.WriteValue(color.R);
			writer.WriteValue(color.G);
			writer.WriteValue(color.B);
			writer.WriteEndArray();

			base.Serialize(writer);

			writer.WriteEndObject();
		}
	}
}
