#define USE_FAST_BINARY_WRITER

using System;
using System.Collections.Generic;
using System.IO;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;

namespace UnityGLTF
{
public partial class GLTFSceneExporter
{
#if USE_FAST_BINARY_WRITER
	private BinaryWriterWithLessAllocations _bufferWriter;
#else
		private BinaryWriter _bufferWriter;
#endif

	/// <summary>
	/// Convenience function to copy from a stream to a binary writer, for
	/// compatibility with pre-.NET 4.0.
	/// Note: Does not set position/seek in either stream. After executing,
	/// the input buffer's position should be the end of the stream.
	/// </summary>
	/// <param name="input">Stream to copy from</param>
	/// <param name="output">Stream to copy to.</param>
	private static void CopyStream(Stream input, BinaryWriter output)
	{
		byte[] buffer = new byte[8 * 1024];
		int length;
		while ((length = input.Read(buffer, 0, buffer.Length)) > 0)
		{
			output.Write(buffer, 0, length);
		}
	}

	/// <summary>
	/// Pads a stream with additional bytes.
	/// </summary>
	/// <param name="stream">The stream to be modified.</param>
	/// <param name="pad">The padding byte to append. Defaults to ASCII
	/// space (' ').</param>
	/// <param name="boundary">The boundary to align with, in bytes.
	/// </param>
	private static void AlignToBoundary(Stream stream, byte pad = (byte)' ', uint boundary = 4)
	{
		uint currentLength = (uint)stream.Length;
		uint newLength = CalculateAlignment(currentLength, boundary);
		for (int i = 0; i < newLength - currentLength; i++)
		{
			stream.WriteByte(pad);
		}
	}

	/// <summary>
	/// Calculates the number of bytes of padding required to align the
	/// size of a buffer with some multiple of byteAllignment.
	/// </summary>
	/// <param name="currentSize">The current size of the buffer.</param>
	/// <param name="byteAlignment">The number of bytes to align with.</param>
	/// <returns></returns>
	public static uint CalculateAlignment(uint currentSize, uint byteAlignment)
	{
		return (currentSize + byteAlignment - 1) / byteAlignment * byteAlignment;
	}

