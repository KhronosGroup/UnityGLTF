using Newtonsoft.Json;

namespace GLTF.Schema
{
	/// <summary>
	/// A texture and its sampler.
	/// </summary>
	public class GLTFTexture : GLTFChildOfRootProperty
	{
		/// <summary>
		/// The index of the sampler used by this texture.
		/// </summary>
		public SamplerId Sampler;

		/// <summary>
		/// The index of the image used by this texture.
		/// </summary>
		public ImageId Source;

		public GLTFTexture()
		{
		}

		public GLTFTexture(GLTFTexture texture, GLTFRoot gltfRoot) : base(texture, gltfRoot)
		{
			if (texture == null) return;

			if (texture.Sampler != null)
			{
				Sampler = new SamplerId(texture.Sampler, gltfRoot);
			}

			if (texture.Source != null)
			{
				Source = new ImageId(texture.Source, gltfRoot);
			}
		}

		public static GLTFTexture Deserialize(GLTFRoot root, JsonReader reader)
		{
			var texture = new GLTFTexture();

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "sampler":
						texture.Sampler = SamplerId.Deserialize(root, reader);
						break;
					case "source":
						texture.Source = ImageId.Deserialize(root, reader);
						break;
					default:
						texture.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return texture;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			if (Sampler != null)
			{
				writer.WritePropertyName("sampler");
				writer.WriteValue(Sampler.Id);
			}

			if (Source != null)
			{
				writer.WritePropertyName("source");
				writer.WriteValue(Source.Id);
			}

			base.Serialize(writer);

			writer.WriteEndObject();
		}
	}
}
