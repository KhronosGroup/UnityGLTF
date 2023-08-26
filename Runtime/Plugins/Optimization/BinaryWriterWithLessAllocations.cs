using System;
using System.IO;
using UnityEngine;

namespace UnityGLTF
{
	// It seems that BinaryWriter creates a ton of garbage: the implementation allocates a byte[4] for every float write. (and for double as well)
	// foreach (var v in arr)
	// {
	//     _bufferWriter.Write(v); // this will allocate 36B per call
	// }
	// For large accessors, this adds up to hundreds of megabytes of garbage.
	// This class adds special implementations that skip BitConverterLE and instead convert with the same logic,
	// differentiating between LittleEndian and BigEndian.

	// Following https://github.com/mono/mono/blob/4a5ffcabd58d6439e60126f46e0063bcf30e7a47/mcs/class/referencesource/mscorlib/system/io/binarywriter.cs#L381
	// Here's the 4-byte allocation we're seeing: https://github.com/mono/mono/blob/4a5ffcabd58d6439e60126f46e0063bcf30e7a47/mcs/class/System.ServiceModel/Mono.Security.Protocol.Ntlm/BitConverterLE.cs#L127
	// Another discussion of this problem: https://forum.unity.com/threads/binarywriter-floats.1108478/
	// The implementations here pull BitConverter.IsLittleEndian out of the loop and directly write converted bytes into the BinaryWriter.
	internal class BinaryWriterWithLessAllocations : BinaryWriter
	{
		private static readonly byte[] _buffer = new byte[16];    // temp space for writing primitives to.

		public BinaryWriterWithLessAllocations(Stream binStream) : base(binStream) { }

		public unsafe void Write(float[] value)
		{
			if (BitConverter.IsLittleEndian)
			{
				foreach (var v in value)
				{
					uint tmpValue = *(uint *)&v;
					_buffer[0] = (byte) (tmpValue);
					_buffer[1] = (byte) (tmpValue >> 8);
					_buffer[2] = (byte) (tmpValue >> 16);
					_buffer[3] = (byte) (tmpValue >> 24);
					Write(_buffer, 0, 4);
				}
			}
			else
			{
				foreach (var v in value)
				{
					uint tmpValue = *(uint *)&v;
					_buffer[0] = (byte) (tmpValue >> 24);
					_buffer[1] = (byte) (tmpValue >> 16);
					_buffer[2] = (byte) (tmpValue >> 8);
					_buffer[3] = (byte) (tmpValue);
					Write(_buffer, 0, 4);
				}
			}
		}

		public override unsafe void Write(float value)
		{
			if (BitConverter.IsLittleEndian)
			{
				uint tmpValue = *(uint *)&value;
				_buffer[0] = (byte) (tmpValue);
				_buffer[1] = (byte) (tmpValue >> 8);
				_buffer[2] = (byte) (tmpValue >> 16);
				_buffer[3] = (byte) (tmpValue >> 24);
				Write(_buffer, 0, 4);
			}
			else
			{
				uint tmpValue = *(uint *)&value;
				_buffer[0] = (byte) (tmpValue >> 24);
				_buffer[1] = (byte) (tmpValue >> 16);
				_buffer[2] = (byte) (tmpValue >> 8);
				_buffer[3] = (byte) (tmpValue);
				Write(_buffer, 0, 4);
			}
		}