	private AccessorId ExportAccessorUint(Vector4[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorUintArrayMarker.Begin();

			var count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.UnsignedShort;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float minZ = arr[0].z;
			float minW = arr[0].w;
			float maxX = arr[0].x;
			float maxY = arr[0].y;
			float maxZ = arr[0].z;
			float maxW = arr[0].w;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.w < minW)
				{
					minW = cur.w;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
				if (cur.w > maxW)
				{
					maxW = cur.w;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ, minW };
			accessor.Max = new List<double> { maxX, maxY, maxZ, maxW };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
			foreach (var vec in arr)
			{
				_bufferWriter.Write((ushort)vec.x);
				_bufferWriter.Write((ushort)vec.y);
				_bufferWriter.Write((ushort)vec.z);
				_bufferWriter.Write((ushort)vec.w);
			}
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorUintArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		// This is used for Quaternions / Rotations. Lights' and Cameras' orientations need to be flipped.
		private AccessorId ExportAccessorSwitchHandedness(Quaternion[] arr, bool invertLookDirection)
		{
			exportAccessorMarker.Begin();
			exportAccessorVector4ArrayMarker.Begin();

			var count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var copy = new Quaternion[count];
			Array.Copy(arr, copy, count);
			for (int i = 0; i < count; i++)
			{
				var v = copy[i];
				if (invertLookDirection)
				{
					v *= new Quaternion(0, -1, 0, 0);
				}
				v = v.SwitchHandedness().normalized;
				copy[i] = v;
			}
			arr = copy;

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			var a0 = arr[0];
			float minX = a0.x;
			float minY = a0.y;
			float minZ = a0.z;
			float minW = a0.w;
			float maxX = a0.x;
			float maxY = a0.y;
			float maxZ = a0.z;
			float maxW = a0.w;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.w < minW)
				{
					minW = cur.w;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
				if (cur.w > maxW)
				{
					maxW = cur.w;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ, minW };
			accessor.Max = new List<double> { maxX, maxY, maxZ, maxW };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
#if USE_FAST_BINARY_WRITER
			_bufferWriter.Write(arr);
#else
			foreach (var vec in arr)
			{
				_bufferWriter.Write(vect.x);
				_bufferWriter.Write(vect.y);
				_bufferWriter.Write(vect.z);
				_bufferWriter.Write(vect.w);
			}
#endif
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorVector4ArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(Matrix4x4[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorMatrix4x4ArrayMarker.Begin();

			var count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.MAT4;

			// Dont serialize min/max for matrices

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
			foreach (var mat in arr)
			{
				var m = SchemaExtensions.ToGltfMatrix4x4Convert(mat);
				for (uint i = 0; i < 4; ++i)
				{
					var col = m.GetColumn(i);
					_bufferWriter.Write(col.X);
					_bufferWriter.Write(col.Y);
					_bufferWriter.Write(col.Z);
					_bufferWriter.Write(col.W);
				}
			}
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorMatrix4x4ArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		/// <summary>
		/// Manually export an accessor of an arbitrary type. You need to manage conversion of your data to byte[] yourself.
		/// </summary>
		/// <returns>
		/// The accessor id.You can get the actual accessor via val.Root.Accessors[val.Id].
		/// </returns>
		public AccessorId ExportAccessor(byte[] data, uint count, GLTFAccessorAttributeType type, GLTFComponentType componentType, List<double> min, List<double> max)
		{
			exportAccessorMarker.Begin();
			exportAccessorByteArrayMarker.Begin();
			
			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}
			
			var accessor = new Accessor();
			accessor.Count = count;
			accessor.Type = type;
			accessor.ComponentType = componentType;
			
			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);
			
			foreach (var v in data)
			{
				_bufferWriter.Write((byte)v);
			}
			
			accessor.Min = min;
			accessor.Max = max;
			
			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);
			accessor.BufferView = ExportBufferView(byteOffset, byteLength);
			
			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);
			
			exportAccessorByteArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}
		
		public AccessorId ExportAccessor(byte[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorByteArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.SCALAR;

			int min = arr[0];
			int max = arr[0];

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur < min)
				{
					min = cur;
				}
				if (cur > max)
				{
					max = cur;
				}
			}

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			accessor.ComponentType = GLTFComponentType.UnsignedByte;

			foreach (var v in arr)
			{
				_bufferWriter.Write((byte)v);
			}

			accessor.Min = new List<double> { min };
			accessor.Max = new List<double> { max };

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorByteArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(float[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorFloatArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.SCALAR;

			exportAccessorMinMaxMarker.Begin();
			var min = arr[0];
			var max = arr[0];

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur < min)
				{
					min = cur;
				}
				if (cur > max)
				{
					max = cur;
				}
			}
			exportAccessorMinMaxMarker.End();

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, sizeof(float));

			exportAccessorBufferWriteMarker.Begin();
			accessor.ComponentType = GLTFComponentType.Float;

#if USE_FAST_BINARY_WRITER
			_bufferWriter.Write(arr);
#else
			foreach (var v in arr)
			{
				_bufferWriter.Write((float)v);
			}
#endif
			exportAccessorBufferWriteMarker.End();

			accessor.Min = new List<double> { min };
			accessor.Max = new List<double> { max };

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorFloatArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(int[] arr, bool isIndices = false)
		{
			exportAccessorMarker.Begin();
			if (isIndices) exportAccessorIntArrayIndicesMarker.Begin(); else exportAccessorIntArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.SCALAR;

			int min = arr[0];
			int max = arr[0];

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur < min)
				{
					min = cur;
				}
				if (cur > max)
				{
					max = cur;
				}
			}

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			// From the spec:
			// Values of the index accessor must not include the maximum value for the given component type,
			// which triggers primitive restart in several graphics APIs and would require client implementations to rebuild the index buffer.
			// Primitive restart values are disallowed and all index values must refer to actual vertices.
			int maxAllowedValue = isIndices ? max + 1 : max;

