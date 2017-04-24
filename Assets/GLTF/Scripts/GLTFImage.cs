using System;
using System.Collections;
using System.ComponentModel;
using Newtonsoft.Json;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// Image data used to create a texture. Image can be referenced by URI or
    /// `bufferView` index. `mimeType` is required in the latter case.
    /// </summary>
    [Serializable]
    public class GLTFImage : GLTFChildOfRootProperty
    {
        /// <summary>
        /// The uri of the image.  Relative paths are relative to the .gltf file.
        /// Instead of referencing an external file, the uri can also be a data-uri.
        /// The image format must be jpg, png, bmp, or gif.
        /// </summary>
        public GLTFUri Uri;

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

        /// <summary>
        /// Return the GLTFTexture's Texture object.
        /// </summary>
        public Texture2D Texture;

        /// <summary>
        /// Ensure the image is loaded from its URI.
        /// If the image data is stored in a bufferView it will be loaded separately.
        /// <see cref="GLTFRoot.LoadAllScenes"/>
        /// </summary>
        public IEnumerator Load()
        {
            if (Uri != null)
            {
                yield return Uri.LoadTexture();
                Texture = Uri.Texture;
            }
            else
            {
                Texture = new Texture2D(0, 0);
	            var bufferView = BufferView.Value;
				var bufferData = bufferView.Buffer.Value.Data;
	            var data = new byte[bufferView.ByteLength];
	            Buffer.BlockCopy(bufferData, bufferView.ByteOffset, data, 0, data.Length);
                Texture.LoadImage(data);
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
                        image.Uri = GLTFUri.Deserialize(root, reader);
                        break;
                    case "mimeType":
                        image.MimeType = reader.ReadAsString();
                        break;
                    case "bufferView":
                        image.BufferView = GLTFBufferViewId.Deserialize(root, reader);
                        break;
                    case "name":
                        image.Name = reader.ReadAsString();
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
