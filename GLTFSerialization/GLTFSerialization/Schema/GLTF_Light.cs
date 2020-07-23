using System.Collections.Generic;
using System.Linq;
using GLTF.Extensions;
using GLTF.Math;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	/// <summary>
	/// A node in the node hierarchy.
	/// When the node contains `skin`, all `mesh.primitives` must contain `JOINT`
	/// and `WEIGHT` attributes.  A node can have either a `matrix` or any combination
	/// of `translation`/`rotation`/`scale` (TRS) properties.
	/// TRS properties are converted to matrices and postmultiplied in
	/// the `T * R * S` order to compose the transformation matrix;
	/// first the scale is applied to the vertices, then the rotation, and then
	/// the translation. If none are provided, the transform is the Identity.
	/// When a node is targeted for animation
	/// (referenced by an animation.channel.target), only TRS properties may be present;
	/// `matrix` will not be present.
	/// </summary>
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
