using System;
using System.IO;
using GLTF.Schema;

namespace GLTF
{
	public class GLTFParser
	{

		private enum ChunkFormat : uint
		{
			JSON = 0x4e4f534a,
			BIN = 0x004e4942
		}

		internal struct GLBHeader
		{
			public uint Version { get; set; }
			public uint FileLength { get; set; }
		}
		
		public static GLTFRoot ParseJson(Stream stream)
		{
            stream.Position = 0;
            // Check for binary format magic bytes
            if (IsGLB(stream))
			{
				ParseJsonChunk(stream);
			}
			else
			{
				stream.Position = 0;
			}

			return GLTFRoot.Deserialize(new StreamReader(stream));
		}
		
		// Moves stream position to binary chunk location
		public static void SeekToBinaryChunk(Stream stream, int binaryChunkIndex)
		{
			stream.Position = 4;	 // start after magic number chunk
			GLBHeader header = ParseGLBHeader(stream);
			uint chunkOffset = 12;   // sizeof(GLBHeader) + magic number
			uint chunkLength = 0;
			for (int i = 0; i < binaryChunkIndex + 2; ++i)
			{
				chunkOffset += chunkLength;
				stream.Position = chunkOffset;
				chunkLength = GetUInt32(stream);
				chunkOffset += 8;   // to account for chunk length (4 bytes) and type (4 bytes)
			}

			// Load Binary Chunk
			if (chunkOffset + chunkLength <= header.FileLength)
			{
				uint chunkType = GetUInt32(stream);
				if (chunkType != (uint)ChunkFormat.BIN)
				{
					throw new GLTFHeaderInvalidException("Second chunk must be of type BIN if present");
				}
			}
			else
			{
				throw new GLTFHeaderInvalidException("File length does not match chunk header.");
			}
		}

		private static GLBHeader ParseGLBHeader(Stream stream)
		{
			uint version = GetUInt32(stream);   // 4
			uint length = GetUInt32(stream); // 8

			return new GLBHeader
			{
				Version = version,
				FileLength = length
			};
		}

		private static bool IsGLB(Stream stream)
		{
			return GetUInt32(stream) == 0x46546c67;  // 0
		}

		private static void ParseJsonChunk(Stream stream)
		{
			GLBHeader header = ParseGLBHeader(stream);  // 4, 8
			if (header.Version != 2)
			{
				throw new GLTFHeaderInvalidException("Unsupported glTF version");
			};

			if (header.FileLength != stream.Length)
			{
				throw new GLTFHeaderInvalidException("File length does not match header.");
			}

			int chunkLength = (int)GetUInt32(stream);   // 12
			var chunkType = GetUInt32(stream);		  // 16
			if (chunkType != (uint)ChunkFormat.JSON)
			{
				throw new GLTFHeaderInvalidException("First chunk must be of type JSON");
			}
		}

		private static uint GetUInt32(Stream stream)
		{
			var uintSize = sizeof(uint);
			byte[] headerBuffer = new byte[uintSize];
			stream.Read(headerBuffer, 0, uintSize);
			return BitConverter.ToUInt32(headerBuffer, 0);
		}
	}
}

