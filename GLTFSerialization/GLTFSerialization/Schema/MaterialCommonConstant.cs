using System;
using GLTF.Extensions;
using GLTF.Math;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	public class MaterialCommonConstant : GLTFProperty
	{
		/// <summary>
		/// Used to scale the ambient light contributions to this material
		/// </summary>
		public Color AmbientFactor = Color.White;

		/// <summary>
		/// Texture used to store pre-computed direct lighting
		/// </summary>
		public TextureInfo LightmapTexture;

		/// <summary>
		/// Scale factor for the lightmap texture
		/// </summary>
		public Color LightmapFactor = Color.White;

		public static MaterialCommonConstant Deserialize(GLTFRoot root, JsonReader reader)
		{
			var commonConstant = new MaterialCommonConstant();

			if (reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				throw new Exception("Asset must be an object.");
			}

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "ambientFactor":
						commonConstant.AmbientFactor = reader.ReadAsRGBColor();
						break;
					case "lightmapTexture":
						commonConstant.LightmapTexture = TextureInfo.Deserialize(root, reader);
						break;
					case "lightmapFactor":
						commonConstant.LightmapFactor = reader.ReadAsRGBColor();
						break;
					default:
						commonConstant.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return commonConstant;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			if (AmbientFactor != Color.White)
			{
				writer.WritePropertyName("ambientFactor");
				writer.WriteStartArray();
				writer.WriteValue(AmbientFactor.R);
				writer.WriteValue(AmbientFactor.G);
				writer.WriteValue(AmbientFactor.B);
				writer.WriteEndArray();
			}

			if (LightmapTexture != null)
			{
				writer.WritePropertyName("lightmapTexture");
				LightmapTexture.Serialize(writer);
			}

			if (LightmapFactor != Color.White)
			{
				writer.WritePropertyName("lightmapFactor");
				writer.WriteStartArray();
				writer.WriteValue(LightmapFactor.R);
				writer.WriteValue(LightmapFactor.G);
				writer.WriteValue(LightmapFactor.B);
				writer.WriteEndArray();
			}

			base.Serialize(writer);

			writer.WriteEndObject();
		}
	}
}
