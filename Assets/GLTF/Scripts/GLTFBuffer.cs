using System.Collections;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// A buffer points to binary geometry, animation, or skins.
    /// </summary>
    public class GLTFBuffer
    {
        /// <summary>
        /// The uri of the buffer.
        /// Relative paths are relative to the .gltf file.
        /// Instead of referencing an external file, the uri can also be a data-uri.
        /// </summary>
        [JsonProperty(Required = Required.DisallowNull)]
        public GLTFUri uri;

        /// <summary>
        /// The length of the buffer in bytes.
        /// <minimum>0</minimum>
        /// </summary>
        public int byteLength = 0;

        public string name;

        public byte[] Data
        {
            get
            {
                return uri.data;
            }
        }

        public IEnumerator Load()
        {
            yield return uri.Load();
        }
    }
}
