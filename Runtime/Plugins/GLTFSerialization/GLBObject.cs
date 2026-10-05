using GLTF.Schema;
using System.Collections.Generic;
using System.IO;

namespace GLTF
{
	public enum GLBChunkFormat : uint
	{
		JSON = 0x4e4f534a, // ASCII string "JSON" in little-endian order.
		BIN = 0x004e4942 // ASCII string "BIN\0" in little-endian order.
	}

	/// <summary>
	/// Information containing parsed GLB Header
	/// </summary>
	public struct GLBHeader
	{
		public uint Version { get; set; }
		public long FileLength { get; set; }

		public long GetFileHeaderSize() => Version == 2 ? 12 : 16;
		public long GetChunkHeaderSize() => Version == 2 ? 8 : 16;
		public long GetAlignment() => Version == 2 ? 4 : 8;
		public long GetAlignmentBitmask() => Version == 2 ? 3 : 7;

		/// <summary>
		/// ASCII string "glTF" in little-endian order.
		/// </summary>
		public static readonly uint GLTF_MAGIC_NUMBER = 0x46546C67;
	}

	/// <summary>
	/// Information that contains parsed chunk
	/// </summary>
	public struct GLBChunkInfo
	{
		public long StartPosition;
		public long Length;
		public GLBChunkFormat Type;
		public uint Encoding;
	}

	/// <summary>
	/// Objects containing GLB data and associated parsing information
	/// </summary>
	public class GLBObject : IGLTFObject
	{
		public GLBObject(GLBObject other)
		{
			Root = other.Root;
			Header = other.Header;
			StreamStartPosition = other.StreamStartPosition;
			JsonChunkInfo = other.JsonChunkInfo;
			BinaryChunkInfo = other.BinaryChunkInfo;
		}

		/// <summary>
		/// Parsed JSON of the GLB
		/// </summary>
		public GLTFRoot Root { get; internal set; }

		/// <summary>
		/// Read/Write Stream that GLB exists in
		/// </summary>
		public Stream Stream { get; set; }

		/// <summary>
		/// Header of GLB
		/// </summary>
		public GLBHeader Header { get { return _glbHeader; } internal set { _glbHeader = value; } }
		
		/// <summary>
		/// Start position for the GLB stream
		/// </summary>
		public long StreamStartPosition { get; internal set; }

		/// <summary>
		/// Information on JSON chunk
		/// </summary>
		public GLBChunkInfo JsonChunkInfo { get { return _jsonChunkInfo; } internal set { _jsonChunkInfo = value; } }

		/// <summary>
		/// Information on Binary chunk
		/// </summary>
		public GLBChunkInfo BinaryChunkInfo { get { return _binaryChunkInfo; } internal set { _binaryChunkInfo = value; } }

		private GLBHeader _glbHeader;
		private GLBChunkInfo _jsonChunkInfo;
		private GLBChunkInfo _binaryChunkInfo;

		internal GLBObject()
		{
		}

		internal void SetFileLength(long newHeaderLength)
		{
			_glbHeader.FileLength = newHeaderLength;
		}

		internal void SetJsonChunkStartPosition(long startPosition)
		{
			_jsonChunkInfo.StartPosition = startPosition;
		}

		internal void SetJsonChunkLength(long jsonChunkLength)
		{
			_jsonChunkInfo.Length = jsonChunkLength;
		}

		internal void SetBinaryChunkStartPosition(long startPosition)
		{
			_binaryChunkInfo.StartPosition = startPosition;
		}

		internal void SetBinaryChunkLength(long binaryChunkLength)
		{
			_binaryChunkInfo.Length = binaryChunkLength;
			if (Root.Buffers == null)
			{
				Root.Buffers = new List<GLTFBuffer>();
			}

			if (Root.Buffers.Count == 0)
			{
				Root.Buffers.Add(new GLTFBuffer
				{
					ByteLength = binaryChunkLength
				});
			}
			else
			{
				Root.Buffers[0].ByteLength = binaryChunkLength;
			}
		}
	}

}
