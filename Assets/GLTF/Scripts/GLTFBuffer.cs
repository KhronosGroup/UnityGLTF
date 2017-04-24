using System;
using System.Collections;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// A buffer points to binary geometry, animation, or skins.
    /// </summary>
    [System.Serializable]
    public class GLTFBuffer : GLTFChildOfRootProperty
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

        public virtual byte[] Data
        {
            get
            {
                return uri.data;
            }
        }

        public IEnumerator Load()
        {
            if (uri != null)
            {
                yield return uri.LoadBuffer();
            }
                
        }

        public static GLTFBuffer Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var buffer = new GLTFBuffer();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "uri":
                        buffer.uri = GLTFUri.Deserialize(root, reader);
                        break;
                    case "byteLength":
                        buffer.byteLength = reader.ReadAsInt32().Value;
                        break;
                    case "name":
                        buffer.name = reader.ReadAsString();
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return buffer;
        }
    }

    /// <summary>
    /// The internal buffer references data stored in the binary chunk of a .glb file.
    /// </summary>
    [System.Serializable]
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
