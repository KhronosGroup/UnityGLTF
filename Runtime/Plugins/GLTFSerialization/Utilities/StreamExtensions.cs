namespace System.IO
{
	/// <summary>
	/// Adding .NET CopyTo as extension method for .NET 3.5 support
	/// </summary>
	internal static class StreamExtensions
	{
		// We pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
		// The CopyTo/CopyToAsync buffer is short-lived and is likely to be collected at Gen0, and it offers a significant
		// improvement in Copy performance.
		private const int _DefaultCopyBufferSize = 81920;
		private static readonly byte[] _CopyBuffer = new byte[_DefaultCopyBufferSize];

#if !NETFX_CORE
		public static void CopyTo(this Stream source, Stream destination)
		{
			int highwaterMark = 0;
			try
			{
				int read;
				while ((read = source.Read(_CopyBuffer, 0, _CopyBuffer.Length)) != 0)
				{
					if (read > highwaterMark) highwaterMark = read;
					destination.Write(_CopyBuffer, 0, read);
				}
			}
			finally
			{
				Array.Clear(_CopyBuffer, 0, highwaterMark); // clear only the most we used
			}
		}
#endif

		/// <summary>
		/// Implementation to copy a fixed amount of data to self in stream
		/// </summary>
		/// <param name="source">The stream to modify</param>
		/// <param name="destinationOffset">Offset into stream to copy</param>
		/// <param name="amountToCopy">Amount of stream to copy</param>
		/// <param name="bufferSize">Size of array to use for each copy</param>
		public static void CopyToSelf(this Stream source, int destinationOffset, uint amountToCopy)
		{
			if (destinationOffset <= source.Position) throw new NotImplementedException("desintation offset must be larger than source offset");

			int highwaterMark = 0;
			long initialOffset = source.Position;
			try
			{
				int read;
				int amountToRead;
				while ((source.Position = initialOffset + amountToCopy - (amountToRead = (int)Math.Min(_CopyBuffer.Length, amountToCopy))) >= 0 && (read = source.Read(_CopyBuffer, 0, amountToRead)) != 0)
				{
					if (read > highwaterMark) highwaterMark = read;
					source.Position = destinationOffset + amountToCopy - read;
					source.Write(_CopyBuffer, 0, read);
					amountToCopy -= (uint)read;
				}
			}
			finally
			{
				Array.Clear(_CopyBuffer, 0, highwaterMark); // clear only the most we used
			}
		}
	}
}