		public unsafe void Write(Vector4[] arr)
		{
			if (BitConverter.IsLittleEndian)
			{
				foreach (var v0 in arr)
				{
					float vx = v0.x;
					float vy = v0.y;
					float vz = v0.z;
					float vw = v0.w;

					uint tmpValue = *(uint *)&vx;
					_buffer[ 0] = (byte) (tmpValue);
					_buffer[ 1] = (byte) (tmpValue >> 8);
					_buffer[ 2] = (byte) (tmpValue >> 16);
					_buffer[ 3] = (byte) (tmpValue >> 24);

					tmpValue = *(uint *)&vy;
					_buffer[ 4] = (byte) (tmpValue);
					_buffer[ 5] = (byte) (tmpValue >> 8);
					_buffer[ 6] = (byte) (tmpValue >> 16);
					_buffer[ 7] = (byte) (tmpValue >> 24);

					tmpValue = *(uint *)&vz;
					_buffer[ 8] = (byte) (tmpValue);
					_buffer[ 9] = (byte) (tmpValue >> 8);
					_buffer[10] = (byte) (tmpValue >> 16);
					_buffer[11] = (byte) (tmpValue >> 24);

					tmpValue = *(uint *)&vw;
					_buffer[12] = (byte) (tmpValue);
					_buffer[13] = (byte) (tmpValue >> 8);
					_buffer[14] = (byte) (tmpValue >> 16);
					_buffer[15] = (byte) (tmpValue >> 24);


					Write(_buffer, 0, 16);
				}
			}
			else
			{
				foreach (var v0 in arr)
				{
					float vx = v0.x;
					float vy = v0.y;
					float vz = v0.z;
					float vw = v0.w;

					uint tmpValue = *(uint *)&vx;
					_buffer[ 0] = (byte) (tmpValue >> 24);
					_buffer[ 1] = (byte) (tmpValue >> 16);
					_buffer[ 2] = (byte) (tmpValue >> 8);
					_buffer[ 3] = (byte) (tmpValue);

					tmpValue = *(uint *)&vy;
					_buffer[ 4] = (byte) (tmpValue >> 24);
					_buffer[ 5] = (byte) (tmpValue >> 16);
					_buffer[ 6] = (byte) (tmpValue >> 8);
					_buffer[ 7] = (byte) (tmpValue);

					tmpValue = *(uint *)&vz;
					_buffer[ 8] = (byte) (tmpValue >> 24);
					_buffer[ 9] = (byte) (tmpValue >> 16);
					_buffer[10] = (byte) (tmpValue >> 8);
					_buffer[11] = (byte) (tmpValue);

					tmpValue = *(uint *)&vw;
					_buffer[12] = (byte) (tmpValue >> 24);
					_buffer[13] = (byte) (tmpValue >> 16);
					_buffer[14] = (byte) (tmpValue >> 8);
					_buffer[15] = (byte) (tmpValue);


					Write(_buffer, 0, 16);
				}
			}
		}

		public unsafe void Write(Vector3[] arr)
		{
			if (BitConverter.IsLittleEndian)
			{
				foreach (var v0 in arr)
				{
					float vx = v0.x;
					float vy = v0.y;
					float vz = v0.z;

					uint tmpValue = *(uint *)&vx;
					_buffer[ 0] = (byte) (tmpValue);
					_buffer[ 1] = (byte) (tmpValue >> 8);
					_buffer[ 2] = (byte) (tmpValue >> 16);
					_buffer[ 3] = (byte) (tmpValue >> 24);

					tmpValue = *(uint *)&vy;
					_buffer[ 4] = (byte) (tmpValue);
					_buffer[ 5] = (byte) (tmpValue >> 8);
					_buffer[ 6] = (byte) (tmpValue >> 16);
					_buffer[ 7] = (byte) (tmpValue >> 24);

					tmpValue = *(uint *)&vz;
					_buffer[ 8] = (byte) (tmpValue);
					_buffer[ 9] = (byte) (tmpValue >> 8);
					_buffer[10] = (byte) (tmpValue >> 16);
					_buffer[11] = (byte) (tmpValue >> 24);


					Write(_buffer, 0, 12);
				}
			}
			else
			{
				foreach (var v0 in arr)
				{
					float vx = v0.x;
					float vy = v0.y;
					float vz = v0.z;

					uint tmpValue = *(uint *)&vx;
					_buffer[ 0] = (byte) (tmpValue >> 24);
					_buffer[ 1] = (byte) (tmpValue >> 16);
					_buffer[ 2] = (byte) (tmpValue >> 8);
					_buffer[ 3] = (byte) (tmpValue);

					tmpValue = *(uint *)&vy;
					_buffer[ 4] = (byte) (tmpValue >> 24);
					_buffer[ 5] = (byte) (tmpValue >> 16);
					_buffer[ 6] = (byte) (tmpValue >> 8);
					_buffer[ 7] = (byte) (tmpValue);

					tmpValue = *(uint *)&vz;
					_buffer[ 8] = (byte) (tmpValue >> 24);
					_buffer[ 9] = (byte) (tmpValue >> 16);
					_buffer[10] = (byte) (tmpValue >> 8);
					_buffer[11] = (byte) (tmpValue);

					Write(_buffer, 0, 12);
				}
			}
		}

