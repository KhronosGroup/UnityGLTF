using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// Image data used to create a texture. Image can be referenced by URI or
    /// `bufferView` index. `mimeType` is required in the latter case.
    /// </summary>
    public class GLTFImage : GLTFChildOfRootProperty
    {
        /// <summary>
        /// The uri of the image.  Relative paths are relative to the .gltf file.
        /// Instead of referencing an external file, the uri can also be a data-uri.
        /// The image format must be jpg, png, bmp, or gif.
        /// </summary>
        public string Uri;

        /// <summary>
        /// The image's MIME type.
        /// <minLength>1</minLength>
        /// </summary>
        public string MimeType;

        /// <summary>
        /// The index of the bufferView that contains the image.
        /// Use this instead of the image's uri property.
        /// </summary>
        public GLTFBufferViewId BufferView;

        public static GLTFImage Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var image = new GLTFImage();
            
            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "uri":
                        image.Uri = reader.ReadAsString();
                        break;
                    case "mimeType":
                        image.MimeType = reader.ReadAsString();
                        break;
                    case "bufferView":
                        image.BufferView = GLTFBufferViewId.Deserialize(root, reader);
                        break;
					default:
						image.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return image;
        }
    }
}
