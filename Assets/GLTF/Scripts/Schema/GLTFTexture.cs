using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// A texture and its sampler.
    /// </summary>
    public class GLTFTexture : GLTFChildOfRootProperty
    {
        /// <summary>
        /// The index of the sampler used by this texture.
        /// </summary>
        public GLTFSamplerId Sampler;

        /// <summary>
        /// The index of the image used by this texture.
        /// </summary>
        public GLTFImageId Source;

        public static GLTFTexture Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var texture = new GLTFTexture();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "sampler":
                        texture.Sampler = GLTFSamplerId.Deserialize(root, reader);
                        break;
                    case "source":
                        texture.Source = GLTFImageId.Deserialize(root, reader);
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
