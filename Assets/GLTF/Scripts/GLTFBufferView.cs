using System;
using GLTF.JsonExtensions;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// A view into a buffer generally representing a subset of the buffer.
    /// </summary>
    public class GLTFBufferView : GLTFChildOfRootProperty
    {
        /// <summary>
        /// The index of the buffer.
        /// </summary>
        public GLTFBufferId Buffer;

        /// <summary>
        /// The offset into the buffer in bytes.
        /// <minimum>0</minimum>
        /// </summary>
        public int ByteOffset;

        /// <summary>
        /// The length of the bufferView in bytes.
        /// <minimum>0</minimum>
        /// </summary>
        public int ByteLength;

        /// <summary>
        /// The stride, in bytes, between vertex attributes or other interleavable data.
        /// When this is zero, data is tightly packed.
        /// <minimum>0</minimum>
        /// <maximum>255</maximum>
        /// </summary>
        public int ByteStride;

        /// <summary>
        /// The target that the WebGL buffer should be bound to.
        /// All valid values correspond to WebGL enums.
        /// When this is not provided, the bufferView contains animation or skin data.
        /// </summary>
        public GLTFBufferViewTarget Target;

        public static GLTFBufferView Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var bufferView = new GLTFBufferView();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "buffer":
                        bufferView.Buffer = GLTFBufferId.Deserialize(root, reader);
                        break;
                    case "byteOffset":
                        bufferView.ByteOffset = reader.ReadAsInt32().Value;
                        break;
                    case "byteLength":
                        bufferView.ByteLength = reader.ReadAsInt32().Value;
                        break;
                    case "byteStride":
                        bufferView.ByteStride = reader.ReadAsInt32().Value;
                        break;
                    case "target":
                        bufferView.Target = (GLTFBufferViewTarget) reader.ReadAsInt32().Value;
                        break;
                    case "name":
                        bufferView.Name = reader.ReadAsString();
                        break;
					case "extensions":
		                bufferView.Extensions = reader.ReadAsObjectDictionary();
		                break;
	                case "extras":
		                bufferView.Extras = reader.ReadAsObjectDictionary();
		                break;
	                default:
		                throw new Exception("Unexpected property.");
				}
            }

            return bufferView;
        }
    }

    public enum GLTFBufferViewTarget
    {
        ArrayBuffer = 34962,
        ElementArrayBuffer = 34963
    }
}
