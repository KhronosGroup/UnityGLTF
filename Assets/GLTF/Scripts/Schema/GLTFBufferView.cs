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
        public GLTFBufferViewTarget Target = GLTFBufferViewTarget.None;

        public static GLTFBufferView Deserialize(GLTFRoot root, JsonReader reader)
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
                        bufferView.Target = (GLTFBufferViewTarget)reader.ReadAsInt32().Value;
                        break;
                    default:
                        bufferView.DefaultPropertyDeserializer(root, reader);
                        break;
                }
            }

            return bufferView;
        }

        public override void Serialize(JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("buffer");
            writer.WriteValue(Buffer.Id);

            if (ByteOffset != 0)
            {
                writer.WritePropertyName("byteOffset");
                writer.WriteValue(ByteOffset);
            }

            writer.WritePropertyName("byteLength");
            writer.WriteValue(ByteLength);

            if (ByteStride != 0)
            {
                writer.WritePropertyName("byteStride");
                writer.WriteValue(ByteStride);
            }

            if (Target != GLTFBufferViewTarget.None)
            {
                writer.WritePropertyName("target");
                writer.WriteValue((int)Target);
            }

            base.Serialize(writer);

            writer.WriteEndObject();
        }
    }

    public enum GLTFBufferViewTarget
    {
        None = 0,
        ArrayBuffer = 34962,
        ElementArrayBuffer = 34963,
    }
}
