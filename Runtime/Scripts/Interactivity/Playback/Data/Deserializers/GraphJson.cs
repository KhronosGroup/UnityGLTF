using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>
    /// Thrown when a behavior graph violates a "graph MUST be rejected" rule of the specification.
    /// </summary>
    public class InteractivityGraphException : Exception
    {
        public InteractivityGraphException(string message) : base(message) { }
    }

    /// <summary>
    /// Strict accessors for the JSON syntax of KHR_interactivity graphs.
    /// </summary>
    internal static class GraphJson
    {
        public static void Reject(string message) => throw new InteractivityGraphException(message);

        /// <summary>Returns the array property or an empty enumerable when it is omitted.</summary>
        public static IEnumerable<JToken> OptionalArray(JObject obj, string name, string context)
        {
            var token = obj[name];

            if (token == null || token.Type == JTokenType.Null)
                return Array.Empty<JToken>();

            if (token is not JArray array)
            {
                Reject($"{context}: \"{name}\" must be an array.");
                return null;
            }

            return array;
        }

        public static JObject OptionalObject(JToken obj, string name, string context)
        {
            var token = obj[name];

            if (token == null || token.Type == JTokenType.Null)
                return null;

            if (token is not JObject o)
            {
                Reject($"{context}: \"{name}\" must be an object.");
                return null;
            }

            return o;
        }

        public static JObject AsObject(JToken token, string context)
        {
            if (token is not JObject o)
            {
                Reject($"{context} must be a JSON object.");
                return null;
            }

            return o;
        }

        public static string OptionalString(JToken obj, string name, string context)
        {
            var token = obj[name];

            if (token == null)
                return null;

            if (token.Type != JTokenType.String)
                Reject($"{context}: \"{name}\" must be a string.");

            return token.Value<string>();
        }

        public static string RequiredString(JToken obj, string name, string context)
        {
            var s = OptionalString(obj, name, context);

            if (s == null)
                Reject($"{context}: \"{name}\" is required.");

            return s;
        }

        /// <summary>Reads a required index property that must be in [0, count).</summary>
        public static int RequiredIndex(JToken obj, string name, int count, string context)
        {
            var token = obj[name];

            if (token == null)
                Reject($"{context}: \"{name}\" is required.");

            return Index(token, count, $"{context}: \"{name}\"");
        }

        public static int Index(JToken token, int count, string context)
        {
            if (!Helpers.IsExactInt32(token))
                Reject($"{context} must be an integer.");

            var i = (int)token.Value<double>();

            if (i < 0 || i >= count)
                Reject($"{context} value {i} is out of range [0, {count}).");

            return i;
        }

        public static JArray OptionalValueArray(JToken obj, string context)
        {
            var token = obj[ConstStrings.VALUE];

            if (token == null)
                return null;

            if (token is not JArray array)
            {
                Reject($"{context}: \"value\" must be an array.");
                return null;
            }

            return array;
        }

        /// <summary>
        /// Parses an inline value using the "Variables" section rules. A missing array yields the type-default.
        /// </summary>
        public static IProperty ParseValue(Type type, JArray value, string context)
        {
            if (!Helpers.TryValidateValueArray(type, value, out var error))
                Reject($"{context}: {error}.");

            return Helpers.CreateProperty(type, value);
        }
    }
}
