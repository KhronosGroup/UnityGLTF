using System;
using System.Collections.Generic;
using GLTF.JsonExtensions;
using Newtonsoft.Json;
using UnityEngine;

namespace GLTF
{
    [System.Serializable]
    public class GLTFAccessor : GLTFChildOfRootProperty
    {
        /// <summary>
        /// The index of the bufferView.
        /// If this is undefined, look in the sparse object for the index and value buffer views.
        /// </summary>
        public GLTFBufferViewId bufferView;

        /// <summary>
        /// The offset relative to the start of the bufferView in bytes.
        /// This must be a multiple of the size of the component datatype.
        /// <minimum>0</minimum>
        /// </summary>
        public int byteOffset = 0;

        /// <summary>
        /// The datatype of components in the attribute.
        /// All valid values correspond to WebGL enums.
        /// The corresponding typed arrays are: `Int8Array`, `Uint8Array`, `Int16Array`,
        /// `Uint16Array`, `Uint32Array`, and `Float32Array`, respectively. 
        /// 5125 (UNSIGNED_INT) is only allowed when the accessor contains indices
        /// i.e., the accessor is only referenced by `primitive.indices`.
        /// </summary>
        public GLTFComponentType componentType;

        /// <summary>
        /// Specifies whether integer data values should be normalized
        /// (`true`) to [0, 1] (for unsigned types) or [-1, 1] (for signed types),
        /// or converted directly (`false`) when they are accessed.
        /// Must be `false` when accessor is used for animation data.
        /// </summary>
        public bool normalized = false;

        /// <summary>
        /// The number of attributes referenced by this accessor, not to be confused
        /// with the number of bytes or number of components.
        /// <minimum>1</minimum>
        /// </summary>
        public int count;

        /// <summary>
        /// Specifies if the attribute is a scalar, vector, or matrix,
        /// and the number of elements in the vector or matrix.
        /// </summary>
        public GLTFAccessorAttributeType type;

        /// <summary>
        /// Maximum value of each component in this attribute.
        /// Both min and max arrays have the same length.
        /// The length is determined by the value of the type property;
        /// it can be 1, 2, 3, 4, 9, or 16.
        /// 
        /// When `componentType` is `5126` (FLOAT) each array value must be stored as
        /// double-precision JSON number with numerical value which is equal to
        /// buffer-stored single-precision value to avoid extra runtime conversions.
        /// 
        /// `normalized` property has no effect on array values: they always correspond
        /// to the actual values stored in the buffer. When accessor is sparse, this
        /// property must contain max values of accessor data with sparse substitution
        /// applied.
        /// <minItems>1</minItems>
        /// <maxItems>16</maxItems>
        /// </summary>
        public List<double> max;

        /// <summary>
        /// Minimum value of each component in this attribute.
        /// Both min and max arrays have the same length.  The length is determined by
        /// the value of the type property; it can be 1, 2, 3, 4, 9, or 16.
        /// 
        /// When `componentType` is `5126` (FLOAT) each array value must be stored as
        /// double-precision JSON number with numerical value which is equal to
        /// buffer-stored single-precision value to avoid extra runtime conversions.
        /// 
        /// `normalized` property has no effect on array values: they always correspond
        /// to the actual values stored in the buffer. When accessor is sparse, this
        /// property must contain min values of accessor data with sparse substitution
        /// applied.
        /// <minItems>1</minItems>
        /// <maxItems>16</maxItems>
        /// </summary>
        public List<double> min;

        /// <summary>
        /// Sparse storage of attributes that deviate from their initialization value.
        /// </summary>
        public GLTFAccessorSparse sparse;

