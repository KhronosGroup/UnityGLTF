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
				GLBChunkInfo jsonChunkInfo = ParseJsonChunkHeader(stream, startPosition);
				ThrowIfUnsupportedChunkEncoding(jsonChunkInfo);
			}
			else
			{
				stream.Position = startPosition;
			}

			gltfRoot = GLTFRoot.Deserialize(new StreamReader(stream));
			gltfRoot.IsGLB = isGLB;
		}
		
		/// <summary>
		/// Moves stream position to the data inside of the binary chunk with the given index, after the chunk header.
		/// Throws an exception if the headers are invalid or the chunk index is beyond the number of chunks in the stream.
		/// </summary>
		public static GLBChunkInfo SeekToBinaryChunkData(Stream stream, int binaryChunkIndex, long startPosition = 0)
		{
			stream.Position = startPosition + 4; // Start after "glTF" magic number.
			GLBHeader glbHeader = ParseGLBHeader(stream); // 4 -> 12 or 16
			long alignmentBitmask = glbHeader.GetAlignmentBitmask();
			long chunkHeaderSize = glbHeader.GetChunkHeaderSize();
			GLBChunkInfo chunkInfo = new GLBChunkInfo();
			for (int i = 0; i < binaryChunkIndex + 1; i++)
			{
				if (i > 0)
				{
					long paddedChunkLength = (chunkInfo.Length + alignmentBitmask) & ~alignmentBitmask; // Align to 4 or 8 bytes.
					stream.Position += paddedChunkLength; // Seek over the previous chunk's data plus padding.
				}
				if (stream.Position + chunkHeaderSize > stream.Length)
				{
					throw new GLTFHeaderInvalidException("There are no more chunks in the stream to read. The GLB file only has " + i + " chunks, but the requested chunk index was " + binaryChunkIndex + ".");
				}
				chunkInfo = ParseChunkInfo(stream, glbHeader.Version);
				if (stream.Position + chunkInfo.Length > stream.Length)
				{
					throw new GLTFHeaderInvalidException("Chunk length exceeds stream length.");
				}
			}
			return chunkInfo;
		}

		public static GLBHeader ParseGLBHeader(Stream stream)
		{
			uint version = GetUInt32(stream); // 4 -> 8
			long length;
			if (version == 2)
			{
				length = GetUInt32(stream); // 8 -> 12
			}
			else if (version == 3)
			{
				length = GetLength64(stream, "file"); // 8 -> 16
			}
			else
			{
				throw new GLTFHeaderInvalidException("Unsupported glTF binary format version " + version + ". Only versions 2 and 3 are supported.");
			}
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

		public static GLBChunkInfo ParseChunkInfo(Stream stream, uint glbVersion)
		{
			GLBChunkInfo chunkInfo = new GLBChunkInfo
			{
				StartPosition = stream.Position
			};
			// Binary format version 2 and 3 have different chunk header sizes and field order.
			if (glbVersion == 2)
			{
				chunkInfo.Length = GetUInt32(stream);
				chunkInfo.Type = (GLBChunkFormat)GetUInt32(stream);
				chunkInfo.Encoding = 0;
			}
			else if (glbVersion == 3)
			{
				chunkInfo.Type = (GLBChunkFormat)GetUInt32(stream);
				chunkInfo.Encoding = GetUInt32(stream);
				chunkInfo.Length = GetLength64(stream, "chunk");
			}
			else
			{
				throw new GLTFHeaderInvalidException("Unsupported glTF binary format version " + glbVersion + ". Only versions 2 and 3 are supported.");
			}
			return chunkInfo;
		}

		public static List<GLBChunkInfo> FindChunks(Stream stream, long startPosition = 0)
		{
			stream.Position = startPosition + 4; // Start after "glTF" magic number.
			GLBHeader glbHeader = ParseGLBHeader(stream);
			long alignmentBitmask = glbHeader.GetAlignmentBitmask();
			List<GLBChunkInfo> allChunks = new List<GLBChunkInfo>();

			// we only need to search for top two chunks (the JSON and binary chunks are guaranteed to be the top two chunks)
			// other chunks can be in the file but we do not care about them
			for (int i = 0; i < 2; ++i)
			{
				if (stream.Position >= stream.Length)
				{
					break;
				}

				GLBChunkInfo chunkInfo = ParseChunkInfo(stream, glbHeader.Version);
				allChunks.Add(chunkInfo);
				long paddedChunkLength = (chunkInfo.Length + alignmentBitmask) & ~alignmentBitmask; // Align to 4 or 8 bytes.
				stream.Position += paddedChunkLength;
			}

			return allChunks;
		}

		private static GLBChunkInfo ParseJsonChunkHeader(Stream stream, long startPosition)
		{
			GLBHeader glbHeader = ParseGLBHeader(stream); // 4 -> 12 or 16
			if (glbHeader.Version != 2 && glbHeader.Version != 3)
			{
				throw new GLTFHeaderInvalidException("Unsupported glTF version");
			};

			if (glbHeader.FileLength > (stream.Length - startPosition))
			{
				throw new GLTFHeaderInvalidException("File length does not match GLB file header declared length.");
			}

			long alignmentBitmask = glbHeader.GetAlignmentBitmask();
			while (true)
			{
				if (stream.Position + glbHeader.GetChunkHeaderSize() > stream.Length)
				{
					throw new GLTFHeaderInvalidException("GLB file has no JSON chunk");
				}

				GLBChunkInfo chunkInfo = ParseChunkInfo(stream, glbHeader.Version); // 12 -> 20 or 16 -> 32
				if (chunkInfo.Type == GLBChunkFormat.JSON)
				{
					return chunkInfo;
				}

				// In binary format version 2, the JSON chunk must be the first chunk.
				// In version 3, the first chunk that is a JSON chunk is the glTF JSON chunk, other chunks may precede it.
				if (glbHeader.Version == 2)
				{
					throw new GLTFHeaderInvalidException("First chunk must be of type JSON");
				}

				long paddedChunkLength = (chunkInfo.Length + alignmentBitmask) & ~alignmentBitmask; // Align to 8 bytes.
				stream.Position += paddedChunkLength;
			}
		}

		public static void ThrowIfUnsupportedChunkEncoding(GLBChunkInfo chunkInfo)
		{
			// Only support plainly encoded chunks for now. If we want to support compressed or encrypted
			// chunks, we need to decompress or decrypt them at the call sites of this function.
			if (chunkInfo.Encoding != 0)
			{
				throw new GLTFHeaderInvalidException("Unsupported encoding '" + UInt32ToAsciiString(chunkInfo.Encoding) + "' (0x" + chunkInfo.Encoding.ToString("X") + ") found. Only plain encoding (0x00000000) is supported.");
			}
		}

		private static string UInt32ToAsciiString(uint number)
		{
			string str = "";
			for (int i = 0; i < 4; i++)
			{
				byte lowByte = (byte)number;
				if (lowByte > 0x1F && lowByte < 0x7F)
				{
					str += (char)lowByte;
				}
				else
				{
					str += '?';
				}
				number >>= 8;
			}
			return str;
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

		/// <summary>
		/// Reads a 64-bit length of binary format version 3, whose most significant bit must be 0.
		/// </summary>
		private static long GetLength64(Stream stream, string lengthName)
		{
			ulong length = GetUInt64(stream);
			if (length > long.MaxValue)
			{
				throw new GLTFHeaderInvalidException("Invalid GLB " + lengthName + " length: the most significant bit must be 0.");
			}
			return (long)length;
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

