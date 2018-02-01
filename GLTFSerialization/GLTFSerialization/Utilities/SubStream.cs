/// <summary>
/// Implementation from: https://social.msdn.microsoft.com/Forums/vstudio/en-US/c409b63b-37df-40ca-9322-458ffe06ea48/how-to-access-part-of-a-filestream-or-memorystream?forum=netfxbcl 
/// </summary>

using System;
using System.IO;

namespace GLTF.Utilities
{
	/// <summary>
	/// Allows only part of a stream to be accessed
	/// Prevents reading of the stream past the desired length
	/// SubStream start is based on input position
	/// </summary>
	public class SubStream : Stream
	{
		private Stream _baseStream;
		private readonly long _length;
		private long _position;

		public override bool CanRead
		{
			get
			{
				CheckDisposed();
				return _baseStream.CanRead;
			}
		}

		public override bool CanSeek
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		public override long Length
		{
			get
			{
				CheckDisposed();
				return _length;
			}
		}

		public override long Position
		{
			get
			{
				CheckDisposed();
				return _position;
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public SubStream(Stream baseStream, long offset, long length)
		{
			if (baseStream == null) throw new ArgumentNullException(nameof(baseStream));
			if (!baseStream.CanRead) throw new ArgumentException("cannot read base stream");
			if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
			if (!baseStream.CanSeek) throw new ArgumentException("cannot seek on base stream");

			_length = length;
			_baseStream = baseStream;
			_baseStream.Seek(offset, SeekOrigin.Current);
		}

		public override void Flush()
		{
			CheckDisposed();
			_baseStream.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			CheckDisposed();
			return _baseStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			CheckDisposed();
			long remaining = _length - _position;
			if (remaining <= 0)
			{
				return 0;
			}

			if (remaining < count)
			{
				count = (int)remaining;
			}

			int read = _baseStream.Read(buffer, offset, count);
			_position += read;
			return read;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}
		
		private void CheckDisposed()
		{
			if (_baseStream == null)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
		}
	}
}
