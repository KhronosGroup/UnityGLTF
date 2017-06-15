using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace GLTF
{
	public class LightsStaticExtension : TextureInfo, Extension
	{
		/// <summary>
		/// A scalar multiplier controlling the amount of light applied from the lightmap.
		/// </summary>
		public double Strength = 1.0;

		public static bool Equivalent(double a, double b, double e = 1e-6)
		{
			return a <= b + e && a >= b - e;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			SerializeProperties(writer);
			base.SerializeProperties(writer);

			writer.WriteEndObject();
		}

		public new void SerializeProperties(JsonWriter writer)
		{
			// get around float precision equality bugs
			if (!Equivalent(Strength, 1))
			{
				writer.WritePropertyName("strength");
				writer.WriteValue(Strength);
			}
		}
	}

	public class LightsStaticFactory : ExtensionFactory
	{
		public LightsStaticFactory()
		{
			ExtensionName = "AVR_lights_static";
		}

		public override Extension Deserialize(GLTFRoot root, JsonReader reader)
		{
			return DeserializeSpecific(root, reader);
		}

		private LightsStaticExtension DeserializeSpecific(GLTFRoot root, JsonReader reader)
		{
			var lightmapData = new LightsStaticExtension();

			if (reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				throw new Exception("Asset must be an object.");
			}

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "index":
						lightmapData.Index = new TextureId() {Root = root, Id = reader.ReadAsInt32().Value};
						break;
					case "texCoord":
						lightmapData.TexCoord = reader.ReadAsInt32().Value;
						break;
					case "strength":
						lightmapData.Strength = reader.ReadAsDouble().Value;
						break;
					default:
						lightmapData.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return lightmapData;
		}
	}
}
