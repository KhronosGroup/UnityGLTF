using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GLTF.Schema;
using GLTF.Math;

namespace GLTF
{
	public class DataExporter
	{
		public static BufferViewId ExportBufferView(BufferId bufferId, int byteOffset, int byteLength, GLTFRoot root)
		{
			var bufferView = new BufferView
			{
				Buffer = bufferId,
				ByteOffset = byteOffset,
				ByteLength = byteLength,
			};

			var id = new BufferViewId
			{
				Id = root.BufferViews.Count,
				Root = root
			};

			root.BufferViews.Add(bufferView);

			return id;
		}

		public static AccessorId ExportAccessor(BufferViewId bufferViewId, GLTFComponentType componentType, int count, GLTFAccessorAttributeType type, IEnumerable<double> min, IEnumerable<double> max, GLTFRoot root, string name = null)
		{
			Accessor accessor = new Accessor
			{
				BufferView = bufferViewId,
				ComponentType = componentType,
				Count = count,
				Type = type,
				Min = min == null ? null : min.ToList<double>(),
				Max = max == null ? null : max.ToList<double>(),
				Name = name
			};

			var id = new AccessorId
			{
				Id = root.Accessors.Count,
				Root = root
			};

			root.Accessors.Add(accessor);

			return id;
		}

		public static AccessorId ExportData(GLTFAccessorAttributeType type, GLTFComponentType componentType, int componentSize,
			int count, IEnumerable<double> min, IEnumerable<double> max, int byteLength, Action<BinaryWriter> writeData,
				BufferId bufferId, GLTFRoot root, BinaryWriter bufferWriter, string accessorName = null)
		{
			// The offset of the data must be aligned to a multiple of the component size.
			var position = checked((int)bufferWriter.BaseStream.Position);
			var alignedPosition = Align(position, componentSize);
			for (var i = position; i < alignedPosition; i++)
			{
				bufferWriter.Write(byte.MinValue);
			}

			var bufferViewId = ExportBufferView(bufferId, alignedPosition, byteLength, root);
			var accessorId = ExportAccessor(bufferViewId, componentType, count, type, min, max, root, accessorName);

			writeData(bufferWriter);

			return accessorId;
		}

		public static int Align(int value, int size)
		{
			return (value + size - 1) / size * size;
		}

		public static AccessorId ExportData(IEnumerable<ushort> values, BufferId bufferId, GLTFRoot root, BinaryWriter bufferWriter)
		{
			return ExportData(
				GLTFAccessorAttributeType.SCALAR,
				GLTFComponentType.UnsignedShort,
				sizeof(ushort),
				values.Count(),
				null,
				null,
				sizeof(ushort) * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
				bufferId,
				root,
				bufferWriter);
		}

		public static AccessorId ExportData(IEnumerable<float> values, BufferId bufferId, GLTFRoot root, BinaryWriter bufferWriter, bool minMax = false)
		{
			return ExportData(
				GLTFAccessorAttributeType.SCALAR,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				minMax ? new double[] { values.Min() } : null,
				minMax ? new double[] { values.Max() } : null,
				sizeof(float) * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
				bufferId,
				root,
				bufferWriter);
		}

		public static AccessorId ExportData(IEnumerable<Vector2> values, BufferId bufferId, GLTFRoot root, BinaryWriter bufferWriter)
		{
			return ExportData(
				GLTFAccessorAttributeType.VEC2,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				null,
				null,
				sizeof(float) * 2 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
				bufferId,
				root,
				bufferWriter);
		}

		public static AccessorId ExportData(IEnumerable<Vector3> values, BufferId bufferId, GLTFRoot root, BinaryWriter bufferWriter, bool minMax = false, string accessorName = null)
		{
			return ExportData(
				GLTFAccessorAttributeType.VEC3,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				minMax ? new double[] { values.Select(value => value.X).Min(), values.Select(value => value.Y).Min(), values.Select(value => value.Z).Min() } : null,
				minMax ? new double[] { values.Select(value => value.X).Max(), values.Select(value => value.Y).Max(), values.Select(value => value.Z).Max() } : null,
				sizeof(float) * 3 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
				bufferId,
				root,
				bufferWriter,
				accessorName);
		}

		public static AccessorId ExportData(IEnumerable<Vector4> values, BufferId bufferId, GLTFRoot root, BinaryWriter bufferWriter)
		{
			return ExportData(
				GLTFAccessorAttributeType.VEC4,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				null,
				null,
				sizeof(float) * 4 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
				bufferId,
				root,
				bufferWriter);
		}

		public static AccessorId ExportData(IEnumerable<Quaternion> values, BufferId bufferId, GLTFRoot root, BinaryWriter bufferWriter)
		{
			return ExportData(
				GLTFAccessorAttributeType.VEC4,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				null,
				null,
				sizeof(float) * 4 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
				bufferId,
				root,
				bufferWriter);
		}

		public static AccessorId ExportColors(IEnumerable<Color> values, BufferId bufferId, GLTFRoot root, BinaryWriter bufferWriter)
		{
			if (values.All(color => color.A == 1.0f))
			{
				return ExportData(
					GLTFAccessorAttributeType.VEC3,
					GLTFComponentType.Float,
					sizeof(float),
					values.Count(),
					null,
					null,
					sizeof(float) * 3 * values.Count(),
					binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
					bufferId,
					root,
					bufferWriter);
			}
			else
			{
				return ExportData(
					GLTFAccessorAttributeType.VEC4,
					GLTFComponentType.Float,
					sizeof(float),
					values.Count(),
					null,
					null,
					sizeof(float) * 4 * values.Count(),
					binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
					bufferId,
					root,
					bufferWriter);
			}
		}

		public static AccessorId ExportData(IEnumerable<GLTF.Math.ByteVector4> values, BufferId bufferId, GLTFRoot root, BinaryWriter bufferWriter)
		{
			return ExportData(
				GLTFAccessorAttributeType.VEC4,
				GLTFComponentType.UnsignedByte,
				sizeof(byte),
				values.Count(),
				null,
				null,
				sizeof(byte) * 4 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
				bufferId,
				root,
				bufferWriter);
		}

		public static AccessorId ExportData(IEnumerable<GLTF.Math.UShortVector4> values, BufferId bufferId, GLTFRoot root, BinaryWriter bufferWriter)
		{
			return ExportData(
				GLTFAccessorAttributeType.VEC4,
				GLTFComponentType.UnsignedShort,
				sizeof(ushort),
				values.Count(),
				null,
				null,
				sizeof(ushort) * 4 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
				bufferId,
				root,
				bufferWriter);
		}

		public static AccessorId ExportData(IEnumerable<Matrix4x4> values, BufferId bufferId, GLTFRoot root, BinaryWriter bufferWriter)
		{
			return ExportData(
				GLTFAccessorAttributeType.MAT4,
				GLTFComponentType.Float,
				sizeof(float),
				values.Count(),
				null,
				null,
				sizeof(float) * 16 * values.Count(),
				binaryWriter => values.ForEach(value => binaryWriter.Write(value)),
				bufferId,
				root,
				bufferWriter);
		}
	}

	internal static class EnumerableExtensions
	{
		internal static void ForEach<T>(this IEnumerable<T> values, Action<T> action)
		{
			foreach (var value in values)
			{
				action(value);
			}
		}
	}
}