			if (maxAllowedValue <= byte.MaxValue && min >= byte.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedByte;

				foreach (var v in arr)
				{
					_bufferWriter.Write((byte)v);
				}
			}
			else if (maxAllowedValue <= sbyte.MaxValue && min >= sbyte.MinValue && !isIndices)
			{
				accessor.ComponentType = GLTFComponentType.Byte;

				foreach (var v in arr)
				{
					_bufferWriter.Write((sbyte)v);
				}
			}
			else if (maxAllowedValue <= short.MaxValue && min >= short.MinValue && !isIndices)
			{
				accessor.ComponentType = GLTFComponentType.Short;

				foreach (var v in arr)
				{
					_bufferWriter.Write((short)v);
				}
			}
			else if (maxAllowedValue <= ushort.MaxValue && min >= ushort.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedShort;

				foreach (var v in arr)
				{
					_bufferWriter.Write((ushort)v);
				}
			}
			else if (maxAllowedValue >= uint.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedInt;

				foreach (var v in arr)
				{
					_bufferWriter.Write((uint)v);
				}
			}
			else
			{
				accessor.ComponentType = GLTFComponentType.Float;

				foreach (var v in arr)
				{
					_bufferWriter.Write((float)v);
				}
			}

			accessor.Min = new List<double> { min };
			accessor.Max = new List<double> { max };

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			if (isIndices) exportAccessorIntArrayIndicesMarker.End(); else exportAccessorIntArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(Vector2[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorVector2ArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC2;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float maxX = arr[0].x;
			float maxY = arr[0].y;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
			}

			accessor.Min = new List<double> { minX, minY };
			accessor.Max = new List<double> { maxX, maxY };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			foreach (var vec in arr)
			{
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
			}

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorVector2ArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(Vector3[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorVector3ArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC3;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float minZ = arr[0].z;
			float maxX = arr[0].x;
			float maxY = arr[0].y;
			float maxZ = arr[0].z;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ };
			accessor.Max = new List<double> { maxX, maxY, maxZ };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
#if USE_FAST_BINARY_WRITER
			_bufferWriter.Write(arr);
#else
			foreach (var vec in arr)
			{
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
			}
#endif
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength, sizeof(float) * 3);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorVector3ArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="baseAccessor"></param>
		/// <param name="baseData">The data is treated as "additive" (e.g. blendshapes) when baseData != null</param>
		/// <param name="arr"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private AccessorId ExportSparseAccessor(AccessorId baseAccessor, Vector3[] baseData, Vector3[] arr)
		{
			exportSparseAccessorMarker.Begin();

			uint dataCount = (uint) arr.Length;
			if (dataCount == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			// TODO need to assert that these types match to the base accessor
			// TODO we might need to build a data <> accessor dict as well to avoid having to pass in the base data again

			// need to assert data and baseData have the same length etc.
			if (baseData != null && baseData.Length != arr.Length)
			{
				throw new Exception("Sparse Accessor Base Data must either be null or the same length as the data array, current: " + baseData.Length + " != " + arr.Length);
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = dataCount;
			accessor.Type = GLTFAccessorAttributeType.VEC3;

			if(baseAccessor != null)
			{
				accessor.BufferView = baseAccessor.Value.BufferView;
				accessor.ByteOffset = baseAccessor.Value.ByteOffset;
			}

			accessor.Sparse = new AccessorSparse();
			var sparse = accessor.Sparse;

			var indices = new List<int>();

			// Debug.Log("Values for sparse data array:\n " + string.Join("\n ", arr));
			for (int i = 0; i < arr.Length; i++)
			{
				var comparer = (baseAccessor == null || baseData == null) ? Vector3.zero : baseData[i];
				if (comparer != arr[i])
				{
					indices.Add(i);
				}
			}

			// HACK since GLTF doesn't allow 0 buffer length, but that can well happen when a morph target exactly matches the base mesh
			// NOT doing this results in GLTF validation errors about buffers having length 0
			if (indices.Count < 1)
			{
				indices = new List<int>() {0};
			}

			// we need to calculate the min/max of the entire new data array
			uint count = (uint) arr.Length;

			var firstElement = baseData != null ? (baseData[0] + arr[0]) : arr[0];
			float minX = firstElement.x;
			float minY = firstElement.y;
			float minZ = firstElement.z;
			float maxX = firstElement.x;
			float maxY = firstElement.y;
			float maxZ = firstElement.z;

			for (var i = 1; i < count; i++)
			{
				var cur = baseData != null ? (baseData[i] + arr[i]) : arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ };
			accessor.Max = new List<double> { maxX, maxY, maxZ };

			// write indices
			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffsetIndices = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			// Debug.Log("Storing " + indices.Count + " sparse indices + values");

			foreach (var index in indices)
			{
				_bufferWriter.Write(index);
			}

			uint byteLengthIndices = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffsetIndices, 4);

			sparse.Indices = new AccessorSparseIndices();
			// TODO should be properly using the smallest possible component type
			sparse.Indices.ComponentType = GLTFComponentType.UnsignedInt;
			sparse.Indices.BufferView = ExportBufferView(byteOffsetIndices, byteLengthIndices);

			// write values
			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
			foreach (var i in indices)
			{
				var vec = arr[i];
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
			}
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			sparse.Values = new AccessorSparseValues();
			sparse.Values.BufferView = ExportBufferView(byteOffset, byteLength);

			sparse.Count = indices.Count;

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportSparseAccessorMarker.End();

			return id;
		}


		private AccessorId ExportAccessor(Vector4[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorVector4ArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			// sanitize tangents: in some cases Unity's importer produces NaN values for calculated tangents.
			for (uint i = 0; i < arr.Length; i++)
			{
				var v = arr[i];
				if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsNaN(v.w))
					arr[i].Set(0,0,1,1);
			}

			float minX = arr[0].x;
			float minY = arr[0].y;
			float minZ = arr[0].z;
			float minW = arr[0].w;
			float maxX = arr[0].x;
			float maxY = arr[0].y;
			float maxZ = arr[0].z;
			float maxW = arr[0].w;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.w < minW)
				{
					minW = cur.w;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
				if (cur.w > maxW)
				{
					maxW = cur.w;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ, minW };
			accessor.Max = new List<double> { maxX, maxY, maxZ, maxW };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
#if USE_FAST_BINARY_WRITER
			_bufferWriter.Write(arr);
#else
			foreach (var vec in arr)
			{
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
				_bufferWriter.Write(vec.w);
			}
#endif
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorVector4ArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(Color[] arr, bool exportAlphaChannel)
		{
			exportAccessorMarker.Begin();
			exportAccessorColorArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = exportAlphaChannel ? GLTFAccessorAttributeType.VEC4 : GLTFAccessorAttributeType.VEC3;

			float minR = arr[0].r;
			float minG = arr[0].g;
			float minB = arr[0].b;
			float minA = arr[0].a;
			float maxR = arr[0].r;
			float maxG = arr[0].g;
			float maxB = arr[0].b;
			float maxA = arr[0].a;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.r < minR)
				{
					minR = cur.r;
				}
				if (cur.g < minG)
				{
					minG = cur.g;
				}
				if (cur.b < minB)
				{
					minB = cur.b;
				}
				if (cur.a < minA)
				{
					minA = cur.a;
				}
				if (cur.r > maxR)
				{
					maxR = cur.r;
				}
				if (cur.g > maxG)
				{
					maxG = cur.g;
				}
				if (cur.b > maxB)
				{
					maxB = cur.b;
				}
				if (cur.a > maxA)
				{
					maxA = cur.a;
				}
			}

			if (exportAlphaChannel)
			{
				accessor.Min = new List<double> { minR, minG, minB, minA };
				accessor.Max = new List<double> { maxR, maxG, maxB, maxA };
			}
			else
			{
				accessor.Min = new List<double> { minR, minG, minB };
				accessor.Max = new List<double> { maxR, maxG, maxB };
			}

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			if(exportAlphaChannel)
			{
				foreach (var color in arr)
				{
					_bufferWriter.Write(color.r);
					_bufferWriter.Write(color.g);
					_bufferWriter.Write(color.b);
					_bufferWriter.Write(color.a);
				}
			}
			else
			{
				foreach (var color in arr)
				{
					_bufferWriter.Write(color.r);
					_bufferWriter.Write(color.g);
					_bufferWriter.Write(color.b);
				}
			}

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorColorArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		public BufferViewId ExportBufferView(byte[] bytes) {
			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);
			_bufferWriter.Write(bytes);
			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);
			return ExportBufferView((uint)byteOffset, (uint)byteLength);
		}

		private BufferViewId ExportBufferView(uint byteOffset, uint byteLength, uint byteStride = 0)
		{
			var bufferView = new BufferView
			{
				Buffer = _bufferId,
				ByteOffset = byteOffset,
				ByteLength = byteLength,
				ByteStride = byteStride
			};

			var id = new BufferViewId
			{
				Id = _root.BufferViews.Count,
				Root = _root
			};

			_root.BufferViews.Add(bufferView);

			return id;
		}
	}
}
