using System;
using Unity.Collections;

namespace UnityGLTF.Cache
{
	public class BufferCacheData : IDisposable
	{
		/// <summary>
		/// The offset in the stream where the chunk data starts, past the chunk header.
		/// </summary>
		public uint ChunkDataOffset { get; set; }
		public System.IO.Stream Stream { get; set; }

		public NativeArray<byte> bufferData { get; set; }

		public void Dispose()
		{
			if (Stream != null)
			{
#if !WINDOWS_UWP
				Stream.Close();
#else
				Stream.Dispose();
#endif				
				Stream = null;
			}
		}
	}
}
