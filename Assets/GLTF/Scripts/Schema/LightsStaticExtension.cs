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

		public override void Serialize(JsonWriter writer)
		{

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
