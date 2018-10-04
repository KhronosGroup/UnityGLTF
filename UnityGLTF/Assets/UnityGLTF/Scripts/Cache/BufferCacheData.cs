using System;

namespace UnityGLTF.Cache
{
	public class BufferCacheData : IDisposable
	{
		public uint ChunkOffset { get; set; }
		public System.IO.Stream Stream { get; set; }

		public void Dispose()
		{
			if (Stream != null)
			{
				Stream.Dispose();
				Stream = null;
			}
		}
	}
}
