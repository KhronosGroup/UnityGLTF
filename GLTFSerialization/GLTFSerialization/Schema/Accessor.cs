using System;
using System.Collections.Generic;
using System.Linq;
using GLTF.Extensions;
using GLTF.Math;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using GLTF.Utilities;

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
		public uint ByteOffset;

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
		public uint Count;

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

		public Accessor()
		{
		}

		public Accessor(Accessor accessor, GLTFRoot gltfRoot) : base(accessor, gltfRoot)
		{
			if (accessor == null)
			{
				return;
			}

			if (accessor.BufferView != null)
			{
				BufferView = new BufferViewId(accessor.BufferView, gltfRoot);
			}

			ByteOffset = accessor.ByteOffset;
			ComponentType = accessor.ComponentType;
			Normalized = accessor.Normalized;
			Count = accessor.Count;
			Type = accessor.Type;

			if (accessor.Max != null)
			{
				Max = accessor.Max.ToList();
			}

			if (accessor.Min != null)
			{
				Min = accessor.Min.ToList();
			}

			if (accessor.Sparse != null)
			{
				Sparse = new AccessorSparse(accessor.Sparse, gltfRoot);
			}
		}

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
						accessor.ByteOffset = reader.ReadDoubleAsUInt32();
						break;
					case "componentType":
						accessor.ComponentType = (GLTFComponentType)reader.ReadAsInt32().Value;
						break;
					case "normalized":
						accessor.Normalized = reader.ReadAsBoolean().Value;
						break;
					case "count":
						accessor.Count = reader.ReadDoubleAsUInt32();
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

			if (!isMinNull)
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
				if (isMinNull || isMaxNull)
				{
					throw new JsonSerializationException("Min and max attribute cannot be null when attribute is sparse");
				}

				writer.WritePropertyName("sparse");
				Sparse.Serialize(writer);
			}

			base.Serialize(writer);

			writer.WriteEndObject();
		}

		private static sbyte GetByteElement(byte[] buffer, uint byteOffset)
		{
			return Convert.ToSByte(GetUByteElement(buffer, byteOffset));
		}

		private static byte GetUByteElement(byte[] buffer, uint byteOffset)
		{
			return buffer[byteOffset]; // should only be byte size long
		}

		private static unsafe short GetShortElement(byte[] buffer, uint byteOffset)
		{
			fixed (byte* offsetBuffer = &buffer[byteOffset])
			{
				return *((short*)offsetBuffer);
			}
		}

		private static unsafe ushort GetUShortElement(byte[] buffer, uint byteOffset)
		{
			fixed (byte* offsetBuffer = &buffer[byteOffset])
			{
				return *((ushort*)offsetBuffer);
			}
		}

		private static unsafe uint GetUIntElement(byte[] buffer, uint byteOffset)
		{
			fixed (byte* offsetBuffer = &buffer[byteOffset])
			{
				return *((uint*)offsetBuffer);
			}
		}

		private static unsafe float GetFloatElement(byte[] buffer, uint byteOffset)
		{
			fixed (byte* offsetBuffer = &buffer[byteOffset])
			{
				return *((float*)offsetBuffer);
			}
		}

		private static void GetTypeDetails(
			GLTFComponentType type,
			out uint componentSize,
			out float maxValue)
		{
			componentSize = 1;
			maxValue = byte.MaxValue;

			switch (type)
			{
				case GLTFComponentType.Byte:
					componentSize = sizeof(sbyte);
					maxValue = sbyte.MaxValue;
					break;
				case GLTFComponentType.UnsignedByte:
					componentSize = sizeof(byte);
					maxValue = byte.MaxValue;
					break;
				case GLTFComponentType.Short:
					componentSize = sizeof(short);
					maxValue = short.MaxValue;
					break;
				case GLTFComponentType.UnsignedShort:
					componentSize = sizeof(ushort);
					maxValue = ushort.MaxValue;
					break;
				case GLTFComponentType.UnsignedInt:
					componentSize = sizeof(uint);
					maxValue = uint.MaxValue;
					break;
				case GLTFComponentType.Float:
					componentSize = sizeof(float);
					maxValue = float.MaxValue;
					break;
				default:
					throw new Exception("Unsupported component type.");
			}
		}

		public uint[] AsUIntArray(ref NumericArray contents, byte[] bufferViewData, uint offset)
		{
			if (contents.AsUInts != null)
			{
				return contents.AsUInts;
			}

			if (Type != GLTFAccessorAttributeType.SCALAR)
			{
				return null;
			}

			var arr = new uint[Count];
			var totalByteOffset = ByteOffset + offset;

			uint componentSize;
			float maxValue;
			GetTypeDetails(ComponentType, out componentSize, out maxValue);

			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize;

			for (uint idx = 0; idx < Count; idx++)
			{
				if (ComponentType == GLTFComponentType.Float)
					arr[idx] = (uint)System.Math.Floor(GetFloatElement(bufferViewData, totalByteOffset + idx * stride));
				else
					arr[idx] = GetUnsignedDiscreteElement(bufferViewData, totalByteOffset + idx * stride, ComponentType);
			}

			contents.AsUInts = arr;
			return arr;
		}

		public float[] AsFloatArray(ref NumericArray contents, byte[] bufferViewData, uint offset)
		{
			if (contents.AsUInts != null)
			{
				return contents.AsFloats;
			}

			if (Type != GLTFAccessorAttributeType.SCALAR)
			{
				return null;
			}

			var arr = new float[Count];
			uint totalByteOffset = ByteOffset + offset;

			uint componentSize;
			float maxValue;
			GetTypeDetails(ComponentType, out componentSize, out maxValue);

			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize;

			for (uint idx = 0; idx < Count; idx++)
			{
				if (ComponentType == GLTFComponentType.Float)
					arr[idx] = GetFloatElement(bufferViewData, totalByteOffset + idx * stride);
				else
					arr[idx] = GetUnsignedDiscreteElement(bufferViewData, totalByteOffset + idx * stride, ComponentType);
			}

			contents.AsFloats = arr;
			return arr;
		}

		public Vector2[] AsVector2Array(ref NumericArray contents, byte[] bufferViewData, uint offset, bool normalizeIntValues = true)
		{
			if (contents.AsVec2s != null)
			{
				return contents.AsVec2s;
			}

			if (Type != GLTFAccessorAttributeType.VEC2)
			{
				return null;
			}

			if (ComponentType == GLTFComponentType.UnsignedInt)
			{
				return null;
			}

			var arr = new Vector2[Count];
			var totalByteOffset = ByteOffset + offset;

			uint componentSize;
			float maxValue;
			GetTypeDetails(ComponentType, out componentSize, out maxValue);

			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 2;
			if (normalizeIntValues) maxValue = 1;

			for (uint idx = 0; idx < Count; idx++)
			{
				if (ComponentType == GLTFComponentType.Float)
				{
					arr[idx].X = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 0);
					arr[idx].Y = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 1);
				}
				else
				{
					arr[idx].X = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 0, ComponentType) / maxValue;
					arr[idx].Y = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 1, ComponentType) / maxValue;
				}
			}

			contents.AsVec2s = arr;
			return arr;
		}

		public Vector3[] AsVector3Array(ref NumericArray contents, byte[] bufferViewData, uint offset, bool normalizeIntValues = true)
		{
			if (contents.AsVec3s != null)
			{
				return contents.AsVec3s;
			}

			if (Type != GLTFAccessorAttributeType.VEC3)
			{
				return null;
			}

			var arr = new Vector3[Count];
			var totalByteOffset = ByteOffset + offset;

			uint componentSize;
			float maxValue;
			GetTypeDetails(ComponentType, out componentSize, out maxValue);

			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 3;
			if (normalizeIntValues) maxValue = 1;

			for (uint idx = 0; idx < Count; idx++)
			{
				if (ComponentType == GLTFComponentType.Float)
				{
					arr[idx].X = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 0);
					arr[idx].Y = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 1);
					arr[idx].Z = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 2);
				}
				else
				{
					arr[idx].X = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 0, ComponentType) / maxValue;
					arr[idx].Y = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 1, ComponentType) / maxValue;
					arr[idx].Z = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 2, ComponentType) / maxValue;
				}
			}

			contents.AsVec3s = arr;
			return arr;
		}

		public Vector4[] AsVector4Array(ref NumericArray contents, byte[] bufferViewData, uint offset, bool normalizeIntValues = true)
		{
			if (contents.AsVec4s != null)
			{
				return contents.AsVec4s;
			}

			if (Type != GLTFAccessorAttributeType.VEC4)
			{
				return null;
			}

			if (ComponentType == GLTFComponentType.UnsignedInt)
			{
				return null;
			}

			var arr = new Vector4[Count];
			var totalByteOffset = ByteOffset + offset;

			uint componentSize;
			float maxValue;
			GetTypeDetails(ComponentType, out componentSize, out maxValue);

			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 4;
			if (normalizeIntValues) maxValue = 1;

			for (uint idx = 0; idx < Count; idx++)
			{
				if (ComponentType == GLTFComponentType.Float)
				{
					arr[idx].X = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 0);
					arr[idx].Y = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 1);
					arr[idx].Z = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 2);
					arr[idx].W = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 3);
				}
				else
				{
					arr[idx].X = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 0, ComponentType) / maxValue;
					arr[idx].Y = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 1, ComponentType) / maxValue;
					arr[idx].Z = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 2, ComponentType) / maxValue;
					arr[idx].W = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 3, ComponentType) / maxValue;
				}
			}

			contents.AsVec4s = arr;
			return arr;
		}

		public Color[] AsColorArray(ref NumericArray contents, byte[] bufferViewData, uint offset)
		{
			if (contents.AsColors != null)
			{
				return contents.AsColors;
			}

			if (Type != GLTFAccessorAttributeType.VEC3 && Type != GLTFAccessorAttributeType.VEC4)
			{
				return null;
			}

			if (ComponentType == GLTFComponentType.UnsignedInt)
			{
				return null;
			}

			var arr = new Color[Count];
			var totalByteOffset = ByteOffset + offset;

			uint componentSize;
			float maxValue;
			GetTypeDetails(ComponentType, out componentSize, out maxValue);

			uint stride = (uint)(BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * (Type == GLTFAccessorAttributeType.VEC3 ? 3 : 4));

			for (uint idx = 0; idx < Count; idx++)
			{
				if (ComponentType == GLTFComponentType.Float)
				{
					arr[idx].R = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 0);
					arr[idx].G = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 1);
					arr[idx].B = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 2);
					if (Type == GLTFAccessorAttributeType.VEC4)
						arr[idx].A = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 3);
					else
						arr[idx].A = 1;
				}
				else
				{
					// todo: can be optimized to get these in a block...
					arr[idx].R = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 0, ComponentType) / maxValue;
					arr[idx].G = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 1, ComponentType) / maxValue;
					arr[idx].B = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 2, ComponentType) / maxValue;
					if (Type == GLTFAccessorAttributeType.VEC4)
						arr[idx].A = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * 3, ComponentType) / maxValue;
					else
						arr[idx].A = 1;
				}
			}

			contents.AsColors = arr;
			return arr;
		}

		public Vector2[] AsTexcoordArray(ref NumericArray contents, byte[] bufferViewData, uint offset)
		{
			if (contents.AsTexcoords != null)
			{
				return contents.AsTexcoords;
			}

			contents.AsTexcoords = AsVector2Array(ref contents, bufferViewData, offset);

			return contents.AsTexcoords;
		}

		public Vector3[] AsVertexArray(ref NumericArray contents, byte[] bufferViewData, uint offset)
		{
			if (contents.AsVertices != null)
			{
				return contents.AsVertices;
			}

			contents.AsVertices = AsVector3Array(ref contents, bufferViewData, offset);

			return contents.AsVertices;
		}

		public Vector3[] AsNormalArray(ref NumericArray contents, byte[] bufferViewData, uint offset)
		{
			if (contents.AsNormals != null)
			{
				return contents.AsNormals;
			}

			contents.AsNormals = AsVector3Array(ref contents, bufferViewData, offset);

			return contents.AsNormals;
		}

		public Vector4[] AsTangentArray(ref NumericArray contents, byte[] bufferViewData, uint offset)
		{
			if (contents.AsTangents != null)
			{
				return contents.AsTangents;
			}

			contents.AsTangents = AsVector4Array(ref contents, bufferViewData, offset);

			return contents.AsTangents;
		}

		public uint[] AsTriangles(ref NumericArray contents, byte[] bufferViewData, uint offset)
		{
			if (contents.AsTriangles != null)
			{
				return contents.AsTriangles;
			}

			contents.AsTriangles = AsUIntArray(ref contents, bufferViewData, offset);

			return contents.AsTriangles;
		}

		public Matrix4x4[] AsMatrix4x4Array(ref NumericArray contents, byte[] bufferViewData, uint offset, bool normalizeIntValues = true)
		{
			if (contents.AsMatrix4x4s != null)
			{
				return contents.AsMatrix4x4s;
			}

			if (Type != GLTFAccessorAttributeType.MAT4)
			{
				return null;
			}

			Matrix4x4[] arr = new Matrix4x4[Count];
			uint totalByteOffset = ByteOffset + offset;

			uint componentSize;
			float maxValue;
			GetTypeDetails(ComponentType, out componentSize, out maxValue);

			if (normalizeIntValues)
			{
				maxValue = 1;
			}

			uint stride = (uint)(BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 16);

			for (uint idx = 0; idx < Count; idx++)
			{
				arr[idx] = new Matrix4x4(Matrix4x4.Identity);

				if (ComponentType == GLTFComponentType.Float)
				{
					for (uint i = 0; i < 16; i++)
					{
						float value = GetFloatElement(bufferViewData, totalByteOffset + idx * stride + componentSize * i);
						arr[idx].SetValue((int)i, value);
					}
				}
				else
				{
					for (uint i = 0; i < 16; i++)
					{
						float value = GetDiscreteElement(bufferViewData, totalByteOffset + idx * stride + componentSize * i, ComponentType) / maxValue;
						arr[idx].SetValue((int)i, value);
					}
				}
			}

			contents.AsMatrix4x4s = arr;
			return arr;
		}

		private static int GetDiscreteElement(byte[] bufferViewData, uint offset, GLTFComponentType type)
		{
			switch (type)
			{
				case GLTFComponentType.Byte:
					{
						return GetByteElement(bufferViewData, offset);
					}
				case GLTFComponentType.UnsignedByte:
					{
						return GetUByteElement(bufferViewData, offset);
					}
				case GLTFComponentType.Short:
					{
						return GetShortElement(bufferViewData, offset);
					}
				case GLTFComponentType.UnsignedShort:
					{
						return GetUShortElement(bufferViewData, offset);
					}
				default:
					{
						throw new Exception("Unsupported type passed in: " + type);
					}
			}
		}

		// technically byte and short are not spec compliant for unsigned types, but various files have it
		private static uint GetUnsignedDiscreteElement(byte[] bufferViewData, uint offset, GLTFComponentType type)
		{
			switch (type)
			{
				case GLTFComponentType.Byte:
					{
						return (uint)GetByteElement(bufferViewData, offset);
					}
				case GLTFComponentType.UnsignedByte:
					{
						return GetUByteElement(bufferViewData, offset);
					}
				case GLTFComponentType.Short:
					{
						return (uint)GetShortElement(bufferViewData, offset);
					}
				case GLTFComponentType.UnsignedShort:
					{
						return GetUShortElement(bufferViewData, offset);
					}
				case GLTFComponentType.UnsignedInt:
					{
						return GetUIntElement(bufferViewData, offset);
					}
				default:
					{
						throw new Exception("Unsupported type passed in: " + type);
					}

			}
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

    /// <summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct NumericArray
	{
		[FieldOffset(0)]
		public uint[] AsUInts;
		[FieldOffset(0)]
		public float[] AsFloats;
		[FieldOffset(0)]
		public Vector2[] AsVec2s;
		[FieldOffset(0)]
		public Vector3[] AsVec3s;
		[FieldOffset(0)]
		public Vector4[] AsVec4s;
		[FieldOffset(0)]
		public Matrix4x4[] AsMatrix4x4s;
		[FieldOffset(0)]
		public Color[] AsColors;
		[FieldOffset(0)]
		public Vector2[] AsTexcoords;
		[FieldOffset(0)]
		public Vector3[] AsVertices;
		[FieldOffset(0)]
		public Vector3[] AsNormals;
		[FieldOffset(0)]
		public Vector4[] AsTangents;
		[FieldOffset(0)]
		public uint[] AsTriangles;
	}
}
