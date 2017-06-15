using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace GLTF
{
	public class TextureOffsetTileExtension : Extension
	{
		/// <summary>
		/// The horizontal offset of the UV coordinate origin as a percentage of the texture width.
		/// </summary>
		public double OffsetS = 0;

		/// <summary>
		/// The vertical offset of the UV coordinate origin as a percentage of the texture height.
		/// </summary>
		public double OffsetT = 0;

		/// <summary>
		/// The scale factor applied to the horizontal component of the UV coordinates.
		/// </summary>
		public double TileS = 1;

		/// <summary>
		/// The scale factor applied to the vertical component of the UV coordinates.
		/// </summary>
		public double TileT = 1;

		public void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			// get around float precision equality bugs
			if (!Equivalent(OffsetS, 0))
			{
				writer.WritePropertyName("offsetS");
				writer.WriteValue(OffsetS);
			}

			if (!Equivalent(OffsetT, 0))
			{
				writer.WritePropertyName("offsetT");
				writer.WriteValue(OffsetT);
			}

			if (!Equivalent(TileS, 1))
			{
				writer.WritePropertyName("tileS");
				writer.WriteValue(TileS);
			}

			if (!Equivalent(TileT, 1))
			{
				writer.WritePropertyName("tileT");
				writer.WriteValue(TileT);
			}

			writer.WriteEndObject();
		}

		public static bool Equivalent(double a, double b, double e = 1e-6)
		{
			return a <= b + e && a >= b - e;
		}
	}

	public class TextureOffsetTileFactory : ExtensionFactory
	{
		public TextureOffsetTileFactory()
		{
			ExtensionName = "AVR_texture_offset_tile";
		}

		public override Extension Deserialize(GLTFRoot root, JsonReader reader)
		{
			return DeserializeSpecific(root, reader);
		}

		private TextureOffsetTileExtension DeserializeSpecific(GLTFRoot root, JsonReader reader)
		{
			var offsetTileData = new TextureOffsetTileExtension();

			if (reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				throw new Exception("Asset must be an object.");
			}

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "offsetS":
						offsetTileData.OffsetS = reader.ReadAsDouble().Value;
						break;
					case "offsetT":
						offsetTileData.OffsetT = reader.ReadAsDouble().Value;
						break;
					case "tileS":
						offsetTileData.TileS = reader.ReadAsDouble().Value;
						break;
					case "tileT":
						offsetTileData.TileT = reader.ReadAsDouble().Value;
						break;
					default:
						throw new Exception("The AVR_texture_offset_tile extension does not allow extra properties.");
				}
			}

			return offsetTileData;
		}
	}
}
