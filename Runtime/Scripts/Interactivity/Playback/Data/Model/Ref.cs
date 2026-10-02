using System;

namespace UnityGLTF.Interactivity.Playback
{
    public enum RefKind : byte
    {
        Null = 0,
        /// <summary>A glTF object addressed by a collection path and an index, e.g. /animations/1.</summary>
        Gltf = 1,
        /// <summary>A delayed flow activation scheduled by flow/setDelay.</summary>
        Delay = 2,
        /// <summary>An event occurrence produced by an event/* operation.</summary>
        Event = 3,
    }

    /// <summary>
    /// Opaque reference value used by the "ref" value type of KHR_interactivity.
    /// Two glTF references are equal when they point at the same collection and index,
    /// regardless of whether that object exists, as required by ref/eq.
    /// </summary>
    public readonly struct Ref : IEquatable<Ref>
    {
        public static readonly Ref Null = default;

        public readonly RefKind kind;
        /// <summary>For glTF references, the JSON pointer of the containing array, e.g. "/nodes".</summary>
        public readonly string collection;
        /// <summary>Index into the collection for glTF references, unique id otherwise.</summary>
        public readonly int id;

        private Ref(RefKind kind, string collection, int id)
        {
            this.kind = kind;
            this.collection = collection;
            this.id = id;
        }

        public bool isNull => kind == RefKind.Null;

        public static Ref Gltf(string collection, int index) => new(RefKind.Gltf, collection, index);
        public static Ref Delay(int id) => new(RefKind.Delay, null, id);
        public static Ref Event(int id) => new(RefKind.Event, null, id);

        /// <summary>
        /// Checks RFC 6901 syntax: empty, or starting with '/' with every '~' followed by '0' or '1'.
        /// </summary>
        public static bool IsValidJsonPointer(string pointer)
        {
            if (pointer == null)
                return false;

            if (pointer.Length == 0)
                return true;

            if (pointer[0] != '/')
                return false;

            for (int i = 0; i < pointer.Length; i++)
            {
                if (pointer[i] != '~')
                    continue;

                if (i + 1 >= pointer.Length || (pointer[i + 1] != '0' && pointer[i + 1] != '1'))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Converts a static JSON pointer such as "/animations/1" into a glTF reference.
        /// Returns false only when the pointer is syntactically invalid.
        /// A syntactically valid pointer that does not address an array element yields <see cref="Null"/>.
        /// </summary>
        public static bool TryParsePointer(string pointer, out Ref value)
        {
            value = Null;

            if (!IsValidJsonPointer(pointer))
                return false;

            var lastSlash = pointer.LastIndexOf('/');

            // "" or "/x" cannot address an element of an array.
            if (lastSlash <= 0)
                return true;

            if (!TryParseCanonicalIndex(pointer.AsSpan(lastSlash + 1), out var index))
                return true;

            value = Gltf(pointer.Substring(0, lastSlash), index);
            return true;
        }

        /// <summary>
        /// Parses a non-negative decimal integer without sign or leading zeros, as used by JSON pointer array indices.
        /// </summary>
        public static bool TryParseCanonicalIndex(ReadOnlySpan<char> s, out int index)
        {
            index = -1;

            if (s.Length == 0 || s.Length > 10)
                return false;

            if (s.Length > 1 && s[0] == '0')
                return false;

            long v = 0;

            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c < '0' || c > '9')
                    return false;

                v = v * 10 + (c - '0');
            }

            if (v > int.MaxValue)
                return false;

            index = (int)v;
            return true;
        }

        /// <summary>The static JSON pointer for glTF references, null for other kinds.</summary>
        public string ToPointer()
        {
            return kind == RefKind.Gltf ? $"{collection}/{id}" : null;
        }

        public bool Equals(Ref other)
        {
            return kind == other.kind && id == other.id && string.Equals(collection, other.collection, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is Ref other && Equals(other);

        public override int GetHashCode() => HashCode.Combine((int)kind, collection, id);

        public static bool operator ==(Ref a, Ref b) => a.Equals(b);
        public static bool operator !=(Ref a, Ref b) => !a.Equals(b);

        public override string ToString()
        {
            return kind switch
            {
                RefKind.Null => "null",
                RefKind.Gltf => ToPointer(),
                RefKind.Delay => $"<delay {id}>",
                RefKind.Event => $"<event {id}>",
                _ => "<unknown>",
            };
        }
    }
}
