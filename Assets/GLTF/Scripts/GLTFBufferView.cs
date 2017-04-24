using System;
using System.Collections;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// A view into a buffer generally representing a subset of the buffer.
    /// </summary>
    [System.Serializable]
    public class GLTFBufferView : GLTFChildOfRootProperty
    {
        /// <summary>
        /// The index of the buffer.
        /// </summary>
        public GLTFBufferId buffer;

        /// <summary>
        /// The offset into the buffer in bytes.
        /// <minimum>0</minimum>
        /// </summary>
        public int byteOffset;

        /// <summary>
        /// The length of the bufferView in bytes.
        /// <minimum>0</minimum>
        /// </summary>
        public int byteLength;

        /// <summary>
        /// The stride, in bytes, between vertex attributes or other interleavable data.
        /// When this is zero, data is tightly packed.
        /// <minimum>0</minimum>
        /// <maximum>255</maximum>
        /// </summary>
        public int byteStride = 0;

        /// <summary>
        /// The target that the WebGL buffer should be bound to.
        /// All valid values correspond to WebGL enums.
        /// When this is not provided, the bufferView contains animation or skin data.
        /// </summary>
        public GLTFBufferViewTarget target;

        private byte[] data;

        public byte[] Data
        {
            get
            {
                if (data != null)
                {
                    return data;
                }

                data = new byte[byteLength];

                byte[] source = buffer.Value.Data;

                if (byteStride != 0)
                {
                    for (int i = 0; i < byteLength; i++)
                    {
                        data[i] = source[byteOffset + (i * byteStride)];
                    }
                }
                else
                {
                    Buffer.BlockCopy(source, byteOffset, data, 0, byteLength);
                }
                
                
                return data;
            }
        }

        public static GLTFBufferView Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var bufferView = new GLTFBufferView();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "buffer":
                        bufferView.buffer = GLTFBufferId.Deserialize(root, reader);
                        break;
                    case "byteOffset":
                        bufferView.byteOffset = reader.ReadAsInt32().Value;
                        break;
                    case "byteLength":
                        bufferView.byteLength = reader.ReadAsInt32().Value;
                        break;
                    case "byteStride":
                        bufferView.byteStride = reader.ReadAsInt32().Value;
                        break;
                    case "target":
                        bufferView.target = (GLTFBufferViewTarget) reader.ReadAsInt32().Value;
                        break;
                    case "name":
                        bufferView.name = reader.ReadAsString();
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return bufferView;
        }
    }

    public enum GLTFBufferViewTarget
    {
        ARRAY_BUFFER = 34962,
        ELEMENT_ARRAY_BUFFER = 34963
    }
}
