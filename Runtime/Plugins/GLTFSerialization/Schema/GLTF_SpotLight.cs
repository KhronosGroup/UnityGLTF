using GLTF.Extensions;
using GLTF.Schema.KHR_lights_punctual;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	public class GLTFSpotLight : GLTFLight
	{
		public float innerConeAngle = 0;
		public float outerConeAngle = (float)(System.Math.PI / 4.0);

		public GLTFSpotLight()
		{
		}

		public GLTFSpotLight(GLTFSpotLight node, GLTFRoot gltfRoot) : base(node, gltfRoot)
		{
			if (node == null) return;

			innerConeAngle = node.innerConeAngle;
			outerConeAngle = node.outerConeAngle;
		}

		public new static GLTFSpotLight Deserialize(GLTFRoot root, JsonReader reader)
		{
			var node = new GLTFSpotLight();

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
					case "spot":
						var spot = Spot.Deserialize(reader);
						node.innerConeAngle = (float) spot.InnerConeAngle;
						node.outerConeAngle = (float) spot.OuterConeAngle;
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

			writer.WritePropertyName("spot");
			writer.WriteStartObject();
			if(innerConeAngle != 0)
			{
				writer.WritePropertyName("innerConeAngle");
				writer.WriteValue(innerConeAngle);
			}
			// if (outerConeAngle != PI / 4)
			{
				writer.WritePropertyName("outerConeAngle");
				writer.WriteValue(outerConeAngle);
			}
			writer.WriteEndObject();

			// //write raw json
			// writer.WriteRaw(",\"spot\":{\"innerConeAngle\":" + innerConeAngle.ToString(System.Globalization.CultureInfo.InvariantCulture));
			// writer.WriteRaw(",\"outerConeAngle\":" + outerConeAngle.ToString(System.Globalization.CultureInfo.InvariantCulture));
			// writer.WriteRaw("}");

			writer.WritePropertyName("color");
			writer.WriteStartArray();
			writer.WriteValue(color.R);
			writer.WriteValue(color.G);
			writer.WriteValue(color.B);
			//writer.WriteValue(color.A);
			writer.WriteEndArray();

			//base.Serialize(writer);

			writer.WriteEndObject();
		}
	}
}
