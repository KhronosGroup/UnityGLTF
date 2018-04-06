using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using GLTF.Schema;
using VectorTypes;

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{

		private AccessorId ExportData(GLTFAccessorAttributeType type, GLTFComponentType componentType, int componentSize, int count, IEnumerable<object> min, IEnumerable<object> max, int byteLength, Action<BinaryWriter> writeData, string accessorName = null)
		{
			// The offset of the data must be aligned to a multiple of the component size.
			var position = checked((int)this._bufferWriter.BaseStream.Position);
			var alignedPosition = Align(position, componentSize);
			for (var i = position; i < alignedPosition; i++)
			{
				this._bufferWriter.Write(byte.MinValue);
			}

			var bufferViewId = this.ExportBufferView(alignedPosition, byteLength);
			var accessorId = this.ExportAccessor(bufferViewId, componentType, count, type, min, max, accessorName);

			writeData(this._bufferWriter);

			return accessorId;
		}

		private AccessorId ExportAccessor(BufferViewId bufferViewId, GLTFComponentType componentType, int count, GLTFAccessorAttributeType type, IEnumerable<object> min, IEnumerable<object> max, string name = null)
		{
			Accessor accessor = new Accessor
			{
				BufferView = bufferViewId,
				ComponentType = componentType,
				Count = count,
				Type = type,
				Min = (List<double>) min,
				Max = (List<double>) max,
				Name = name
			};

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};

			_root.Accessors.Add(accessor);

			return id;
		}

		private static int Align(int value, int size)
		{
			var remainder = value % size;
			return (remainder == 0 ? value : checked(value + size - remainder));
		}

		private AccessorId ExportData(IEnumerable<ushort> values)
		{
			return this.ExportData(
				GLTFAccessorAttributeType.SCALAR,
				GLTFComponentType.UnsignedShort,
				sizeof(ushort),
				values.Count(),
				null,
				null,
				sizeof(ushort) * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)));
		}

		private AccessorId ExportData(IEnumerable<float> values, bool minMax = false)
		{
			return this.ExportData(
				GLTFAccessorAttributeType.SCALAR,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				minMax ? new object[] { values.Min() } : null,
				minMax ? new object[] { values.Max() } : null,
				sizeof(float) * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)));
		}

		private AccessorId ExportData(IEnumerable<Vector2> values)
		{
			return this.ExportData(
				GLTFAccessorAttributeType.VEC2,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				null,
				null,
				sizeof(float) * 2 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)));
		}

		private AccessorId ExportData(IEnumerable<Vector3> values, bool minMax = false, string accessorName = null)
		{
			return this.ExportData(
				GLTFAccessorAttributeType.VEC3,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				minMax ? new object[] { values.Select(value => value.x).Min(), values.Select(value => value.y).Min(), values.Select(value => value.z).Min() } : null,
				minMax ? new object[] { values.Select(value => value.x).Max(), values.Select(value => value.y).Max(), values.Select(value => value.z).Max() } : null,
				sizeof(float) * 3 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
				accessorName);
		}

		private AccessorId ExportData(IEnumerable<Vector4> values)
		{
			return this.ExportData(
				GLTFAccessorAttributeType.VEC4,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				null,
				null,
				sizeof(float) * 4 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)));
		}

		private AccessorId ExportData(IEnumerable<Quaternion> values)
		{
			return this.ExportData(
				GLTFAccessorAttributeType.VEC4,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				null,
				null,
				sizeof(float) * 4 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)));
		}

		private AccessorId ExportColors(IEnumerable<Color> values)
		{
			if (values.All(color => color.a == 1.0f))
			{
				return this.ExportData(
					GLTFAccessorAttributeType.VEC3,
					GLTFComponentType.Float,
					sizeof(float),
					values.Count(),
					null,
					null,
					sizeof(float) * 3 * values.Count(),
					binaryWriter => values.Select(value => (Vector3)(Vector4)value).ForEach(value => binaryWriter.Write(value)));
			}
			else
			{
				return this.ExportData(
					GLTFAccessorAttributeType.VEC4,
					GLTFComponentType.Float,
					sizeof(float),
					values.Count(),
					null,
					null,
					sizeof(float) * 4 * values.Count(),
					binaryWriter => values.ForEach(value => binaryWriter.Write(value)));
			}
		}

		private AccessorId ExportData(IEnumerable<ByteVector4> values)
		{
			return this.ExportData(
				GLTFAccessorAttributeType.VEC4,
				GLTFComponentType.UnsignedByte,
				sizeof(byte),
				values.Count(),
				null,
				null,
				sizeof(byte) * 4 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)));
		}

		private AccessorId ExportData(IEnumerable<UShortVector4> values)
		{
			return this.ExportData(
				GLTFAccessorAttributeType.VEC4,
				GLTFComponentType.UnsignedShort,
				sizeof(ushort),
				values.Count(),
				null,
				null,
				sizeof(ushort) * 4 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)));
		}

		private AccessorId ExportData(IEnumerable<Matrix4x4> values)
		{
			return this.ExportData(
				GLTFAccessorAttributeType.MAT4,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				null,
				null,
				sizeof(float) * 16 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)));
		}
	}
}

public static class EnumerableExtensions
{
	public static void ForEach<T>(this IEnumerable<T> values, Action<T> action)
	{
		foreach (var value in values)
		{
			action(value);
		}
	}
}
