using System;
using System.Buffers;

namespace UnityGLTF.Interactivity.Playback
{
    public ref struct ValueStringBuilder
    {
        private int _bufferPosition;
        private Span<char> _buffer;
#nullable enable
        private char[]? _arrayFromPool;
#nullable disable

        public int length => _bufferPosition;

        public ValueStringBuilder(int initialSize)
        {
            _bufferPosition = 0;
            _buffer = ArrayPool<char>.Shared.Rent(initialSize);
            _arrayFromPool = null;
        }

        public ref char this[int index] => ref _buffer[index];

        public int FirstIndexOf(char c)
        {
            for (int i = 0; i < _bufferPosition; i++)
            {
                if (_buffer[i] == c)
                    return i;
            }

            return -1;
        }

        public int LastIndexOf(char c)
        {
            var lastIndex = -1;
            for (int i = 0; i < _bufferPosition; i++)
            {
                if (_buffer[i] == c)
                    lastIndex = i;
            }

            return lastIndex;
        }

        public void Append(char c)
        {
            if (_bufferPosition >= _buffer.Length - 1)
            {
                Grow();
            }

            _buffer[_bufferPosition++] = c;
        }

        public void Clear()
        {
            _bufferPosition = 0;
        }

        public void Append(ReadOnlySpan<char> str)
        {
            var newSize = str.Length + _bufferPosition;
            if (newSize > _buffer.Length)
                Grow(newSize * 2);

            str.CopyTo(_buffer[_bufferPosition..]);
            _bufferPosition += str.Length;
        }

        public void AppendLine(ReadOnlySpan<char> str)
        {
            Append(str);
            Append(Environment.NewLine);
        }
        public void MoveBufferPositionBack(int numIndices)
        {
            _bufferPosition -= numIndices;
        }

        public override string ToString() => new(_buffer[.._bufferPosition]);
        public string ToString(int start, int end) => new(_buffer[start..end]);

        public void Dispose()
        {
            if (_arrayFromPool is not null)
            {
                ArrayPool<char>.Shared.Return(_arrayFromPool);
            }
        }

        private void Grow(int capacity = 0)
        {
            var currentSize = _buffer.Length;
            var newSize = capacity > 0 ? capacity : currentSize * 2;
            var rented = ArrayPool<char>.Shared.Rent(newSize);
            var oldBuffer = _arrayFromPool;
            _buffer.CopyTo(rented);
            _buffer = _arrayFromPool = rented;
            if (oldBuffer is not null)
            {
                ArrayPool<char>.Shared.Return(oldBuffer);
            }
        }
    }
}