		public unsafe void Write(Quaternion[] arr)
		{
			if (BitConverter.IsLittleEndian)
			{
				foreach (var v0 in arr)
				{
					float vx = v0.x;
					float vy = v0.y;
					float vz = v0.z;
					float vw = v0.w;

					uint tmpValue = *(uint *)&vx;
					_buffer[ 0] = (byte) (tmpValue);
					_buffer[ 1] = (byte) (tmpValue >> 8);
					_buffer[ 2] = (byte) (tmpValue >> 16);
					_buffer[ 3] = (byte) (tmpValue >> 24);

					tmpValue = *(uint *)&vy;
					_buffer[ 4] = (byte) (tmpValue);
					_buffer[ 5] = (byte) (tmpValue >> 8);
					_buffer[ 6] = (byte) (tmpValue >> 16);
					_buffer[ 7] = (byte) (tmpValue >> 24);

					tmpValue = *(uint *)&vz;
					_buffer[ 8] = (byte) (tmpValue);
					_buffer[ 9] = (byte) (tmpValue >> 8);
					_buffer[10] = (byte) (tmpValue >> 16);
					_buffer[11] = (byte) (tmpValue >> 24);

					tmpValue = *(uint *)&vw;
					_buffer[12] = (byte) (tmpValue);
					_buffer[13] = (byte) (tmpValue >> 8);
					_buffer[14] = (byte) (tmpValue >> 16);
					_buffer[15] = (byte) (tmpValue >> 24);

					Write(_buffer, 0, 16);
				}
			}
			else
			{
				foreach (var v0 in arr)
				{
					float vx = v0.x;
					float vy = v0.y;
					float vz = v0.z;
					float vw = v0.w;

					uint tmpValue = *(uint *)&vx;
					_buffer[ 0] = (byte) (tmpValue >> 24);
					_buffer[ 1] = (byte) (tmpValue >> 16);
					_buffer[ 2] = (byte) (tmpValue >> 8);
					_buffer[ 3] = (byte) (tmpValue);

					tmpValue = *(uint *)&vy;
					_buffer[ 4] = (byte) (tmpValue >> 24);
					_buffer[ 5] = (byte) (tmpValue >> 16);
					_buffer[ 6] = (byte) (tmpValue >> 8);
					_buffer[ 7] = (byte) (tmpValue);

					tmpValue = *(uint *)&vz;
					_buffer[ 8] = (byte) (tmpValue >> 24);
					_buffer[ 9] = (byte) (tmpValue >> 16);
					_buffer[10] = (byte) (tmpValue >> 8);
					_buffer[11] = (byte) (tmpValue);

					tmpValue = *(uint *)&vw;
					_buffer[12] = (byte) (tmpValue >> 24);
					_buffer[13] = (byte) (tmpValue >> 16);
					_buffer[14] = (byte) (tmpValue >> 8);
					_buffer[15] = (byte) (tmpValue);

					Write(_buffer, 0, 16);
				}
			}
		}
	}
}
