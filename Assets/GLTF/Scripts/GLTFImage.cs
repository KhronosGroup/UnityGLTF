using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace GLTF
{
    /// <summary>
    /// Image data used to create a texture. Image can be referenced by URI or
    /// `bufferView` index. `mimeType` is required in the latter case.
    /// </summary>
    [System.Serializable]
    public class GLTFImage : GLTFChildOfRootProperty
    {
        /// <summary>
        /// The uri of the image.  Relative paths are relative to the .gltf file.
        /// Instead of referencing an external file, the uri can also be a data-uri.
        /// The image format must be jpg, png, bmp, or gif.
        /// </summary>
        public GLTFUri uri;

        /// <summary>
        /// The image's MIME type.
        /// <minLength>1</minLength>
        /// </summary>
        public string mimeType;

        /// <summary>
        /// The index of the bufferView that contains the image.
        /// Use this instead of the image's uri property.
        /// </summary>
        public GLTFBufferViewId bufferView;

        /// <summary>
        /// Return the GLTFTexture's Texture object.
        /// </summary>
        public Texture2D texture;

        /// <summary>
        /// Ensure the image is loaded from its URI.
        /// If the image data is stored in a bufferView it will be loaded separately.
        /// <see cref="GLTFRoot.LoadAllScenes"/>
        /// </summary>
        public IEnumerator Load()
        {
            if (uri != null)
            {
                yield return uri.LoadTexture();
                texture = uri.texture;
            }
            else
            {
                texture = new Texture2D(0, 0);
                texture.LoadImage(bufferView.Value.Data);
            }
        }

        public static GLTFImage Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var image = new GLTFImage();
            
            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "uri":
                        image.uri = GLTFUri.Deserialize(root, reader);
                        break;
                    case "mimeType":
                        image.mimeType = reader.ReadAsString();
                        break;
                    case "bufferView":
                        image.bufferView = GLTFBufferViewId.Deserialize(root, reader);
                        break;
                    case "name":
                        image.name = reader.ReadAsString();
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return image;
        }
    }
}
