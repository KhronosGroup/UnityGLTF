using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.IO;

namespace GLTF
{
	public class GLTFParser
	{
		public static void ParseJson(Stream stream, out GLTFRoot gltfRoot, long startPosition = 0)
		{
			stream.Position = startPosition;
			bool isGLB = IsGLB(stream);
			
			// Check for binary format magic bytes
			if (isGLB)
			{
				ParseJsonChunkAndFileHeader(stream, startPosition);
			}
			else
			{
				stream.Position = startPosition;
			}

			gltfRoot = GLTFRoot.Deserialize(new StreamReader(stream));
			gltfRoot.IsGLB = isGLB;
		}
		
		// todo: this needs reimplemented. There is no such thing as a binary chunk index, and the chunk may not be in 0, 1, 2 order
		// Moves stream position to binary chunk location
		public static GLBChunkInfo SeekToBinaryChunkData(Stream stream, int binaryChunkIndex, long startPosition = 0)
		{
			stream.Position = startPosition + 4; // Start after "glTF" magic number.
			GLBHeader glbHeader = ParseGLBHeader(stream);
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
			if (chunkOffset + chunkLength <= glbHeader.FileLength)
			{
				GLBChunkFormat chunkType = (GLBChunkFormat)GetUInt32(stream);
				if (chunkType != GLBChunkFormat.BIN)
				{
					throw new GLTFHeaderInvalidException("Second chunk must be of type BIN if present");
				}

				return new GLBChunkInfo
				{
					StartPosition = stream.Position - GLBHeader.GLB2_CHUNK_HEADER_SIZE,
					Length = chunkLength,
					Type = chunkType
				};
			}


			// Be aware that File length does not match header when MeshOpt compression is used!
			//throw new GLTFHeaderInvalidException("File length does not match chunk header.");

			return new GLBChunkInfo
			{
				StartPosition = stream.Position - GLBHeader.GLB2_CHUNK_HEADER_SIZE,
				Length = chunkLength,
				Type = GLBChunkFormat.BIN
			};
		}

		public static GLBHeader ParseGLBHeader(Stream stream)
		{
			uint version = GetUInt32(stream);   // 4
			uint length = GetUInt32(stream); // 8

			return new GLBHeader
			{
				Version = version,
				FileLength = length
			};
		}

		public static bool IsGLB(Stream stream)
		{
			return GetUInt32(stream) == GLBHeader.GLTF_MAGIC_NUMBER;
		}

		public static GLBChunkInfo ParseChunkInfo(Stream stream)
		{
			GLBChunkInfo chunkInfo = new GLBChunkInfo
			{
				StartPosition = stream.Position
			};

			chunkInfo.Length = GetUInt32(stream);					// 12
			chunkInfo.Type = (GLBChunkFormat)GetUInt32(stream);		// 16
			return chunkInfo;
		}

		public static List<GLBChunkInfo> FindChunks(Stream stream, long startPosition = 0)
		{
			stream.Position = startPosition + 4; // Start after "glTF" magic number.
			ParseGLBHeader(stream);
			List<GLBChunkInfo> allChunks = new List<GLBChunkInfo>();

			// we only need to search for top two chunks (the JSON and binary chunks are guaranteed to be the top two chunks)
			// other chunks can be in the file but we do not care about them
			for (int i = 0; i < 2; ++i)
			{
				if (stream.Position == stream.Length)
				{
					break;
				}

				GLBChunkInfo chunkInfo = ParseChunkInfo(stream);
				allChunks.Add(chunkInfo);
				stream.Position += chunkInfo.Length;
			}

			return allChunks;
		}

		private static void ParseJsonChunkAndFileHeader(Stream stream, long startPosition)
		{
			GLBHeader glbHeader = ParseGLBHeader(stream);  // 4, 8
			if (glbHeader.Version != 2)
			{
				throw new GLTFHeaderInvalidException("Unsupported glTF version");
			};

			if (glbHeader.FileLength > (stream.Length - startPosition))
			{
				throw new GLTFHeaderInvalidException("File length does not match GLB file header declared length.");
			}

			GLBChunkInfo chunkInfo = ParseChunkInfo(stream);
			if (chunkInfo.Type != GLBChunkFormat.JSON)
			{
				throw new GLTFHeaderInvalidException("First chunk must be of type JSON");
			}
		}

		private static uint GetUInt32(Stream stream)
		{
			var uintSize = sizeof(uint);
			byte[] headerBuffer = new byte[uintSize];
			int bytesRead = stream.Read(headerBuffer, 0, uintSize);
			if (bytesRead != uintSize)
			{
				throw new EndOfStreamException("Unexpected end of stream while reading uint32.");
			}
			return BitConverter.ToUInt32(headerBuffer, 0);
		}

		private static ulong GetUInt64(Stream stream)
		{
			var ulongSize = sizeof(ulong);
			byte[] headerBuffer = new byte[ulongSize];
			int bytesRead = stream.Read(headerBuffer, 0, ulongSize);
			if (bytesRead != ulongSize)
			{
				throw new EndOfStreamException("Unexpected end of stream while reading uint64.");
			}
			return BitConverter.ToUInt64(headerBuffer, 0);
		}
	}
}