        public static GLTFAccessor Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var accessor = new GLTFAccessor();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "bufferView":
                        accessor.bufferView = GLTFBufferViewId.Deserialize(root, reader);
                        break;
                    case "byteOffset":
                        accessor.byteOffset = reader.ReadAsInt32().Value;
                        break;
                    case "componentType":
                        accessor.componentType = (GLTFComponentType) reader.ReadAsInt32().Value;
                        break;
                    case "normalized":
                        accessor.normalized = reader.ReadAsBoolean().Value;
                        break;
                    case "count":
                        accessor.count = reader.ReadAsInt32().Value;
                        break;
                    case "type":
                        accessor.type = reader.ReadStringEnum<GLTFAccessorAttributeType>();
                        break;
                    case "max":
                        accessor.max = reader.ReadDoubleList();
                        break;
                    case "min":
                        accessor.min = reader.ReadDoubleList();
                        break;
                    case "sparse":
                        // TODO: Implement Deserialization of Sparse Arrays
                        break;
                    case "name":
                        accessor.name = reader.ReadAsString();
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return accessor;
        }

        public int[] AsIntArray()
        {
            int[] arr = new int[count];
            int totalByteOffset = bufferView.Value.byteOffset + byteOffset;
            int stride = 0;
            byte[] bytes = bufferView.Value.buffer.Value.Data;

            switch (componentType)
            {
                case GLTFComponentType.BYTE:
                    stride = bufferView.Value.byteStride + sizeof(sbyte);
                    for (int idx = 0; idx < count; idx++)
                    {
                        arr[idx] = (sbyte)bytes[totalByteOffset + (idx * stride)];
                    }
                    break;
                case GLTFComponentType.UNSIGNED_BYTE:
                    stride = bufferView.Value.byteStride + sizeof(byte);
                    for (int idx = 0; idx < count; idx++)
                    {
                        arr[idx] = bytes[totalByteOffset + (idx * stride)];
                    }
                    break;
                case GLTFComponentType.SHORT:
                    stride = bufferView.Value.byteStride + sizeof(Int16);
                    for (int idx = 0; idx < count; idx++)
                    {
                        arr[idx] = BitConverter.ToInt16(bytes, totalByteOffset + (idx * stride));
                    }
                    break;
                case GLTFComponentType.UNSIGNED_SHORT:
                    if (bufferView.Value.byteStride == 0)
                    {
                        UInt16[] intermediateArr = new UInt16[count];
                        Buffer.BlockCopy(bytes, totalByteOffset, intermediateArr, 0, count * sizeof(UInt16));
                        for (int idx = 0; idx < count; idx++)
                        {
                            arr[idx] = (int)intermediateArr[idx];
                        }
                    }
                    else
                    {
                        stride = bufferView.Value.byteStride + sizeof(UInt16);
                        for (int idx = 0; idx < count; idx++)
                        {
                            arr[idx] = BitConverter.ToUInt16(bytes, totalByteOffset + (idx * stride));
                        }
                    }
                    break;
                case GLTFComponentType.FLOAT:
                    if (bufferView.Value.byteStride == 0)
                    {
                        int[] intermediateArr = new int[count];
                        Buffer.BlockCopy(bytes, totalByteOffset, intermediateArr, 0, count * sizeof(int));
                        for (int idx = 0; idx < count; idx++)
                        {
                            arr[idx] = intermediateArr[idx];
                        }
                    }
                    else
                    {
                        stride = bufferView.Value.byteStride + sizeof(sbyte);
                        for (int idx = 0; idx < count; idx++)
                        {
                            arr[idx] = (int)BitConverter.ToSingle(bytes, totalByteOffset + (idx * stride));
                        }
                    }
                    break;
            }

            return arr;
        }

