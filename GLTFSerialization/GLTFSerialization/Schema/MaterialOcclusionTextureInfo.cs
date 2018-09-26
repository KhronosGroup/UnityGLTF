using System;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	public class OcclusionTextureInfo : TextureInfo
	{
		public const string STRENGTH = "strength";

		/// <summary>
		/// A scalar multiplier controlling the amount of occlusion applied.
		/// A value of 0.0 means no occlusion.
		/// A value of 1.0 means full occlusion.
		/// This value is ignored if the corresponding texture is not specified.
		/// This value is linear.
		/// <minimum>0.0</minimum>
		/// <maximum>1.0</maximum>
		/// </summary>
		public double Strength = 1.0f;

		public OcclusionTextureInfo()
		{
		}

		public OcclusionTextureInfo(OcclusionTextureInfo occulisionTextureInfo, GLTFRoot gltfRoot) : base(occulisionTextureInfo, gltfRoot)
		{
			Strength = occulisionTextureInfo.Strength;
		}

		public static new OcclusionTextureInfo Deserialize(GLTFRoot root, JsonReader reader)
		{
			var textureInfo = new OcclusionTextureInfo();

			if (reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				throw new Exception("Asset must be an object.");
			}

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case INDEX:
						textureInfo.Index = TextureId.Deserialize(root, reader);
						break;
					case TEXCOORD:
						textureInfo.TexCoord = reader.ReadAsInt32().Value;
						break;
					case STRENGTH:
						textureInfo.Strength = reader.ReadAsDouble().Value;
						break;
					default:
						textureInfo.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return textureInfo;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			if (Strength != 1.0f)
			{
				writer.WritePropertyName("strength");
				writer.WriteValue(Strength);
			}

			// Write the parent class' properties only.
			// Don't accidentally call write start/end object.
			base.SerializeProperties(writer);

			writer.WriteEndObject();
		}
	}
}
