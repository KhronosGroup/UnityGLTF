using GLTF.Utilities;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	/// <summary>
	/// A buffer points to binary geometry, animation, or skins.
	/// </summary>
	public class GLTFBuffer : GLTFChildOfRootProperty
	{
		/// <summary>
		/// The uri of the buffer.
		/// Relative paths are relative to the .gltf file.
		/// Instead of referencing an external file, the uri can also be a data-uri.
		/// </summary>
		public string Uri;

		/// <summary>
		/// The length of the buffer in bytes.
		/// <minimum>0</minimum>
		/// </summary>
		public uint ByteLength;

		public GLTFBuffer()
		{
		}

		public GLTFBuffer(GLTFBuffer buffer, GLTFRoot gltfRoot) : base(buffer, gltfRoot)
		{
			if (buffer == null) return;
			Uri = buffer.Uri;
			ByteLength = buffer.ByteLength;
		}

		public static GLTFBuffer Deserialize(GLTFRoot root, JsonReader reader)
		{
			var buffer = new GLTFBuffer();

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "uri":
						buffer.Uri = reader.ReadAsString();
						break;
					case "byteLength":
						buffer.ByteLength = reader.ReadDoubleAsUInt32();
						break;
					default:
						buffer.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return buffer;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			if (Uri != null)
			{
				writer.WritePropertyName("uri");
				writer.WriteValue(Uri);
			}

			writer.WritePropertyName("byteLength");
			writer.WriteValue(ByteLength);

			base.Serialize(writer);

			writer.WriteEndObject();
		}
	}
}
