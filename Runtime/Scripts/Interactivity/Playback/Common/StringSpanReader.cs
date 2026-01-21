#define DEBUG_MESSAGES

using System;

namespace UnityGLTF.Interactivity.Playback
{
    public ref struct StringSpanReader
    {
        private int _start;
        private int _end;
        private readonly ReadOnlySpan<char> _buffer;
        public char this[int i]
        {
            get
            {
                if (_start + i > _end)
                    throw new IndexOutOfRangeException();

                return _buffer[_start + i];
            }
        }

        public StringSpanReader(ReadOnlySpan<char> buffer)
        {
            _buffer = buffer;
            _start = 0;
            _end = buffer.Length;
        }

        public bool AdvanceHeadToNextInstanceOfChar(char c, bool inclusive = false)
        {
            // When the character we're already starting from is the one we want to advance to, advance the head by 1.
            if (_buffer[_start] == c)
                _start++;

            for (int i = _start; i < _buffer.Length; i++)
            {
                if (_buffer[i] != c)
                    continue;

                _start = i + (inclusive ? 0 : 1);
                return true;
            }

            return false;
        }

        public bool AdvanceTailToFirstInstanceOfChar(char c, bool inclusive = false)
        {
            for (int i = _start; i < _buffer.Length; i++)
            {
                if (_buffer[i] != c)
                    continue;

                _end = i + (inclusive ? 1 : 0);
                return true;
            }

            return false;
        }

        public bool AdvanceToNextToken(char c)
        {
            if (!AdvanceHeadToNextInstanceOfChar(c))
                return false;

            if (!AdvanceTailToFirstInstanceOfChar(c))
                _end = _buffer.Length;

            return true;
        }

        public void Slice(char startChar, char endChar, bool inclusive = false)
        {
            for (int i = _start; i < _end; i++)
            {
                if (_buffer[i] != startChar)
                    continue;
                
                _start = i + (inclusive ? 0 : 1);
                break;
            }

            for (int i = _start; i < _end; i++)
            {
                if (_buffer[i] != endChar)
                    continue;

                _end = i + (inclusive ? 1 : 0);
                break;
            }
        }

        public ReadOnlySpan<char> AsReadOnlySpan()
        {
            return _buffer.Slice(_start, _end - _start);
        }

        public bool AnyMatch(char a, ReadOnlySpan<char> characters)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] == a)
                    return true;
            }

            return false;
        }

        public override string ToString()
        {
            return AsReadOnlySpan().ToString();
        }

        internal void SetStartIndexToEndIndex()
        {
            _start = _end;
        }
    }
}