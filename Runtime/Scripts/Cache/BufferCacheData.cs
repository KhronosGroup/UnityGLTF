using System;
using Unity.Collections;

namespace UnityGLTF.Cache
{
	public class BufferCacheData : IDisposable
	{
		public uint ChunkOffset { get; set; }
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
