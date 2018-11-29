using GLTF.Schema;
using System.Collections.Generic;
using System.IO;

namespace GLTF
{
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
		public ChunkInfo JsonChunkInfo { get { return _jsonChunkInfo; } internal set { _jsonChunkInfo = value; } }

		/// <summary>
		/// Information on Binary chunk
		/// </summary>
		public ChunkInfo BinaryChunkInfo { get { return _binaryChunkInfo; } internal set { _binaryChunkInfo = value; } }

		private GLBHeader _glbHeader;
		private ChunkInfo _jsonChunkInfo;
		private ChunkInfo _binaryChunkInfo;

		internal GLBObject()
		{
		}

		internal void SetFileLength(uint newHeaderLength)
		{
			_glbHeader.FileLength = newHeaderLength;
		}

		internal void SetJsonChunkStartPosition(long startPosition)
		{
			_jsonChunkInfo.StartPosition = startPosition;
		}

		internal void SetJsonChunkLength(uint jsonChunkLength)
		{
			_jsonChunkInfo.Length = jsonChunkLength;
		}

		internal void SetBinaryChunkStartPosition(long startPosition)
		{
			_binaryChunkInfo.StartPosition = startPosition;
		}

		internal void SetBinaryChunkLength(uint binaryChunkLength)
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
