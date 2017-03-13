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

    /// <summary>
    /// The internal buffer references data stored in the binary chunk of a .glb file.
    /// </summary>
    public class GLTFInternalBuffer : GLTFBuffer
    {
        private byte[] data;

        public GLTFInternalBuffer(GLTFBuffer gltfBuffer, byte[] data)
        {
            name = gltfBuffer.name;
            this.data = data;
        }

        public override byte[] Data
        {
            get
            {
                return data;
            }
        }

        public new int byteLength
        {
            get
            {
                return data.Length;
            }
        }
    }
}
