using System.Collections;

namespace GLTF
{
    /// <summary>
    /// Image data used to create a texture. Image can be referenced by URI or
    /// `bufferView` index. `mimeType` is required in the latter case.
    /// </summary>
    public class GLTFImage
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

        public string name;

        /// <summary>
        /// Get the image buffer data as a byte array.
        /// (Note: this creates a copy of the data inside the buffer)
        /// </summary>
        public byte[] Data
        {
            get
            {
                if (bufferView != null)
                {
                    return bufferView.Value.Data;
                }

                return uri.data;
            }
        }

        /// <summary>
        /// Ensure the image is loaded from its URI.
        /// If the image data is stored in a bufferView it will be loaded separately.
        /// <see cref="GLTFRoot.LoadAllScenes"/>
        /// </summary>
        public IEnumerator Load()
        {
            if (uri != null)
            {
                yield return uri.Load();
            }
        }
    }
}
