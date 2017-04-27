using Newtonsoft.Json;

namespace GLTF
{
    public class GLTFAccessorSparseValues : GLTFProperty
    {
        /// <summary>
        /// The index of the bufferView with sparse values.
        /// Referenced bufferView can't have ARRAY_BUFFER or ELEMENT_ARRAY_BUFFER target.
        /// </summary>
        private GLTFBufferViewId BufferView;

        /// <summary>
        /// The offset relative to the start of the bufferView in bytes. Must be aligned.
        /// <minimum>0</minimum>
        /// </summary>
        public int ByteOffset = 0;

        public static GLTFAccessorSparseValues Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var values = new GLTFAccessorSparseValues();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "bufferView":
                        values.BufferView = GLTFBufferViewId.Deserialize(root, reader);
                        break;
                    case "byteOffset":
                        values.ByteOffset = reader.ReadAsInt32().Value;
                        break;
					default:
						values.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return values;
        }
    }
}
