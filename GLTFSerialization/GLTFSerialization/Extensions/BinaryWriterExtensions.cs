using System.IO;
using GLTF.Math;

public static class BinaryWriterExtensions
{
	public static void Write(this BinaryWriter binaryWriter, Vector2 value)
	{
		binaryWriter.Write(value.X);
		binaryWriter.Write(value.Y);
	}

	public static void Write(this BinaryWriter binaryWriter, Vector3 value)
	{
		binaryWriter.Write(value.X);
		binaryWriter.Write(value.Y);
		binaryWriter.Write(value.Z);
	}

	public static void Write(this BinaryWriter binaryWriter, Vector4 value)
	{
		binaryWriter.Write(value.X);
		binaryWriter.Write(value.Y);
		binaryWriter.Write(value.Z);
		binaryWriter.Write(value.W);
	}

	public static void Write(this BinaryWriter binaryWriter, Quaternion value)
	{
		binaryWriter.Write(value.X);
		binaryWriter.Write(value.Y);
		binaryWriter.Write(value.Z);
		binaryWriter.Write(value.W);
	}

	public static void Write(this BinaryWriter binaryWriter, GLTF.Math.UShortVector4 value)
	{
		binaryWriter.Write(value.X);
		binaryWriter.Write(value.Y);
		binaryWriter.Write(value.Z);
		binaryWriter.Write(value.W);
	}

	public static void Write(this BinaryWriter binaryWriter, GLTF.Math.ByteVector4 value)
	{
		binaryWriter.Write(value.X);
		binaryWriter.Write(value.Y);
		binaryWriter.Write(value.Z);
		binaryWriter.Write(value.W);
	}

	public static void Write(this BinaryWriter binaryWriter, Color value)
	{
		binaryWriter.Write(value.R);
		binaryWriter.Write(value.G);
		binaryWriter.Write(value.B);
		binaryWriter.Write(value.A);
	}

	public static void Write(this BinaryWriter binaryWriter, Matrix4x4 value)
	{
		binaryWriter.Write(value.M11);
		binaryWriter.Write(value.M12);
		binaryWriter.Write(value.M13);
		binaryWriter.Write(value.M14);
		binaryWriter.Write(value.M21);
		binaryWriter.Write(value.M22);
		binaryWriter.Write(value.M23);
		binaryWriter.Write(value.M24);
		binaryWriter.Write(value.M31);
		binaryWriter.Write(value.M32);
		binaryWriter.Write(value.M33);
		binaryWriter.Write(value.M34);
		binaryWriter.Write(value.M41);
		binaryWriter.Write(value.M42);
		binaryWriter.Write(value.M43);
		binaryWriter.Write(value.M44);
	}
}
