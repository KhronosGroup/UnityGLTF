using System;
using System.Collections;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// A buffer points to binary geometry, animation, or skins.
    /// </summary>
    [Serializable]
    public class GLTFBuffer : GLTFChildOfRootProperty
    {
        /// <summary>
        /// The uri of the buffer.
        /// Relative paths are relative to the .gltf file.
        /// Instead of referencing an external file, the uri can also be a data-uri.
        /// </summary>
        public GLTFUri Uri;

        /// <summary>
        /// The length of the buffer in bytes.
        /// <minimum>0</minimum>
        /// </summary>
        public int ByteLength;

        public virtual byte[] Data
        {
            get
            {
                return Uri.Data;
            }
        }

        public IEnumerator Load()
        {
            if (Uri != null)
            {
                yield return Uri.LoadBuffer();
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
                        buffer.Uri = GLTFUri.Deserialize(root, reader);
                        break;
                    case "byteLength":
                        buffer.ByteLength = reader.ReadAsInt32().Value;
                        break;
                    case "name":
                        buffer.Name = reader.ReadAsString();
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
    [Serializable]
    public class GLTFGLBBuffer : GLTFBuffer
    { 

        public GLTFGLBBuffer(GLTFBuffer gltfBuffer, byte[] data)
        {
            Name = gltfBuffer.Name;
            _data = data;
        }

	    private readonly byte[] _data;

		public override byte[] Data
        {
            get { return _data; }
        }

        public new int ByteLength
        {
            get { return _data.Length; }
        }
    }
}
