using System;
using System.Collections.Generic;
using System.Text;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>
    /// A parsed JSON Pointer Template as defined by the "JSON Pointer Template Parsing" section of the spec.
    /// <c>[name]</c> segments are integer parameters and <c>{name}</c> segments are reference parameters.
    /// Literal brackets inside path segments are doubled (<c>[[</c>, <c>{{</c>).
    /// </summary>
    public sealed class PointerTemplate
    {
        /// <summary>Prefix used in effective pointers for references that cannot address the preceding collection.</summary>
        public const string INVALID_REFERENCE = "@invalid";
        public const string DELAY_REFERENCE_PREFIX = "@delay";
        public const string EVENT_REFERENCE_PREFIX = "@event";

        public readonly struct Parameter
        {
            /// <summary>Input value socket id: segment without brackets, with ~1 and ~0 decoded.</summary>
            public readonly string id;
            public readonly bool isReference;
            /// <summary>Index of the path segment holding this parameter.</summary>
            public readonly int segment;

            public Parameter(string id, bool isReference, int segment)
            {
                this.id = id;
                this.isReference = isReference;
                this.segment = segment;
            }
        }

        private enum SegmentKind { Literal, IntParameter, RefParameter }

        private readonly string[] _segments;
        private readonly SegmentKind[] _kinds;
        private readonly int[] _parameterBySegment;

        public string template { get; }
        public IReadOnlyList<Parameter> parameters { get; }
        public bool hasParameters => parameters.Count > 0;

        private PointerTemplate(string template, string[] segments, SegmentKind[] kinds, int[] parameterBySegment, List<Parameter> parameters)
        {
            this.template = template;
            _segments = segments;
            _kinds = kinds;
            _parameterBySegment = parameterBySegment;
            this.parameters = parameters;
        }

        public bool TryGetParameter(string id, out Parameter parameter)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].id == id)
                {
                    parameter = parameters[i];
                    return true;
                }
            }

            parameter = default;
            return false;
        }

        public static bool TryParse(string template, out PointerTemplate result, out string error)
        {
            result = null;
            error = null;

            if (!Ref.IsValidJsonPointer(template))
            {
                error = "not a syntactically valid JSON pointer";
                return false;
            }

            var segments = template.Split('/');
            var kinds = new SegmentKind[segments.Length];
            var parameterBySegment = new int[segments.Length];
            var parameters = new List<Parameter>();

            for (int i = 0; i < segments.Length; i++)
            {
                var s = segments[i];
                parameterBySegment[i] = -1;

                if (s == "[" || s == "{")
                {
                    error = $"segment \"{s}\" is a lone bracket";
                    return false;
                }

                var isInt = s.Length >= 2 && s[0] == '[' && s[1] != '[';
                var isRef = s.Length >= 2 && s[0] == '{' && s[1] != '{';

                if (isInt || isRef)
                {
                    var close = isInt ? ']' : '}';

                    if (s[s.Length - 1] != close || s.Length == 2)
                    {
                        error = $"parameter segment \"{s}\" is malformed";
                        return false;
                    }

                    for (int c = 1; c < s.Length - 1; c++)
                    {
                        var ch = s[c];
                        if (ch == '[' || ch == '{' || ch == ']' || ch == '}')
                        {
                            error = $"parameter segment \"{s}\" contains a bracket inside the parameter";
                            return false;
                        }
                    }

                    var id = s.Substring(1, s.Length - 2).Replace("~1", "/").Replace("~0", "~");

                    for (int p = 0; p < parameters.Count; p++)
                    {
                        if (parameters[p].id == id)
                        {
                            error = $"parameter \"{id}\" is used more than once";
                            return false;
                        }
                    }

                    kinds[i] = isInt ? SegmentKind.IntParameter : SegmentKind.RefParameter;
                    parameterBySegment[i] = parameters.Count;
                    parameters.Add(new Parameter(id, isRef, i));
                    continue;
                }

                if (HasOddBracketRun(s))
                {
                    error = $"literal segment \"{s}\" has an odd number of consecutive brackets";
                    return false;
                }

                kinds[i] = SegmentKind.Literal;
            }

            result = new PointerTemplate(template, segments, kinds, parameterBySegment, parameters);
            return true;
        }

        private static bool HasOddBracketRun(string s)
        {
            for (int i = 0; i < s.Length;)
            {
                var ch = s[i];

                if (ch != '[' && ch != ']' && ch != '{' && ch != '}')
                {
                    i++;
                    continue;
                }

                var run = 0;
                while (i < s.Length && s[i] == ch)
                {
                    run++;
                    i++;
                }

                if ((run & 1) == 1)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Builds the effective JSON pointer. Parameter values are supplied in <see cref="parameters"/> order:
        /// <paramref name="intValues"/> for integer parameters and <paramref name="refValues"/> for reference parameters
        /// (unused entries are ignored). Returns false when an integer is negative or a reference is null,
        /// in which case the operation must report an invalid result without resolving.
        /// </summary>
        public bool TryGenerate(IReadOnlyList<int> intValues, IReadOnlyList<Ref> refValues, out string effectivePointer)
        {
            effectivePointer = null;
            var sb = new StringBuilder(template.Length + 8);

            for (int i = 0; i < _segments.Length; i++)
            {
                if (i > 0)
                    sb.Append('/');

                switch (_kinds[i])
                {
                    case SegmentKind.Literal:
                        sb.Append(Unescape(_segments[i]));
                        break;

                    case SegmentKind.IntParameter:
                        var v = intValues[_parameterBySegment[i]];
                        if (v < 0)
                            return false;
                        sb.Append(v.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;

                    case SegmentKind.RefParameter:
                        var r = refValues[_parameterBySegment[i]];
                        if (r.isNull)
                            return false;
                        // Collection path so far, without the trailing slash.
                        var collection = sb.ToString(0, sb.Length - 1);
                        sb.Append(RepresentReference(collection, r));
                        break;
                }
            }

            effectivePointer = sb.ToString();
            return true;
        }

        /// <summary>
        /// Implementation-specific representation of a reference inside an effective pointer.
        /// A glTF reference is replaced by its index only when it belongs to the collection being addressed.
        /// </summary>
        private static string RepresentReference(string collection, Ref r)
        {
            switch (r.kind)
            {
                case RefKind.Gltf:
                    return string.Equals(r.collection, collection, StringComparison.Ordinal)
                        ? r.id.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        : INVALID_REFERENCE;
                case RefKind.Delay:
                    return DELAY_REFERENCE_PREFIX + r.id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case RefKind.Event:
                    return EVENT_REFERENCE_PREFIX + r.id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                default:
                    return INVALID_REFERENCE;
            }
        }

        /// <summary>Parses a reference representation produced for a delay or event segment.</summary>
        public static bool TryParseReferenceSegment(ReadOnlySpan<char> segment, out Ref value)
        {
            value = Ref.Null;

            if (segment.StartsWith(DELAY_REFERENCE_PREFIX.AsSpan()) && Ref.TryParseCanonicalIndex(segment.Slice(DELAY_REFERENCE_PREFIX.Length), out var d))
            {
                value = Ref.Delay(d);
                return true;
            }

            if (segment.StartsWith(EVENT_REFERENCE_PREFIX.AsSpan()) && Ref.TryParseCanonicalIndex(segment.Slice(EVENT_REFERENCE_PREFIX.Length), out var e))
            {
                value = Ref.Event(e);
                return true;
            }

            return false;
        }

        private static string Unescape(string literal)
        {
            if (literal.IndexOfAny(_brackets) < 0)
                return literal;

            return literal.Replace("[[", "[").Replace("]]", "]").Replace("{{", "{").Replace("}}", "}");
        }

        private static readonly char[] _brackets = { '[', ']', '{', '}' };
    }
}
