using Newtonsoft.Json;

namespace GLTF
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
        public int ByteLength;

        public static GLTFBuffer Deserialize(GLTFRoot root, JsonTextReader reader)
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
                        buffer.ByteLength = reader.ReadAsInt32().Value;
                        break;
	                default:
		                buffer.DefaultPropertyDeserializer(root, reader);
		                break;
				}
            }

            return buffer;
        }
    }
}
