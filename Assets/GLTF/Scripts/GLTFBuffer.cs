using System;
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

        public virtual byte[] Data
        {
            get
            {
                return uri.data;
            }
        }

        public IEnumerator Load()
        {
            if(uri != null)
                yield return uri.Load();
        }
    }

    public class GLTFInternalBuffer : GLTFBuffer
    {
        private byte[] _data;

        public GLTFInternalBuffer(GLTFBuffer json, byte[] data)
        {
            name = json.name;
            _data = data;
        }

        public override byte[] Data
        {
            get
            {
                return _data;
            }
        }

        public new int byteLength
        {
            get
            {
                return _data.Length;
            }
        }
    }
}
