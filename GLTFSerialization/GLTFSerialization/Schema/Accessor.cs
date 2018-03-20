using System;
using System.Collections.Generic;
using GLTF.Extensions;
using GLTF.Math;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	public class Accessor : GLTFChildOfRootProperty
	{
		/// <summary>
		/// The index of the bufferView.
		/// If this is undefined, look in the sparse object for the index and value buffer views.
		/// </summary>
		public BufferViewId BufferView;

		/// <summary>
		/// The offset relative to the start of the bufferView in bytes.
		/// This must be a multiple of the size of the component datatype.
		/// <minimum>0</minimum>
		/// </summary>
		public int ByteOffset;

		/// <summary>
		/// The datatype of components in the attribute.
		/// All valid values correspond to WebGL enums.
		/// The corresponding typed arrays are: `Int8Array`, `Uint8Array`, `Int16Array`,
		/// `Uint16Array`, `Uint32Array`, and `Float32Array`, respectively.
		/// 5125 (UNSIGNED_INT) is only allowed when the accessor contains indices
		/// i.e., the accessor is only referenced by `primitive.indices`.
		/// </summary>
		public GLTFComponentType ComponentType;

		/// <summary>
		/// Specifies whether integer data values should be normalized
		/// (`true`) to [0, 1] (for unsigned types) or [-1, 1] (for signed types),
		/// or converted directly (`false`) when they are accessed.
		/// Must be `false` when accessor is used for animation data.
		/// </summary>
		public bool Normalized;

		/// <summary>
		/// The number of attributes referenced by this accessor, not to be confused
		/// with the number of bytes or number of components.
		/// <minimum>1</minimum>
		/// </summary>
		public int Count;

		/// <summary>
		/// Specifies if the attribute is a scalar, vector, or matrix,
		/// and the number of elements in the vector or matrix.
		/// </summary>
		public GLTFAccessorAttributeType Type;

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
		public List<double> Max;

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
		public List<double> Min;

		/// <summary>
		/// Sparse storage of attributes that deviate from their initialization value.
		/// </summary>
		public AccessorSparse Sparse;

		public static Accessor Deserialize(GLTFRoot root, JsonReader reader)
		{
			var accessor = new Accessor();

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "bufferView":
						accessor.BufferView = BufferViewId.Deserialize(root, reader);
						break;
					case "byteOffset":
						accessor.ByteOffset = reader.ReadAsInt32().Value;
						break;
					case "componentType":
						accessor.ComponentType = (GLTFComponentType)reader.ReadAsInt32().Value;
						break;
					case "normalized":
						accessor.Normalized = reader.ReadAsBoolean().Value;
						break;
					case "count":
						accessor.Count = reader.ReadAsInt32().Value;
						break;
					case "type":
						accessor.Type = reader.ReadStringEnum<GLTFAccessorAttributeType>();
						break;
					case "max":
						accessor.Max = reader.ReadDoubleList();
						break;
					case "min":
						accessor.Min = reader.ReadDoubleList();
						break;
					case "sparse":
						accessor.Sparse = AccessorSparse.Deserialize(root, reader);
						break;
					default:
						accessor.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return accessor;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			if (BufferView != null)
			{
				writer.WritePropertyName("bufferView");
				writer.WriteValue(BufferView.Id);
			}

			if (ByteOffset != 0)
			{
				writer.WritePropertyName("byteOffset");
				writer.WriteValue(ByteOffset);
			}

			writer.WritePropertyName("componentType");
			writer.WriteValue(ComponentType);

			if (Normalized != false)
			{
				writer.WritePropertyName("normalized");
				writer.WriteValue(true);
			}

			writer.WritePropertyName("count");
			writer.WriteValue(Count);

			writer.WritePropertyName("type");
			writer.WriteValue(Type.ToString());

			bool isMaxNull = Max == null;
			bool isMinNull = Min == null;

			if (!isMaxNull)
			{
				writer.WritePropertyName("max");
				writer.WriteStartArray();
				foreach (var item in Max)
				{
					writer.WriteValue(item);
				}
				writer.WriteEndArray();
			}

			if(!isMinNull)
			{
				writer.WritePropertyName("min");
				writer.WriteStartArray();
				foreach (var item in Min)
				{
					writer.WriteValue(item);
				}
				writer.WriteEndArray();
			}

			if (Sparse != null)
			{
				if(isMinNull || isMaxNull)
				{
					throw new JsonSerializationException("Min and max attribute cannot be null when attribute is sparse");
				}

				writer.WritePropertyName("sparse");
				Sparse.Serialize(writer);
			}

			base.Serialize(writer);

			writer.WriteEndObject();
		}

		private static unsafe int GetByteElement(byte[] buffer, int byteOffset)
		{
			fixed (byte* offsetBuffer = &buffer[byteOffset])
			{
				return *((sbyte*)offsetBuffer);
			}
		}

		private static unsafe int GetUByteElement(byte[] buffer, int byteOffset)
		{
			fixed (byte* offsetBuffer = &buffer[byteOffset])
			{
				return *((byte*)offsetBuffer);
			}
		}

		private static unsafe int GetShortElement(byte[] buffer, int byteOffset)
		{
			fixed (byte* offsetBuffer = &buffer[byteOffset])
			{
				return *((short*)offsetBuffer);
			}
		}

		private static unsafe int GetUShortElement(byte[] buffer, int byteOffset)
		{
			fixed (byte* offsetBuffer = &buffer[byteOffset])
			{
				return *((ushort*)offsetBuffer);
			}
		}

		private static unsafe int GetUIntElement(byte[] buffer, int byteOffset)
		{
			fixed (byte* offsetBuffer = &buffer[byteOffset])
			{
				return (int) *((uint*)offsetBuffer);
			}
		}

		private static unsafe float GetFloatElement(byte[] buffer, int byteOffset)
		{
			fixed (byte* offsetBuffer = &buffer[byteOffset])
			{
				return *((float*)offsetBuffer);
			}
		}

		private static void GetTypeDetails(GLTFComponentType type, out int componentSize, out float maxValue,
			out Func<byte[], int, int> discreteFunc, out Func<byte[], int, float> continuousFunc)
		{
			componentSize = 1;
			maxValue = byte.MaxValue;
			discreteFunc = GetUByteElement;
			continuousFunc = GetFloatElement;

			switch (type)
			{
				case GLTFComponentType.Byte:
					discreteFunc = GetByteElement;
					componentSize = sizeof(sbyte);
					maxValue = sbyte.MaxValue;
					break;
				case GLTFComponentType.UnsignedByte:
					discreteFunc = GetUByteElement;
					componentSize = sizeof(byte);
					maxValue = byte.MaxValue;
					break;
				case GLTFComponentType.Short:
					discreteFunc = GetShortElement;
					componentSize = sizeof(short);
					maxValue = short.MaxValue;
					break;
				case GLTFComponentType.UnsignedShort:
					discreteFunc = GetUShortElement;
					componentSize = sizeof(ushort);
					maxValue = ushort.MaxValue;
					break;
				case GLTFComponentType.UnsignedInt:
					discreteFunc = GetUIntElement;
					componentSize = sizeof(uint);
					maxValue = uint.MaxValue;
					break;
				case GLTFComponentType.Float:
					continuousFunc = GetFloatElement;
					componentSize = sizeof(float);
					maxValue = float.MaxValue;
					break;
				default:
					throw new Exception("Unsupported component type.");
			}
		}

		public int[] AsIntArray(ref NumericArray contents, byte[] bufferData)
		{
			if (contents.AsInts != null) return contents.AsInts;

			if (Type != GLTFAccessorAttributeType.SCALAR) return null;

			var arr = new int[Count];
			var totalByteOffset = BufferView.Value.ByteOffset + ByteOffset;

			int componentSize;
			float maxValue;
			Func<byte[], int, int> getDiscreteElement;
			Func<byte[], int, float> getContinuousElement;
			GetTypeDetails(ComponentType, out componentSize, out maxValue, out getDiscreteElement, out getContinuousElement);

			var stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize;

			for (var idx = 0; idx < Count; idx++)
			{
				if (ComponentType == GLTFComponentType.Float)
					arr[idx] = (int)System.Math.Floor(getContinuousElement(bufferData, totalByteOffset + idx * stride));
				else
					arr[idx] = getDiscreteElement(bufferData, totalByteOffset + idx * stride);
			}

			contents.AsInts = arr;
			return arr;
		}

		public float[] AsFloatArray(ref NumericArray contents, byte[] bufferData)
		{
			if (contents.AsFloats != null) return contents.AsFloats;

			if (Type != GLTFAccessorAttributeType.SCALAR) return null;

			var arr = new float[Count];
			var totalByteOffset = BufferView.Value.ByteOffset + ByteOffset;

			int componentSize;
			float maxValue;
			Func<byte[], int, int> getDiscreteElement;
			Func<byte[], int, float> getContinuousElement;
			GetTypeDetails(ComponentType, out componentSize, out maxValue, out getDiscreteElement, out getContinuousElement);

			var stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize;

			for (var idx = 0; idx < Count; idx++)
			{
				arr[idx] = getContinuousElement(bufferData, totalByteOffset + idx * stride);
			}

			contents.AsFloats = arr;
			return arr;
		}

		public Vector2[] AsVector2Array(ref NumericArray contents, byte[] bufferData, bool normalizeIntValues = true)
		{
			if (contents.AsVec2s != null) return contents.AsVec2s;

			if (Type != GLTFAccessorAttributeType.VEC2) return null;

			var arr = new Vector2[Count];
			var totalByteOffset = BufferView.Value.ByteOffset + ByteOffset;

			int componentSize;
			float maxValue;
			Func<byte[], int, int> getDiscreteElement;
			Func<byte[], int, float> getContinuousElement;
			GetTypeDetails(ComponentType, out componentSize, out maxValue, out getDiscreteElement, out getContinuousElement);

			var stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 2;
			if (normalizeIntValues) maxValue = 1;

			for (var idx = 0; idx < Count; idx++)
			{
				if (ComponentType == GLTFComponentType.Float)
				{
					arr[idx].X = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 0);
					arr[idx].Y = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 1);
				}
				else
				{
					arr[idx].X = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 0) / maxValue;
					arr[idx].Y = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 1) / maxValue;
				}
			}

			contents.AsVec2s = arr;
			return arr;
		}

		public Vector3[] AsVector3Array(ref NumericArray contents, byte[] bufferData, bool normalizeIntValues = true)
		{
			if (contents.AsVec3s != null) return contents.AsVec3s;

			if (Type != GLTFAccessorAttributeType.VEC3) return null;

			var arr = new Vector3[Count];
			var totalByteOffset = BufferView.Value.ByteOffset + ByteOffset;

			int componentSize;
			float maxValue;
			Func<byte[], int, int> getDiscreteElement;
			Func<byte[], int, float> getContinuousElement;
			GetTypeDetails(ComponentType, out componentSize, out maxValue, out getDiscreteElement, out getContinuousElement);

			var stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 3;
			if (normalizeIntValues) maxValue = 1;

			for (var idx = 0; idx < Count; idx++)
			{
				if (ComponentType == GLTFComponentType.Float)
				{
					arr[idx].X = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 0);
					arr[idx].Y = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 1);
					arr[idx].Z = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 2);
				}
				else
				{
					arr[idx].X = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 0) / maxValue;
					arr[idx].Y = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 1) / maxValue;
					arr[idx].Z = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 2) / maxValue;
				}
			}

			contents.AsVec3s = arr;
			return arr;
		}

		public Vector4[] AsVector4Array(ref NumericArray contents, byte[] bufferData, bool normalizeIntValues = true)
		{
			if (contents.AsVec4s != null) return contents.AsVec4s;

			if (Type != GLTFAccessorAttributeType.VEC4) return null;

			var arr = new Vector4[Count];
			var totalByteOffset = BufferView.Value.ByteOffset + ByteOffset;

			int componentSize;
			float maxValue;
			Func<byte[], int, int> getDiscreteElement;
			Func<byte[], int, float> getContinuousElement;
			GetTypeDetails(ComponentType, out componentSize, out maxValue, out getDiscreteElement, out getContinuousElement);

			var stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 4;
			if (normalizeIntValues) maxValue = 1;

			for (var idx = 0; idx < Count; idx++)
			{
				if (ComponentType == GLTFComponentType.Float)
				{
					arr[idx].X = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 0);
					arr[idx].Y = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 1);
					arr[idx].Z = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 2);
					arr[idx].W = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 3);
				}
				else
				{
					arr[idx].X = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 0) / maxValue;
					arr[idx].Y = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 1) / maxValue;
					arr[idx].Z = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 2) / maxValue;
					arr[idx].W = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 3) / maxValue;
				}
			}

			contents.AsVec4s = arr;
			return arr;
		}

		public Matrix4x4[] AsMatrixArray(ref NumericArray contents, byte[] bufferData)
		{
			if (contents.AsMatrix4x4s != null) return contents.AsMatrix4x4s;

			if (Type != GLTFAccessorAttributeType.MAT4) return null;

			var arr = new Matrix4x4[Count];
			var totalByteOffset = BufferView.Value.ByteOffset + ByteOffset;

			int componentSize;
			float maxValue;
			Func<byte[], int, int> getDiscreteElement;
			Func<byte[], int, float> getContinuousElement;
			GetTypeDetails(ComponentType, out componentSize, out maxValue, out getDiscreteElement, out getContinuousElement);

			var stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 16;
			for (var i = 0; i < Count; i++)
			{
				int startElement = totalByteOffset + i * stride;
				if (ComponentType == GLTFComponentType.Float)
				{
					arr[i] = new Matrix4x4 (
						getContinuousElement(bufferData, startElement + componentSize * 0),
						getContinuousElement(bufferData, startElement + componentSize * 4),
						getContinuousElement(bufferData, startElement + componentSize * 8),
						getContinuousElement(bufferData, startElement + componentSize * 12),

						getContinuousElement(bufferData, startElement + componentSize * 1),
						getContinuousElement(bufferData, startElement + componentSize * 5),
						getContinuousElement(bufferData, startElement + componentSize * 9),
						getContinuousElement(bufferData, startElement + componentSize * 13),

						getContinuousElement(bufferData, startElement + componentSize * 2),
						getContinuousElement(bufferData, startElement + componentSize * 6),
						getContinuousElement(bufferData, startElement + componentSize * 10),
						getContinuousElement(bufferData, startElement + componentSize * 14),

						getContinuousElement(bufferData, startElement + componentSize * 3),
						getContinuousElement(bufferData, startElement + componentSize * 7),
						getContinuousElement(bufferData, startElement + componentSize * 11),
						getContinuousElement(bufferData, startElement + componentSize * 15)
					);
				}
			}

			contents.AsMatrix4x4s = arr;
			return arr;
		}

		public Color[] AsColorArray(ref NumericArray contents, byte[] bufferData)
		{
			if (contents.AsColors != null) return contents.AsColors;

			if (Type != GLTFAccessorAttributeType.VEC3 && Type != GLTFAccessorAttributeType.VEC4)
				return null;

			var arr = new Color[Count];
			var totalByteOffset = BufferView.Value.ByteOffset + ByteOffset;

			int componentSize;
			float maxValue;
			Func<byte[], int, int> getDiscreteElement;
			Func<byte[], int, float> getContinuousElement;
			GetTypeDetails(ComponentType, out componentSize, out maxValue, out getDiscreteElement, out getContinuousElement);

			var stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * (Type == GLTFAccessorAttributeType.VEC3 ? 3 : 4);

			for (var idx = 0; idx < Count; idx++)
			{
				if (ComponentType == GLTFComponentType.Float)
				{
					arr[idx].R = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 0);
					arr[idx].G = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 1);
					arr[idx].B = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 2);
					if (Type == GLTFAccessorAttributeType.VEC4)
						arr[idx].A = getContinuousElement(bufferData, totalByteOffset + idx * stride + componentSize * 3);
					else
						arr[idx].A = 1;
				}
				else
				{
					arr[idx].R = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 0) / maxValue;
					arr[idx].G = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 1) / maxValue;
					arr[idx].B = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 2) / maxValue;
					if (Type == GLTFAccessorAttributeType.VEC4)
						arr[idx].A = getDiscreteElement(bufferData, totalByteOffset + idx * stride + componentSize * 3) / maxValue;
					else
						arr[idx].A = 1;
				}
			}

			contents.AsColors = arr;
			return arr;
		}

		public Vector2[] AsTexcoordArray(ref NumericArray contents, byte[] bufferData)
		{
			if (contents.AsTexcoords != null) return contents.AsTexcoords;

			var arr = AsVector2Array(ref contents, bufferData);
/*			for (var i = 0; i < arr.Length; i++)
			{
				arr[i].Y = 1 - arr[i].Y;
			}
*/
			contents.AsTexcoords = arr;
			contents.AsVec2s = null;

			return arr;
		}

		public Vector3[] AsVertexArray(ref NumericArray contents, byte[] bufferData)
		{
			if (contents.AsVertices != null) return contents.AsVertices;

			var arr = AsVector3Array(ref contents, bufferData);
			for (var i = 0; i < arr.Length; i++)
			{
				arr[i].Z *= -1;
			}

			contents.AsVertices = arr;
			contents.AsVec3s = null;

			return arr;
		}

		public Vector3[] AsNormalArray(ref NumericArray contents, byte[] bufferData)
		{
			if (contents.AsNormals != null) return contents.AsNormals;

			var arr = AsVector3Array(ref contents, bufferData);
			for (var i = 0; i < arr.Length; i++)
			{
				arr[i].Z *= -1;
			}

			contents.AsNormals = arr;
			contents.AsVec3s = null;

			return arr;
		}

		public Vector4[] AsTangentArray(ref NumericArray contents, byte[] bufferData)
		{
			if (contents.AsTangents != null) return contents.AsTangents;

			var arr = AsVector4Array(ref contents, bufferData);
			for (var i = 0; i < arr.Length; i++)
			{
				arr[i].W *= -1;
			}

			contents.AsTangents = arr;
			contents.AsVec4s = null;

			return arr;
		}

		public int[] AsTriangles(ref NumericArray contents, byte[] bufferData)
		{
			if (contents.AsTriangles != null) return contents.AsTriangles;

			var arr = AsIntArray(ref contents, bufferData);
			for (var i = 0; i < arr.Length; i += 3)
			{
				var temp = arr[i];
				arr[i] = arr[i + 2];
				arr[i + 2] = temp;
			}

			contents.AsTriangles = arr;
			contents.AsInts = null;

			return arr;
		}
	}

	public enum GLTFComponentType
	{
		Byte = 5120,
		UnsignedByte = 5121,
		Short = 5122,
		UnsignedShort = 5123,
		UnsignedInt = 5125,
		Float = 5126
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

	// todo: this should be a union
	public struct NumericArray
	{
		public int[] AsInts;
		public float[] AsFloats;
		public Vector2[] AsVec2s;
		public Vector3[] AsVec3s;
		public Vector4[] AsVec4s;
		public Color[] AsColors;
		public Matrix4x4[] AsMatrix4x4s;
		public Vector2[] AsTexcoords;
		public Vector3[] AsVertices;
		public Vector3[] AsNormals;
		public Vector4[] AsTangents;
		public int[] AsTriangles;
	}
}