        public Vector2[] AsVector2Array()
        {
            Vector2[] arr = new Vector2[count];
            int totalByteOffset = bufferView.Value.byteOffset + byteOffset;
            int stride = 0;
            byte[] bytes = bufferView.Value.buffer.Value.Data;
            const int numComponents = 2;

            switch (componentType)
            {
                case GLTFComponentType.BYTE:
                    stride = numComponents * sizeof(sbyte) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        float x = (sbyte)bytes[totalByteOffset + (idx * stride)];
                        float y = (sbyte)bytes[totalByteOffset + (idx * stride) + sizeof(sbyte)];
                        arr[idx] = new Vector2(x, y);
                    }
                    break;
                case GLTFComponentType.UNSIGNED_BYTE:
                    stride = numComponents * sizeof(byte) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        float x = bytes[totalByteOffset + (idx * stride)];
                        float y = bytes[totalByteOffset + (idx * stride) + sizeof(byte)];
                        arr[idx] = new Vector2(x, y);
                    }
                    break;
                case GLTFComponentType.SHORT:
                    stride = numComponents * sizeof(Int16) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        float x = BitConverter.ToInt16(bytes, totalByteOffset + (idx * stride));
                        float y = BitConverter.ToInt16(bytes, totalByteOffset + (idx * stride) + sizeof(Int16));
                        arr[idx] = new Vector2(x, y);
                    }
                    break;
                case GLTFComponentType.UNSIGNED_SHORT:
                    stride = numComponents * sizeof(UInt16) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        float x = BitConverter.ToUInt16(bytes, totalByteOffset + (idx * stride));
                        float y = BitConverter.ToUInt16(bytes, totalByteOffset + (idx * stride) + sizeof(UInt16));
                        arr[idx] = new Vector2(x, y);
                    }
                    break;
                case GLTFComponentType.UNSIGNED_INT:
                    stride = numComponents * sizeof(UInt32) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        float x = BitConverter.ToUInt32(bytes, totalByteOffset + (idx * stride));
                        float y = BitConverter.ToUInt32(bytes, totalByteOffset + (idx * stride) + sizeof(UInt32));
                        arr[idx] = new Vector2(x, y);
                    }
                    break;
                case GLTFComponentType.FLOAT:
                    if (bufferView.Value.byteStride == 0)
                    {
                        int totalComponents = count * 2;
                        float[] intermediateArr = new float[totalComponents];
                        Buffer.BlockCopy(bytes, totalByteOffset, intermediateArr, 0, totalComponents * sizeof(float));
                        for (int idx = 0; idx < count; idx++)
                        {
                            arr[idx] = new Vector2(
                                intermediateArr[idx * 2],
                                intermediateArr[idx * 2 + 1]
                            );
                        }
                    }
                    else
                    {
                        stride = numComponents * sizeof(float) + bufferView.Value.byteStride;
                        for (int idx = 0; idx < count; idx++)
                        {
                            float x = BitConverter.ToSingle(bytes, totalByteOffset + (idx * stride));
                            float y = BitConverter.ToSingle(bytes, totalByteOffset + (idx * stride) + sizeof(float));
                            arr[idx] = new Vector2(x, y);
                        }
                    }
                    break;
            }

            return arr;
        }

        public Vector3[] AsVector3Array()
        {
            Vector3[] arr = new Vector3[count];
            int totalByteOffset = bufferView.Value.byteOffset + byteOffset;
            int stride = 0;
            byte[] bytes = bufferView.Value.buffer.Value.Data;
            const int numComponents = 3;

            switch (componentType)
            {
                case GLTFComponentType.BYTE:
                    stride = numComponents * sizeof(sbyte) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        float x = (sbyte)bytes[totalByteOffset + (idx * stride)];
                        float y = (sbyte)bytes[totalByteOffset + (idx * stride) + sizeof(sbyte)];
                        float z = (sbyte)bytes[totalByteOffset + (idx * stride) + (2 * sizeof(sbyte))];
                        arr[idx] = new Vector3(x, y, z);
                    }
                    break;
                case GLTFComponentType.UNSIGNED_BYTE:
                    stride = numComponents * sizeof(byte) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        float x = bytes[totalByteOffset + (idx * stride)];
                        float y = bytes[totalByteOffset + (idx * stride) + sizeof(byte)];
                        float z = bytes[totalByteOffset + (idx * stride) + (2 * sizeof(byte))];
                        arr[idx] = new Vector3(x, y, z);
                    }
                    break;
                case GLTFComponentType.SHORT:
                    stride = numComponents * sizeof(Int16) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        float x = BitConverter.ToInt16(bytes, totalByteOffset + (idx * stride));
                        float y = BitConverter.ToInt16(bytes, totalByteOffset + (idx * stride) + sizeof(Int16));
                        float z = BitConverter.ToInt16(bytes, totalByteOffset + (idx * stride) + (2 * sizeof(Int16)));
                        arr[idx] = new Vector3(x, y, z);
                    }
                    break;
                case GLTFComponentType.UNSIGNED_SHORT:
                    stride = numComponents * sizeof(UInt16) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        float x = BitConverter.ToUInt16(bytes, totalByteOffset + (idx * stride));
                        float y = BitConverter.ToUInt16(bytes, totalByteOffset + (idx * stride) + sizeof(UInt16));
                        float z = BitConverter.ToUInt16(bytes, totalByteOffset + (idx * stride) + (2 * sizeof(UInt16)));
                        arr[idx] = new Vector3(x, y, z);
                    }
                    break;
                case GLTFComponentType.UNSIGNED_INT:
                    stride = numComponents * sizeof(UInt32) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        float x = BitConverter.ToUInt32(bytes, totalByteOffset + (idx * stride));
                        float y = BitConverter.ToUInt32(bytes, totalByteOffset + (idx * stride) + sizeof(UInt32));
                        float z = BitConverter.ToUInt32(bytes, totalByteOffset + (idx * stride) + (2 * sizeof(UInt32)));
                        arr[idx] = new Vector3(x, y, z);
                    }
                    break;
                case GLTFComponentType.FLOAT:
                    if (bufferView.Value.byteStride == 0)
                    {
                        int totalComponents = count * 3;
                        float[] intermediateArr = new float[totalComponents];
                        Buffer.BlockCopy(bytes, totalByteOffset, intermediateArr, 0, totalComponents * sizeof(float));
                        for (int idx = 0; idx < count; idx++)
                        {
                            arr[idx] = new Vector3(
                               intermediateArr[idx * 3],
                               intermediateArr[idx * 3 + 1],
                               intermediateArr[idx * 3 + 2]
                            );
                        }
                    }
                    else
                    {
                        stride = numComponents * sizeof(float) + bufferView.Value.byteStride;
                        for (int idx = 0; idx < count; idx++)
                        {
                            float x = BitConverter.ToSingle(bytes, totalByteOffset + (idx * stride));
                            float y = BitConverter.ToSingle(bytes, totalByteOffset + (idx * stride) + sizeof(float));
                            float z = BitConverter.ToSingle(bytes,
                                totalByteOffset + (idx * stride) + (2 * sizeof(float)));
                            arr[idx] = new Vector3(x, y, z);
                        }
                    }
                    break;
            }

            return arr;
        }

        public Color[] AsColorArray()
        {
            Color[] arr = new Color[count];
            int totalByteOffset = bufferView.Value.byteOffset + byteOffset;
            int stride = 0;
            byte[] bytes = bufferView.Value.buffer.Value.Data;
            const int numComponents = 4;

            switch (componentType)
            {
                case GLTFComponentType.BYTE:
                    stride = numComponents * sizeof(sbyte) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        int r = (sbyte)bytes[totalByteOffset + (idx * stride)];
                        int g = (sbyte)bytes[totalByteOffset + (idx * stride) + sizeof(sbyte)];
                        int b = (sbyte)bytes[totalByteOffset + (idx * stride) + (2 * sizeof(sbyte))];
                        int a = (sbyte)bytes[totalByteOffset + (idx * stride) + (3 * sizeof(sbyte))];
                        arr[idx] = new Color(r, g, b, a);
                    }
                    break;
                case GLTFComponentType.UNSIGNED_BYTE:
                    stride = numComponents * sizeof(byte) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        int r = bytes[totalByteOffset + (idx * stride)];
                        int g = bytes[totalByteOffset + (idx * stride) + sizeof(byte)];
                        int b = bytes[totalByteOffset + (idx * stride) + (2 * sizeof(byte))];
                        int a = bytes[totalByteOffset + (idx * stride) + (3 * sizeof(byte))];
                        arr[idx] = new Color(r, g, b, a);
                    }
                    break;
                case GLTFComponentType.SHORT:
                    stride = numComponents * sizeof(Int16) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        int r = BitConverter.ToInt16(bytes, totalByteOffset + (idx * stride));
                        int g = BitConverter.ToInt16(bytes, totalByteOffset + (idx * stride) + sizeof(Int16));
                        int b = BitConverter.ToInt16(bytes, totalByteOffset + (idx * stride) + (2 * sizeof(Int16)));
                        int a = BitConverter.ToInt16(bytes, totalByteOffset + (idx * stride) + (3 * sizeof(Int16)));
                        arr[idx] = new Color(r, g, b, a);
                    }
                    break;
                case GLTFComponentType.UNSIGNED_SHORT:
                    stride = numComponents * sizeof(UInt16) + bufferView.Value.byteStride;
                    for (int idx = 0; idx < count; idx++)
                    {
                        int r = BitConverter.ToUInt16(bytes, totalByteOffset + (idx * stride));
                        int g = BitConverter.ToUInt16(bytes, totalByteOffset + (idx * stride) + sizeof(UInt16));
                        int b = BitConverter.ToUInt16(bytes, totalByteOffset + (idx * stride) + (2 * sizeof(UInt16)));
                        int a = BitConverter.ToUInt16(bytes, totalByteOffset + (idx * stride) + (3 * sizeof(UInt16)));
                        arr[idx] = new Color(r, g, b, a);
                    }
                    break;
            }

            return arr;
        }
    }

    [System.Serializable]
    public class GLTFAccessorSparse
    {
        /// <summary>
        /// Number of entries stored in the sparse array.
        /// <minimum>1</minimum>
        /// </summary>
        public int count;

        /// <summary>
        /// Index array of size `count` that points to those accessor attributes that
        /// deviate from their initialization value. Indices must strictly increase.
        /// </summary>
        public GLTFAccessorSparseIndices indices;

        /// <summary>
        /// "Array of size `count` times number of components, storing the displaced
        /// accessor attributes pointed by `indices`. Substituted values must have
        /// the same `componentType` and number of components as the base accessor.
        /// </summary>
        public GLTFAccessorSparseValues values;
    }

    [System.Serializable]
    public class GLTFAccessorSparseIndices
    {
        /// <summary>
        /// The index of the bufferView with sparse indices.
        /// Referenced bufferView can't have ARRAY_BUFFER or ELEMENT_ARRAY_BUFFER target.
        /// </summary>
        public GLTFBufferViewId bufferView;

        /// <summary>
        /// The offset relative to the start of the bufferView in bytes. Must be aligned.
        /// <minimum>0</minimum>
        /// </summary>
        public int byteOffset = 0;

        /// <summary>
        /// The indices data type. Valid values correspond to WebGL enums:
        /// `5121` (UNSIGNED_BYTE)
        /// `5123` (UNSIGNED_SHORT)
        /// `5125` (UNSIGNED_INT)
        /// </summary>
        public GLTFComponentType componentType;
    }

    [System.Serializable]
    public class GLTFAccessorSparseValues
    {
        /// <summary>
        /// The index of the bufferView with sparse values.
        /// Referenced bufferView can't have ARRAY_BUFFER or ELEMENT_ARRAY_BUFFER target.
        /// </summary>
        private GLTFBufferViewId bufferView;

        /// <summary>
        /// The offset relative to the start of the bufferView in bytes. Must be aligned.
        /// <minimum>0</minimum>
        /// </summary>
        public int byteOffset = 0;
    }

    public enum GLTFComponentType
    {
        BYTE = 5120,
        UNSIGNED_BYTE = 5121,
        SHORT = 5122,
        UNSIGNED_SHORT = 5123,
        UNSIGNED_INT = 5125,
        FLOAT = 5126
    }

    public enum GLTFAccessorAttributeType
    {
        SCALAR,
        VEC2,
        VEC3,
        VEC4,
        MAT2,
        MAT3,
        MAT4
    }
}
