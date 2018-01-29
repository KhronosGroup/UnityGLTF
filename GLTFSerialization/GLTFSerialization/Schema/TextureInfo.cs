using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	/// <summary>
	/// Reference to a texture.
	/// </summary>
	public class TextureInfo : GLTFProperty
	{
		public const string INDEX = "index";
		public const string TEXCOORD = "texCoord";

		/// <summary>
		/// The index of the texture.
		/// </summary>
		public TextureId Index;

		/// <summary>
		/// This integer value is used to construct a string in the format
		/// TEXCOORD_<set index> which is a reference to a key in
		/// mesh.primitives.attributes (e.g. A value of 0 corresponds to TEXCOORD_0).
		/// </summary>
		public int TexCoord = 0;

		public TextureInfo()
		{
		}

		public TextureInfo(TextureInfo textureInfo, GLTFRoot gltfRoot) : base(textureInfo)
		{
			if (textureInfo == null) return;

			Index = new TextureId(textureInfo.Index, gltfRoot);
			TexCoord = textureInfo.TexCoord;
		}

		public static TextureInfo Deserialize(GLTFRoot root, JsonReader reader)
		{
			var textureInfo = new TextureInfo();

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
					default:
						textureInfo.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return textureInfo;
		}

		public static TextureInfo Deserialize(GLTFRoot root, JProperty jProperty)
		{
			var textureInfo = new TextureInfo();

			foreach (JToken child in jProperty.Children())
			{
				if(child is JProperty)
				{
					JProperty childAsJProperty = child as JProperty;
					switch(childAsJProperty.Name)
					{
						case "index":
							textureInfo.Index = TextureId.Deserialize(root, childAsJProperty);
							break;
						case "texCoord":
							textureInfo.TexCoord = (int)childAsJProperty.Value;
							break;
						default:
							// todo: implement
							//textureInfo.DefaultPropertyDeserializer(root, childAsJProperty);
							break;
					}
				}
			}

			return textureInfo;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			SerializeProperties(writer);

			writer.WriteEndObject();
		}

		public void SerializeProperties(JsonWriter writer)
		{
			writer.WritePropertyName(INDEX);
			writer.WriteValue(Index.Id);

			if (TexCoord != 0)
			{
				writer.WritePropertyName(TEXCOORD);
				writer.WriteValue(TexCoord);
			}

			base.Serialize(writer);
		}
	}
}
