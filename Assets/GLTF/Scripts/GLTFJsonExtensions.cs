using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace GLTF.JsonExtensions
{
    public static class JsonTextReaderExtensions
    {
        public static List<string> ReadStringList(this JsonTextReader reader)
        {
            if (reader.Read() && reader.TokenType != JsonToken.StartArray)
            {
                throw new Exception("Invalid array.");
            }

            var list = new List<string>();

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                list.Add(reader.Value.ToString());
            }

            return list;
        }

        public static List<double> ReadDoubleList(this JsonTextReader reader)
        {
            if (reader.Read() && reader.TokenType != JsonToken.StartArray)
            {
                throw new Exception("Invalid array.");
            }

            var list = new List<double>();

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                list.Add(double.Parse(reader.Value.ToString()));
            }

            return list;
        }

        public static List<T> ReadList<T>(this JsonTextReader reader, Func<T> deserializerFunc)
        {
            if (reader.Read() && reader.TokenType != JsonToken.StartArray)
            {
                throw new Exception("Invalid array.");
            }

            var list = new List<T>();

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                list.Add(deserializerFunc());
            }

            return list;
        }

        public static Color ReadAsRGBAColor(this JsonTextReader reader)
        {
            if (reader.Read() && reader.TokenType != JsonToken.StartArray)
            {
                throw new Exception("Invalid color value.");
            }

	        var color = new Color
	        {
		        r = (float) reader.ReadAsDouble().Value,
		        g = (float) reader.ReadAsDouble().Value,
		        b = (float) reader.ReadAsDouble().Value,
		        a = (float) reader.ReadAsDouble().Value
	        };

	        if (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                throw new Exception("Invalid color value.");
            }

            return color;
        }

        public static Color ReadAsRGBColor(this JsonTextReader reader)
        {
            if (reader.Read() && reader.TokenType != JsonToken.StartArray)
            {
                throw new Exception("Invalid color value.");
            }

	        var color = new Color
	        {
		        r = (float) reader.ReadAsDouble().Value,
		        g = (float) reader.ReadAsDouble().Value,
		        b = (float) reader.ReadAsDouble().Value,
		        a = 1.0f
	        };

	        if (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                throw new Exception("Invalid color value.");
            }

            return color;
        }

        public static Vector3 ReadAsVector3(this JsonTextReader reader)
        {
            if (reader.Read() && reader.TokenType != JsonToken.StartArray)
            {
                throw new Exception("Invalid vector value.");
            }

	        var vector = new Vector3
	        {
		        x = (float) reader.ReadAsDouble().Value,
		        y = (float) reader.ReadAsDouble().Value,
		        z = (float) reader.ReadAsDouble().Value
	        };

	        if (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                throw new Exception("Invalid vector value.");
            }

            return vector;
        }

        public static Quaternion ReadAsQuaternion(this JsonTextReader reader)
        {
            if (reader.Read() && reader.TokenType != JsonToken.StartArray)
            {
                throw new Exception("Invalid vector value.");
            }

	        var quat = new Quaternion
	        {
		        x = (float) reader.ReadAsDouble().Value,
		        y = (float) reader.ReadAsDouble().Value,
		        z = (float) reader.ReadAsDouble().Value,
		        w = (float) reader.ReadAsDouble().Value
	        };

	        if (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                throw new Exception("Invalid vector value.");
            }

            return quat;
        }

        public static Dictionary<string, T> ReadAsDictionary<T>(this JsonTextReader reader, Func<T> deserializerFunc)
        {
            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Dictionary must be an object.");
            }

            var dict = new Dictionary<string, T>();

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                dict.Add(reader.Value.ToString(), deserializerFunc());
            }

            return dict;
        }

        public static T ReadStringEnum<T>(this JsonTextReader reader)
        {
            return (T) Enum.Parse(typeof(T), reader.ReadAsString());
        }
    }
}