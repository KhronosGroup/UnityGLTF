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
		
		public static GLTFRoot ParseJson(byte[] gltfBinary)
		{
			string gltfContent;

			if (gltfBinary.Length == 0) {
				throw new GLTFHeaderInvalidException("glTF file cannot be empty.");
			}

			// Check for binary format magic bytes
			if (IsGLB(gltfBinary))
			{
				gltfContent = ParseJsonChunk(gltfBinary);
			}
			else
			{
				gltfContent = System.Text.Encoding.UTF8.GetString(gltfBinary);
			}

			return ParseString(gltfContent);
		}

		// todo optimize: this can have a caching system where GLTFRoot stores data about JSON offset of a GLB
		public static void ExtractBinaryChunk(byte[] gltfBinary, int binaryChunkIndex, out byte[] glbBuffer)
		{
			GLBHeader header = ParseGLBHeader(gltfBinary);
			int chunkOffset = 12;   // sizeof(GLBHeader) + magic number
			int chunkLength = 0;
			for (int i = 0; i < binaryChunkIndex + 2; ++i)
			{
				chunkOffset += chunkLength;
				chunkLength = (int)GetChunkLength(gltfBinary, chunkOffset);
				chunkOffset += 8;   // to account for chunk length (4 bytes) and type (4 bytes)
			}

			// Load Binary Chunk
			if (chunkOffset + chunkLength <= header.FileLength)
			{
				uint chunkType = BitConverter.ToUInt32(gltfBinary, chunkOffset - 4);
				if (chunkType != (uint)ChunkFormat.BIN)
				{
					throw new GLTFHeaderInvalidException("Second chunk must be of type BIN if present");
				}

				glbBuffer = new byte[chunkLength];
				System.Buffer.BlockCopy(gltfBinary, chunkOffset, glbBuffer, 0, chunkLength);
			}
			else
			{
				throw new GLTFHeaderInvalidException("File length does not match chunk header.");
			}
		}

		private static GLBHeader ParseGLBHeader(byte[] gltfBinary)
		{
			uint version = BitConverter.ToUInt32(gltfBinary, 4);
			uint length = BitConverter.ToUInt32(gltfBinary, 8);

			return new GLBHeader
			{
				Version = version,
				FileLength = length
			};
		}

		private static bool IsGLB(byte[] gltfBinary)
		{
			return BitConverter.ToUInt32(gltfBinary, 0) == 0x46546c67;
		}

		private static uint GetChunkLength(byte[] gltfBinary, int chunkOffset)
		{
			return BitConverter.ToUInt32(gltfBinary, chunkOffset);
		}

		private static string ParseJsonChunk(byte[] gltfBinary)
		{
			GLBHeader header = ParseGLBHeader(gltfBinary);
			if (header.Version != 2)
			{
				throw new GLTFHeaderInvalidException("Unsupported glTF version");
			};

			if (header.FileLength != gltfBinary.Length)
			{
				throw new GLTFHeaderInvalidException("File length does not match header.");
			}

			uint chunkLength = GetChunkLength(gltfBinary, 12);
			var chunkType = BitConverter.ToUInt32(gltfBinary, 16);
			if (chunkType != (uint)ChunkFormat.JSON)
			{
				throw new GLTFHeaderInvalidException("First chunk must be of type JSON");
			}

			// Load JSON chunk
			return System.Text.Encoding.UTF8.GetString(gltfBinary, 20, (int)chunkLength);
		}

		private static GLTFRoot ParseString(string gltfContent)
		{
			var stringReader = new StringReader(gltfContent);
			return GLTFRoot.Deserialize(stringReader);
		}
	}
}

