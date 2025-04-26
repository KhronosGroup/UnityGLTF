using System;
using System.Collections.Generic;
using System.Linq;
using GLTF.Extensions;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using GLTF.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityGLTF.Extensions;

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

		private static unsafe sbyte GetByteElement(NativeArray<byte> buffer, uint byteOffset)
		{
			return *((sbyte*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(buffer) + (int)byteOffset);
		}

		private static unsafe byte GetUByteElement(NativeArray<byte> buffer, uint byteOffset)
		{
			return *((byte*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(buffer) + (int)byteOffset);
		}

		private static unsafe short GetShortElement(NativeArray<byte> buffer, uint byteOffset)
		{
			return *((short*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(buffer) + (int)byteOffset);
		}

		private static unsafe ushort GetUShortElement(NativeArray<byte> buffer, uint byteOffset)
		{
			return *((ushort*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(buffer) + (int)byteOffset);
		}

		private static unsafe uint GetUIntElement(NativeArray<byte> buffer, uint byteOffset)
		{
			return *((uint*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(buffer) + (int)byteOffset);
		}
		
		private static unsafe UInt16 GetUInt16Element(NativeArray<byte> buffer, uint byteOffset)
		{
			return *((UInt16*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(buffer) + (int)byteOffset);
		}

		private static unsafe float GetFloatElement(NativeArray<byte> buffer, uint byteOffset)
		{
			return *((float*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(buffer) + (int)byteOffset);
		}
		
		private static unsafe sbyte GetByteElement(void* buffer, uint byteOffset)
		{
			return *(sbyte*)(((byte*)buffer) + byteOffset);
		}
		
		private static unsafe float2 GetByte2Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (sbyte*)(((byte*)buffer) + byteOffset);
			return new float2(*p / maxValue, *(p + 1) / maxValue);
		}	
		
		private static unsafe float3 GetByte3Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (sbyte*)(((byte*)buffer) + byteOffset);
			return new float3(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue);
		}		

		private static unsafe float4 GetByte4Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (sbyte*)(((byte*)buffer) + byteOffset);
			return new float4(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue, *(p + 3) / maxValue);
		}		
		
		private static unsafe float4x4 GetByte4x4Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (sbyte*)(((byte*)buffer) + byteOffset);
			return new float4x4(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue, *(p + 3) / maxValue,
				*(p + 4) / maxValue, *(p + 5) / maxValue, *(p + 6) / maxValue, *(p + 7) / maxValue,
				*(p + 8) / maxValue, *(p + 9) / maxValue, *(p + 10) / maxValue, *(p + 11) / maxValue,
				*(p + 12) / maxValue, *(p + 13) / maxValue, *(p + 14) / maxValue, *(p + 15) / maxValue);
		}		
		
		private static unsafe byte GetUByteElement(void* buffer, uint byteOffset)
		{
			return *(byte*)(((byte*)buffer) + byteOffset);
		}

		private static unsafe float2 GetUByte2Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (byte*)(((byte*)buffer) + byteOffset);
			return new float2(*p / maxValue, *(p + 1) / maxValue);
		}		

		private static unsafe float3 GetUByte3Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (byte*)(((byte*)buffer) + byteOffset);
			return new float3(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue);
		}		

		private static unsafe float4 GetUByte4Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (byte*)(((byte*)buffer) + byteOffset);
			return new float4(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue, *(p + 3) / maxValue);
		}		
		
		private static unsafe float4x4 GetUByte4x4Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (byte*)(((byte*)buffer) + byteOffset);
			return new float4x4(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue, *(p + 3) / maxValue,
				*(p + 4) / maxValue, *(p + 5) / maxValue, *(p + 6) / maxValue, *(p + 7) / maxValue,
				*(p + 8) / maxValue, *(p + 9) / maxValue, *(p + 10) / maxValue, *(p + 11) / maxValue,
				*(p + 12) / maxValue, *(p + 13) / maxValue, *(p + 14) / maxValue, *(p + 15) / maxValue);
		}		
		
		private static unsafe short GetShortElement(void* buffer, uint byteOffset)
		{
			return *(short*)(((byte*)buffer) + byteOffset);
		}
		
		private static unsafe float2 GetShort2Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (short*)(((byte*)buffer) + byteOffset);
			return new float2(*p / maxValue, *(p + 1) / maxValue);
		}	
		
		private static unsafe float3 GetShort3Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (short*)(((byte*)buffer) + byteOffset);
			return new float3(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue);
		}	
		
		private static unsafe float4 GetShort4Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (short*)(((byte*)buffer) + byteOffset);
			return new float4(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue, *(p + 3) / maxValue);
		}			
		
		private static unsafe float4x4 GetShort4x4Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (short*)(((byte*)buffer) + byteOffset);
			return new float4x4(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue, *(p + 3) / maxValue,
				*(p + 4) / maxValue, *(p + 5) / maxValue, *(p + 6) / maxValue, *(p + 7) / maxValue,
				*(p + 8) / maxValue, *(p + 9) / maxValue, *(p + 10) / maxValue, *(p + 11) / maxValue,
				*(p + 12) / maxValue, *(p + 13) / maxValue, *(p + 14) / maxValue, *(p + 15) / maxValue);
		}		
		
		private static unsafe ushort GetUShortElement(void* buffer, uint byteOffset)
		{
			return *(ushort*)(((byte*)buffer) + byteOffset);
		}
		
		private static unsafe float2 GetUShort2Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (ushort*)(((byte*)buffer) + byteOffset);
			return new float2(*p / maxValue, *(p + 1) / maxValue);
		}			
		
		private static unsafe float3 GetUShort3Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (ushort*)(((byte*)buffer) + byteOffset);
			return new float3(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue);
		}		
		
		private static unsafe float4x4 GetUShort4x4Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (ushort*)(((byte*)buffer) + byteOffset);
			return new float4x4(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue, *(p + 3) / maxValue,
				*(p + 4) / maxValue, *(p + 5) / maxValue, *(p + 6) / maxValue, *(p + 7) / maxValue,
				*(p + 8) / maxValue, *(p + 9) / maxValue, *(p + 10) / maxValue, *(p + 11) / maxValue,
				*(p + 12) / maxValue, *(p + 13) / maxValue, *(p + 14) / maxValue, *(p + 15) / maxValue);
		}			
		
		private static unsafe float4 GetUShort4Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (ushort*)(((byte*)buffer) + byteOffset);
			return new float4(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue, *(p + 3) / maxValue);
		}			

		private static unsafe uint GetUIntElement(void* buffer, uint byteOffset)
		{
			return *(uint*)(((byte*)buffer) + byteOffset);
		}
		
		private static unsafe float2 GetUInt2Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (uint*)(((byte*)buffer) + byteOffset);
			return new float2(*p / maxValue, *(p + 1) / maxValue);
		}		
		
		private static unsafe float3 GetUInt3Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (uint*)(((byte*)buffer) + byteOffset);
			return new float3(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue);
		}		
		
		private static unsafe float4 GetUInt4Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (uint*)(((byte*)buffer) + byteOffset);
			return new float4(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue, *(p + 3) / maxValue);
		}		
		
		private static unsafe UInt16 GetUInt16Element(void* buffer, uint byteOffset)
		{
			return *(UInt16*)(((byte*)buffer) + byteOffset);
		}
		
		private static unsafe float2 GetUInt16_2_Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (UInt16*)(((byte*)buffer) + byteOffset);
			return new float2(*p / maxValue, *(p + 1) / maxValue);
		}		
		
		private static unsafe float3 GetUInt16_3_Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (UInt16*)(((byte*)buffer) + byteOffset);
			return new float3(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue);
		}		
		
		private static unsafe float4 GetUInt16_4_Element(void* buffer, uint byteOffset, float maxValue)
		{
			var p = (UInt16*)(((byte*)buffer) + byteOffset);
			return new float4(*p / maxValue, *(p + 1) / maxValue, *(p + 2) / maxValue, *(p + 3) / maxValue);
		}		

		private static unsafe float GetFloatElement(void* buffer, uint byteOffset)
		{
			return *(float*)(((byte*)buffer) + byteOffset);
		}		
		
		private static unsafe float2 GetFloat2Element(void* buffer, uint byteOffset)
		{
			var p = (float*)(((byte*)buffer) + byteOffset);
			return new float2(*p, *(p + 1));
		}			
		
		private static unsafe float3 GetFloat3Element(void* buffer, uint byteOffset)
		{
			var p = (float*)(((byte*)buffer) + byteOffset);
			return new float3(*p, *(p + 1), *(p + 2));
		}

		private static unsafe float4 GetFloat4Element(void* buffer, uint byteOffset)
		{
			var p = (float*)(((byte*)buffer) + byteOffset);
			return new float4(*p, *(p + 1),*(p + 2),*(p + 3));
		}
		
		private static unsafe float4x4 GetFloat4x4Element(void* buffer, uint byteOffset)
		{
			var p = (float4*)(((byte*)buffer) + byteOffset);
			return new float4x4(*p, *(p + 1),*(p + 2),*(p + 3));
		}		
		
		private static unsafe float4 GetColorElement(void* buffer, uint byteOffset)
		{
			var p = (float*)(((byte*)buffer) + byteOffset);
			return new float4(*p, *(p + 1),*(p + 2),*(p + 3));
		}		

		private static unsafe float4 GetColorElement(void* buffer, uint byteOffset, float alpha)
		{
			var p = (float*)(((byte*)buffer) + byteOffset);
			return new float4(*p, *(p + 1),*(p + 2), alpha);
		}

		public static unsafe float3[] AsSparseFloat3Array(Accessor paraAccessor, ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0, bool normalizeIntValues = true)
		{
			var Count = paraAccessor.Sparse.Count;
			var ComponentType = paraAccessor.ComponentType;

			var arr = new float3[paraAccessor.Sparse.Count];
			var totalByteOffset = (uint)paraAccessor.Sparse.Values.ByteOffset + offset;

			GetTypeDetails(paraAccessor.ComponentType, out uint componentSize, out float maxValue);
			uint stride = componentSize * 3;
			if (!normalizeIntValues) maxValue = 1;
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);

			if (ComponentType == GLTFComponentType.Float)
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetFloat3Element(bufferPointer, totalByteOffset + idx * stride);
			}
			else
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetDiscreteFloat3Element(bufferPointer, totalByteOffset + idx * stride + componentSize * 0, ComponentType, maxValue);
			}
			contents.AsFloat3s = arr;
			return arr;
		}
		
		public static unsafe float3[] AsSparseFloat3ArrayConversion(Accessor paraAccessor, ref NumericArray contents, NativeArray<byte> bufferViewData, float3 conversion, uint offset = 0, bool normalizeIntValues = true)
		{
			var Count = paraAccessor.Sparse.Count;
			var ComponentType = paraAccessor.ComponentType;

			var arr = new float3[paraAccessor.Sparse.Count];
			var totalByteOffset = (uint) paraAccessor.Sparse.Values.ByteOffset + offset;

			GetTypeDetails(paraAccessor.ComponentType, out uint componentSize, out float maxValue);
			uint stride = componentSize * 3;
			if (normalizeIntValues) maxValue = 1;
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);
			
			if (ComponentType == GLTFComponentType.Float)
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetFloat3Element(bufferPointer, totalByteOffset + idx * stride) * conversion;
			}
			else
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetDiscreteFloat3Element(bufferPointer, totalByteOffset + idx * stride + componentSize * 0, ComponentType, maxValue) * conversion;
			}
			contents.AsFloat3s = arr;
			return arr;
		}		

		public static unsafe uint[] AsSparseUIntArray(Accessor paraAccessor, ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0)
		{
			var arr = new uint[paraAccessor.Sparse.Count];
			var totalByteOffset = paraAccessor.Sparse.Indices.ByteOffset + offset;

			GetTypeDetails(paraAccessor.Sparse.Indices.ComponentType, out uint componentSize, out _);
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);

			uint stride = componentSize;
			for (uint idx = 0; idx < paraAccessor.Sparse.Count; idx++)
			{
				if (stride == 1)
				{
					arr[idx] = (uint)bufferViewData[(int)(totalByteOffset + idx)];
				}
				else
				{
					arr[idx] = GetUInt16Element(bufferPointer,(uint)(totalByteOffset + idx * stride));
				}
			}
			contents.AsUInts = arr;
			return arr;
		}

		internal static void GetTypeDetails(
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

		public unsafe uint[] AsUIntArray(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0)
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

			GetTypeDetails(ComponentType, out uint componentSize, out _);
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);
			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize;

			if (ComponentType == GLTFComponentType.Float)
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = (uint)System.Math.Floor(GetFloatElement(bufferPointer, totalByteOffset + idx * stride));
			}
			else
			{
				for (uint idx = 0; idx < Count; idx++) 
					arr[idx] = GetUnsignedDiscreteElement(bufferPointer, totalByteOffset + idx * stride, ComponentType);
			}
			contents.AsUInts = arr;
			return arr;
		}

		public unsafe float[] AsFloatArray(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0, bool normalizeIntValues = true)
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
			
			if (bufferViewData == default)
			{
				contents.AsFloats = arr;
				return arr;
			}
			
			uint totalByteOffset = ByteOffset + offset;

			GetTypeDetails(ComponentType, out uint componentSize, out float maxValue);
			if (normalizeIntValues) maxValue = 1f;
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);

			uint stride = BufferView?.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize;

			if (ComponentType == GLTFComponentType.Float)
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetFloatElement(bufferPointer, totalByteOffset + idx * stride);
			}
			else
			{
				for (uint idx = 0; idx < Count; idx++) 
					arr[idx] = GetUnsignedDiscreteElement(bufferPointer, totalByteOffset + idx * stride, ComponentType) / maxValue;
			}
			contents.AsFloats = arr;
			return arr;
		}

		public unsafe float2[] AsFloat2Array(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0, bool normalizeIntValues = true)
		{
			if (contents.AsFloat2s != null)
			{
				return contents.AsFloat2s;
			}

			if (Type != GLTFAccessorAttributeType.VEC2)
			{
				return null;
			}

			if (ComponentType == GLTFComponentType.UnsignedInt)
			{
				return null;
			}

			var arr = new float2[Count];
			var totalByteOffset = ByteOffset + offset;

			GetTypeDetails(ComponentType, out uint componentSize, out float maxValue);
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);

			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 2;
			if (!normalizeIntValues) maxValue = 1;

			if (ComponentType == GLTFComponentType.Float)
			{
				for (uint idx = 0; idx < Count; idx++) 
					arr[idx] = GetFloat2Element(bufferPointer, totalByteOffset + idx * stride);
			}
			else
			{
				for (uint idx = 0; idx < Count; idx++) 
					arr[idx] = GetDiscreteFloat2Element(bufferPointer, totalByteOffset + idx * stride, ComponentType, maxValue);
			}
			contents.AsFloat2s = arr;
			return arr;
		}

		public unsafe float2[] AsTexcoordArray(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0, bool normalizeIntValues = true)
		{
			if (contents.AsFloat2s != null)
			{
				return contents.AsFloat2s;
			}

			if (Type != GLTFAccessorAttributeType.VEC2 && Type != GLTFAccessorAttributeType.VEC3 && Type != GLTFAccessorAttributeType.VEC4)
			{
				return null;
			}

			if (ComponentType == GLTFComponentType.UnsignedInt)
			{
				return null;
			}

			var arr = new float2[Count];
			var totalByteOffset = ByteOffset + offset;

			GetTypeDetails(ComponentType, out uint componentSize, out float maxValue);
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);

			uint typeBasedStride = Type == GLTFAccessorAttributeType.VEC2 ? (uint) 2 : Type == GLTFAccessorAttributeType.VEC3 ? (uint) 3 : (uint) 4;
			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * typeBasedStride;
			if (!normalizeIntValues) maxValue = 1;

			if (ComponentType == GLTFComponentType.Float)
			{
				for (uint idx = 0; idx < Count; idx++)
				{
					var uv = GetFloat2Element(bufferPointer, totalByteOffset + idx * stride);
					arr[idx] = new float2(uv.x, 1f - uv.y);
				}
			}
			else
			{
				for (uint idx = 0; idx < Count; idx++)
				{
					var uv = GetDiscreteFloat2Element(bufferPointer, totalByteOffset + idx * stride, ComponentType, maxValue);
					arr[idx] = new float2(uv.x, 1f - uv.y);
				}
			}
			contents.AsFloat2s = arr;
			return arr;
		}		
		
		public unsafe float3[] AsFloat3Array(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0, bool normalizeIntValues = true)
		{
			if (contents.AsFloat3s != null)
			{
				return contents.AsFloat3s;
			}

			if (Type != GLTFAccessorAttributeType.VEC3)
			{
				return null;
			}

			var arr = new float3[Count];
			var totalByteOffset = ByteOffset + offset;

			GetTypeDetails(ComponentType, out uint componentSize, out float maxValue);
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);

			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 3;
			if (!normalizeIntValues) maxValue = 1;

			if (ComponentType == GLTFComponentType.Float)
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetFloat3Element(bufferPointer, totalByteOffset + idx * stride);
			}
			else
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetDiscreteFloat3Element(bufferPointer, totalByteOffset + idx * stride, ComponentType, maxValue);

			}
			contents.AsFloat3s = arr;
			return arr;
		}

		public unsafe float3[] AsFloat3ArrayConversion(ref NumericArray contents, NativeArray<byte> bufferViewData, float3 conversion, uint offset = 0, bool normalizeIntValues = true)
		{
			if (contents.AsFloat3s != null)
			{
				return contents.AsFloat3s;
			}

			if (Type != GLTFAccessorAttributeType.VEC3)
			{
				return null;
			}

			var arr = new float3[Count];
			var totalByteOffset = ByteOffset + offset;

			GetTypeDetails(ComponentType, out uint componentSize, out float maxValue);
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);

			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 3;
			if (!normalizeIntValues) maxValue = 1;

			if (ComponentType == GLTFComponentType.Float)
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetFloat3Element(bufferPointer, totalByteOffset + idx * stride) * conversion;
			}
			else
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetDiscreteFloat3Element(bufferPointer, totalByteOffset + idx * stride, ComponentType, maxValue) * conversion;

			}
			contents.AsFloat3s = arr;
			return arr;
		}		
		
		public unsafe float4[] AsFloat4Array(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0, bool normalizeIntValues = true)
		{
			if (contents.AsFloat4s != null)
			{
				return contents.AsFloat4s;
			}

			if (Type != GLTFAccessorAttributeType.VEC4)
			{
				return null;
			}

			if (ComponentType == GLTFComponentType.UnsignedInt)
			{
				return null;
			}

			var arr = new float4[Count];
			var totalByteOffset = ByteOffset + offset;

			GetTypeDetails(ComponentType, out uint componentSize, out float maxValue);
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);

			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 4;
			if (!normalizeIntValues) maxValue = 1;

			if (ComponentType == GLTFComponentType.Float)
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetFloat4Element(bufferPointer, totalByteOffset + idx * stride);
			}
			else
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetDiscreteFloat4Element(bufferPointer, totalByteOffset + idx * stride, ComponentType, maxValue);

			}
			contents.AsFloat4s = arr;
			return arr;
		}

		public unsafe float4[] AsFloat4ArrayConversion(ref NumericArray contents, NativeArray<byte> bufferViewData, float4 conversion, uint offset = 0, bool normalizeIntValues = true)
		{
			if (contents.AsFloat4s != null)
			{
				return contents.AsFloat4s;
			}

			if (Type != GLTFAccessorAttributeType.VEC4)
			{
				return null;
			}

			if (ComponentType == GLTFComponentType.UnsignedInt)
			{
				return null;
			}

			var arr = new float4[Count];
			var totalByteOffset = ByteOffset + offset;

			GetTypeDetails(ComponentType, out uint componentSize, out float maxValue);
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);

			uint stride = BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 4;
			if (!normalizeIntValues) maxValue = 1;

			if (ComponentType == GLTFComponentType.Float)
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetFloat4Element(bufferPointer, totalByteOffset + idx * stride) * conversion;
			}
			else
			{
				for (uint idx = 0; idx < Count; idx++)
					arr[idx] = GetDiscreteFloat4Element(bufferPointer, totalByteOffset + idx * stride, ComponentType, maxValue) * conversion;

			}
			contents.AsFloat4s = arr;
			return arr;
		}		
		
		public unsafe float4[] AsColorArray(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0, bool normalizeIntValues = true)
		{
			if (contents.AsFloat4s != null)
			{
				return contents.AsFloat4s;
			}

			if (Type != GLTFAccessorAttributeType.VEC3 && Type != GLTFAccessorAttributeType.VEC4)
			{
				return null;
			}

			if (ComponentType == GLTFComponentType.UnsignedInt)
			{
				return null;
			}

			var arr = new float4[Count];
			var totalByteOffset = ByteOffset + offset;

			GetTypeDetails(ComponentType, out uint componentSize, out float maxValue);
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);
			if (!normalizeIntValues) maxValue = 1f;
			
			uint stride = (uint)(BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * (Type == GLTFAccessorAttributeType.VEC3 ? 3 : 4));

			if (ComponentType == GLTFComponentType.Float)
			{
				if (Type == GLTFAccessorAttributeType.VEC4)
				{
					for (uint idx = 0; idx < Count; idx++)
						arr[idx] = GetColorElement(bufferPointer, totalByteOffset + idx * stride);
					
				}
				else
				{
					for (uint idx = 0; idx < Count; idx++)
						arr[idx] = GetColorElement(bufferPointer, totalByteOffset + idx * stride, 1f);
				}
				
			}
			else
			{
				if (Type == GLTFAccessorAttributeType.VEC4)
				{
					for (uint idx = 0; idx < Count; idx++)
						arr[idx] = GetDiscreteColorElement(bufferPointer, totalByteOffset + idx * stride, ComponentType, maxValue);
				}
				else
				{
					for (uint idx = 0; idx < Count; idx++)
						arr[idx] = GetDiscreteColorElement(bufferPointer, totalByteOffset + idx * stride, ComponentType, maxValue, 1f);
					
				}			
			}
			contents.AsFloat4s = arr;
			return arr;
		}

		public float3[] AsVertexArray(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0, bool normalized = true)
		{
			if (contents.AsFloat3s != null)
			{
				return contents.AsFloat3s;
			}

			float3 conversion = new float3(SchemaExtensions.CoordinateSpaceConversionScale.X,
				SchemaExtensions.CoordinateSpaceConversionScale.Y, SchemaExtensions.CoordinateSpaceConversionScale.Z);
			contents.AsFloat3s = AsFloat3ArrayConversion(ref contents, bufferViewData, conversion, offset, normalized);

			return contents.AsFloat3s;
		}

		public float3[] AsNormalArray(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0, bool normalized = true)
		{
			if (contents.AsFloat3s != null)
			{
				return contents.AsFloat3s;
			}

			float3 conversion = new float3(SchemaExtensions.CoordinateSpaceConversionScale.X,
				SchemaExtensions.CoordinateSpaceConversionScale.Y, 
				SchemaExtensions.CoordinateSpaceConversionScale.Z);
			
			contents.AsFloat3s = AsFloat3ArrayConversion(ref contents, bufferViewData, conversion, offset, normalized);

			return contents.AsFloat3s;
		}

		public float4[] AsTangentArray(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0, bool normalized = true)
		{
			if (contents.AsFloat4s != null)
			{
				return contents.AsFloat4s;
			}
			float4 conversion = new float4(SchemaExtensions.TangentSpaceConversionScale.X, 
				SchemaExtensions.TangentSpaceConversionScale.Y, 
				SchemaExtensions.TangentSpaceConversionScale.Z, 
				SchemaExtensions.TangentSpaceConversionScale.W);

			contents.AsFloat4s = AsFloat4ArrayConversion(ref contents, bufferViewData, conversion, offset, normalized);

			return contents.AsFloat4s;
		}

		public uint[] AsTriangles(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0)
		{
			if (contents.AsTriangles != null)
			{
				return contents.AsTriangles;
			}

			contents.AsTriangles = AsUIntArray(ref contents, bufferViewData, offset);

			return contents.AsTriangles;
		}

		public unsafe float4x4[] AsMatrix4x4Array(ref NumericArray contents, NativeArray<byte> bufferViewData, uint offset = 0, bool normalizeIntValues = true)
		{
			if (contents.AsMatrix4x4s != null)
			{
				return contents.AsMatrix4x4s;
			}

			if (Type != GLTFAccessorAttributeType.MAT4)
			{
				return null;
			}

			float4x4[] arr = new float4x4[Count];
			uint totalByteOffset = ByteOffset + offset;

			GetTypeDetails(ComponentType, out uint componentSize, out float maxValue);
			var bufferPointer = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr<byte>(bufferViewData);

			if (!normalizeIntValues) maxValue = 1;

			uint stride = (uint)(BufferView.Value.ByteStride > 0 ? BufferView.Value.ByteStride : componentSize * 16);

			for (uint idx = 0; idx < Count; idx++)
			{
				arr[idx] = new float4x4(float4x4.identity);

				if (ComponentType == GLTFComponentType.Float)
				{
					arr[idx] = GetFloat4x4Element(bufferPointer, totalByteOffset + idx * stride);
				}
				else
				{
					arr[idx] = GetDiscreteFloat4x4Element(bufferPointer, totalByteOffset + idx * stride, ComponentType, maxValue);
				}
			}
			contents.AsMatrix4x4s = arr;
			return arr;
		}

		private static int GetDiscreteElement(NativeArray<byte> bufferViewData, uint offset, GLTFComponentType type)
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
		
		private static unsafe int GetDiscreteElement(void* bufferViewData, uint offset, GLTFComponentType type)
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

		private static unsafe float4 GetDiscreteColorElement(void* bufferViewData, uint offset, GLTFComponentType type, float maxValue)
		{
			switch (type)
			{
				case GLTFComponentType.Byte:
				{
					return GetByte4Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.UnsignedByte:
				{
					return GetUByte4Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.Short:
				{
					return GetShort4Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.UnsignedShort:
				{
					return GetUShort4Element(bufferViewData, offset, maxValue);
				}
				default:
				{
					throw new Exception("Unsupported type passed in: " + type);
				}
			}
		}			
		
		private static unsafe float4 GetDiscreteColorElement(void* bufferViewData, uint offset, GLTFComponentType type, float maxValue, float alpha)
		{
			switch (type)
			{
				case GLTFComponentType.Byte:
				{
					return new float4(GetByte3Element(bufferViewData, offset, maxValue), alpha);
				}
				case GLTFComponentType.UnsignedByte:
				{
					return new float4(GetUByte3Element(bufferViewData, offset, maxValue), alpha);
				}
				case GLTFComponentType.Short:
				{
					return new float4(GetShort3Element(bufferViewData, offset, maxValue), alpha);
				}
				case GLTFComponentType.UnsignedShort:
				{
					return new float4(GetUShort3Element(bufferViewData, offset, maxValue), alpha);
				}
				default:
				{
					throw new Exception("Unsupported type passed in: " + type);
				}
			}
		}			
		
		private static unsafe float4x4 GetDiscreteFloat4x4Element(void* bufferViewData, uint offset, GLTFComponentType type, float maxValue)
		{
			switch (type)
			{
				case GLTFComponentType.Byte:
				{
					return GetByte4x4Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.UnsignedByte:
				{
					return GetUByte4x4Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.Short:
				{
					return GetShort4x4Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.UnsignedShort:
				{
					return GetUShort4x4Element(bufferViewData, offset, maxValue);
				}
				default:
				{
					throw new Exception("Unsupported type passed in: " + type);
				}
			}
		}		
		
		private static unsafe float4 GetDiscreteFloat4Element(void* bufferViewData, uint offset, GLTFComponentType type, float maxValue)
		{
			switch (type)
			{
				case GLTFComponentType.Byte:
				{
					return GetByte4Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.UnsignedByte:
				{
					return GetUByte4Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.Short:
				{
					return GetShort4Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.UnsignedShort:
				{
					return GetUShort4Element(bufferViewData, offset, maxValue);
				}
				default:
				{
					throw new Exception("Unsupported type passed in: " + type);
				}
			}
		}		
		
		private static unsafe float3 GetDiscreteFloat3Element(void* bufferViewData, uint offset, GLTFComponentType type, float maxValue)
		{
			switch (type)
			{
				case GLTFComponentType.Byte:
				{
					return GetByte3Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.UnsignedByte:
				{
					return GetUByte3Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.Short:
				{
					return GetShort3Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.UnsignedShort:
				{
					return GetUShort3Element(bufferViewData, offset, maxValue);
				}
				default:
				{
					throw new Exception("Unsupported type passed in: " + type);
				}
			}
		}			

		private static unsafe float2 GetDiscreteFloat2Element(void* bufferViewData, uint offset, GLTFComponentType type, float maxValue)
		{
			switch (type)
			{
				case GLTFComponentType.Byte:
				{
					return GetByte2Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.UnsignedByte:
				{
					return GetUByte2Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.Short:
				{
					return GetShort2Element(bufferViewData, offset, maxValue);
				}
				case GLTFComponentType.UnsignedShort:
				{
					return GetUShort2Element(bufferViewData, offset, maxValue);
				}
				default:
				{
					throw new Exception("Unsupported type passed in: " + type);
				}
			}
		}			
		
		// technically byte and short are not spec compliant for unsigned types, but various files have it
		private static uint GetUnsignedDiscreteElement(NativeArray<byte> bufferViewData, uint offset, GLTFComponentType type)
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
		
		private static unsafe uint GetUnsignedDiscreteElement(void* bufferViewData, uint offset, GLTFComponentType type)
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

	public static class GLTFAccessorAttributeTypeExtensions
	{
		public static int ComponentCount(this GLTFAccessorAttributeType attrType)
		{
			switch (attrType)
			{
				case GLTFAccessorAttributeType.SCALAR:
					return 1;
				case GLTFAccessorAttributeType.VEC2:
					return 2;
				case GLTFAccessorAttributeType.VEC3:
					return 3;
				case GLTFAccessorAttributeType.VEC4:
					return 4;
				case GLTFAccessorAttributeType.MAT2:
					return 4;
				case GLTFAccessorAttributeType.MAT3:
					return 9;
				case GLTFAccessorAttributeType.MAT4:
					return 16;
			}

			return 0;
		}
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
		public float2[] AsFloat2s;
		
		[FieldOffset(0)]
		public float3[] AsFloat3s;

		[FieldOffset(0)]
		public float4[] AsFloat4s;
		
		[FieldOffset(0)]
		public float4x4[] AsMatrix4x4s;

		[FieldOffset(0)]
		public uint[] AsTriangles;
	}
}
