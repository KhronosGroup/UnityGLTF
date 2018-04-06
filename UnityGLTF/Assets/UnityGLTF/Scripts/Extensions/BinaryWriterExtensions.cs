using System.IO;
using UnityEngine;
using VectorTypes;

namespace VectorTypes
{
	//New vector types
	public struct ByteVector4
	{
		public byte x;
		public byte y;
		public byte z;
		public byte w;

		public ByteVector4(byte x, byte y, byte z, byte w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
	}

	public struct UShortVector4
	{
		public ushort x;
		public ushort y;
		public ushort z;
		public ushort w;

		public UShortVector4(ushort x, ushort y, ushort z, ushort w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
	}

}

public static class BinaryWriterExtensions
{

	//writing vector types
	public static void Write(this BinaryWriter binaryWriter, Vector2 value)
	{
		binaryWriter.Write(value.x);
		binaryWriter.Write(value.y);
	}

	public static void Write(this BinaryWriter binaryWriter, Vector3 value)
	{
		binaryWriter.Write(value.x);
		binaryWriter.Write(value.y);
		binaryWriter.Write(value.z);
	}

	public static void Write(this BinaryWriter binaryWriter, Vector4 value)
	{
		binaryWriter.Write(value.x);
		binaryWriter.Write(value.y);
		binaryWriter.Write(value.z);
		binaryWriter.Write(value.w);
	}

	public static void Write(this BinaryWriter binaryWriter, Quaternion value)
	{
		binaryWriter.Write(value.x);
		binaryWriter.Write(value.y);
		binaryWriter.Write(value.z);
		binaryWriter.Write(value.w);
	}

	public static void Write(this BinaryWriter binaryWriter, UShortVector4 value)
	{
		binaryWriter.Write(value.x);
		binaryWriter.Write(value.y);
		binaryWriter.Write(value.z);
		binaryWriter.Write(value.w);
	}

	public static void Write(this BinaryWriter binaryWriter, ByteVector4 value)
	{
		binaryWriter.Write(value.x);
		binaryWriter.Write(value.y);
		binaryWriter.Write(value.z);
		binaryWriter.Write(value.w);
	}

	public static void Write(this BinaryWriter binaryWriter, Matrix4x4 value)
	{
		binaryWriter.Write(value[00]);
		binaryWriter.Write(value[01]);
		binaryWriter.Write(value[02]);
		binaryWriter.Write(value[03]);
		binaryWriter.Write(value[04]);
		binaryWriter.Write(value[05]);
		binaryWriter.Write(value[06]);
		binaryWriter.Write(value[07]);
		binaryWriter.Write(value[08]);
		binaryWriter.Write(value[09]);
		binaryWriter.Write(value[10]);
		binaryWriter.Write(value[11]);
		binaryWriter.Write(value[12]);
		binaryWriter.Write(value[13]);
		binaryWriter.Write(value[14]);
		binaryWriter.Write(value[15]);
	}
}
