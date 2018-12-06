using System;
using System.Collections.Generic;
using GLTF.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GLTF.Schema;

namespace GLTF.Extensions
{
	public static class JsonReaderExtensions
	{
		public static List<string> ReadStringList(this JsonReader reader)
		{
			if (reader.Read() && reader.TokenType != JsonToken.StartArray)
			{
				throw new Exception(string.Format("Invalid array at: {0}", reader.Path));
			}

			var list = new List<string>();

			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				list.Add(reader.Value.ToString());
			}

			return list;
		}

		public static List<double> ReadDoubleList(this JsonReader reader)
		{
			if (reader.Read() && reader.TokenType != JsonToken.StartArray)
			{
				throw new Exception(string.Format("Invalid array at: {0}", reader.Path));
			}

			var list = new List<double>();

			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				list.Add(double.Parse(reader.Value.ToString()));
			}

			return list;
		}

		public static List<int> ReadInt32List(this JsonReader reader)
		{
			if (reader.Read() && reader.TokenType != JsonToken.StartArray)
			{
				throw new Exception(string.Format("Invalid array at: {0}", reader.Path));
			}

			var list = new List<int>();

			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				list.Add(int.Parse(reader.Value.ToString()));
			}

			return list;
		}

		public static List<T> ReadList<T>(this JsonReader reader, Func<T> deserializerFunc)
		{
			if (reader.Read() && reader.TokenType != JsonToken.StartArray)
			{
				throw new Exception(string.Format("Invalid array at: {0}", reader.Path));
			}

			var list = new List<T>();

			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				list.Add(deserializerFunc());

				// deserializerFunc can advance to EndArray. We need to check for this case as well. 
				if (reader.TokenType == JsonToken.EndArray)
				{
					break;
				}
			}

			return list;
		}

		public static TextureInfo DeserializeAsTexture(this JToken token, GLTFRoot root)
		{
			TextureInfo textureInfo = null;
			
			if (token != null)
			{
				JObject textureObject = token as JObject;
				if (textureObject == null)
				{
					throw new Exception("JToken used for Texture deserialization was not a JObject. It was a " + token.Type.ToString());
				}

#if DEBUG
				// Broken on il2cpp. Don't ship debug DLLs there.
				System.Diagnostics.Debug.WriteLine("textureObject is " + textureObject.Type + " with a value of: " + textureObject[TextureInfo.INDEX].Type + " " + textureObject.ToString());
#endif

				int indexVal = textureObject[TextureInfo.INDEX].DeserializeAsInt();
				textureInfo = new TextureInfo()
				{
					Index = new TextureId()
					{
						Id = indexVal,
						Root = root
					}
				};
			}

			return textureInfo;
		}

		public static int DeserializeAsInt(this JToken token)
		{
			if (token != null)
			{
				JValue intValue = token as JValue;
				if (intValue == null)
				{
					throw new Exception("JToken used for int deserialization was not a JValue. It was a " + token.Type.ToString());
				}

				return (int)intValue;
			}

			return 0;
		}

		public static double DeserializeAsDouble(this JToken token)
		{
			if (token != null)
			{
				JValue doubleValue = token as JValue;
				if (doubleValue == null)
				{
					throw new Exception("JToken used for double deserialization was not a JValue. It was a " + token.Type.ToString());
				}

				return (double)doubleValue;
			}

			return 0d;
		}

		public static Color ReadAsRGBAColor(this JsonReader reader)
		{
			if (reader.Read() && reader.TokenType != JsonToken.StartArray)
			{
				throw new Exception(string.Format("Invalid color value at: {0}", reader.Path));
			}

			var color = new Color
			{
				R = (float) reader.ReadAsDouble().Value,
				G = (float) reader.ReadAsDouble().Value,
				B = (float) reader.ReadAsDouble().Value,
				A = (float) reader.ReadAsDouble().Value
			};

			if (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				throw new Exception(string.Format("Invalid color value at: {0}", reader.Path));
			}

			return color;
		}
		
		public static Color DeserializeAsColor(this JToken token)
		{
			Color color = Color.White;

			if (token != null)
			{
				JArray colorArray = token as JArray;
				if (colorArray == null)
				{
					throw new Exception("JToken used for Color deserialization was not a JArray. It was a " + token.Type.ToString());
				}
				if (colorArray.Count != 4)
				{
					throw new Exception("JArray used for Color deserialization did not have 4 entries for RGBA. It had " + colorArray.Count);
				}

				color = new Color
				{
					R = (float)colorArray[0].DeserializeAsDouble(),
					G = (float)colorArray[1].DeserializeAsDouble(),
					B = (float)colorArray[2].DeserializeAsDouble(),
					A = (float)colorArray[3].DeserializeAsDouble()
				};
			}

			return color;
		}

		public static Color ReadAsRGBColor(this JsonReader reader)
		{
			if (reader.Read() && reader.TokenType != JsonToken.StartArray)
			{
				throw new Exception(string.Format("Invalid vector value at: {0}", reader.Path));
			}

			var color = new Color
			{
				R = (float) reader.ReadAsDouble().Value,
				G = (float) reader.ReadAsDouble().Value,
				B = (float) reader.ReadAsDouble().Value,
				A = 1.0f
			};

			if (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				throw new Exception(string.Format("Invalid color value at: {0}", reader.Path));
			}

			return color;
		}

		public static Vector3 ReadAsVector3(this JsonReader reader)
		{
			if (reader.Read() && reader.TokenType != JsonToken.StartArray)
			{
				throw new Exception(string.Format("Invalid vector value at: {0}", reader.Path));
			}

			var vector = new Vector3
			{
				X = (float) reader.ReadAsDouble().Value,
				Y = (float) reader.ReadAsDouble().Value,
				Z = (float) reader.ReadAsDouble().Value
			};

			if (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				throw new Exception(string.Format("Invalid vector value at: {0}", reader.Path));
			}

			return vector;
		}

		public static Vector2 DeserializeAsVector2(this JToken token)
		{
			Vector2 vector = new Vector2();

			if (token != null)
			{
				JArray vectorArray = token as JArray;
				if (vectorArray == null)
				{
					throw new Exception("JToken used for Vector2 deserialization was not a JArray. It was a " + token.Type.ToString());
				}
				if (vectorArray.Count != 2)
				{
					throw new Exception("JArray used for Vector2 deserialization did not have 2 entries for XY. It had " + vectorArray.Count);
				}

				vector = new Vector2
				{
					X = (float)vectorArray[0].DeserializeAsDouble(),
					Y = (float)vectorArray[1].DeserializeAsDouble()
				};
			}

			return vector;
		}

		public static Vector3 DeserializeAsVector3(this JToken token)
		{
			Vector3 vector = new Vector3();

			if (token != null)
			{
				JArray vectorArray = token as JArray;
				if (vectorArray == null)
				{
					throw new Exception("JToken used for Vector3 deserialization was not a JArray. It was a " + token.Type.ToString());
				}
				if (vectorArray.Count != 3)
				{
					throw new Exception("JArray used for Vector3 deserialization did not have 3 entries for XYZ. It had " + vectorArray.Count);
				}

				vector = new Vector3
				{
					X = (float)vectorArray[0].DeserializeAsDouble(),
					Y = (float)vectorArray[1].DeserializeAsDouble(),
					Z = (float)vectorArray[2].DeserializeAsDouble()
				};
			}

			return vector;
		}

		public static Quaternion ReadAsQuaternion(this JsonReader reader)
		{
			if (reader.Read() && reader.TokenType != JsonToken.StartArray)
			{
				throw new Exception(string.Format("Invalid vector value at: {0}", reader.Path));
			}

			var quat = new Quaternion
			{
				X = (float) reader.ReadAsDouble().Value,
				Y = (float) reader.ReadAsDouble().Value,
				Z = (float) reader.ReadAsDouble().Value,
				W = (float) reader.ReadAsDouble().Value
			};

			if (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				throw new Exception(string.Format("Invalid vector value at: {0}", reader.Path));
			}

			return quat;
		}

		public static Dictionary<string, T> ReadAsDictionary<T>(this JsonReader reader, Func<T> deserializerFunc, bool skipStartObjectRead = false)
		{
			if (!skipStartObjectRead && reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				throw new Exception(string.Format("Dictionary must be an object at: {0}.", reader.Path));
			}

			var dict = new Dictionary<string, T>();

			while (reader.Read() && reader.TokenType != JsonToken.EndObject)
			{
				dict.Add(reader.Value.ToString(), deserializerFunc());
			}

			return dict;
		}

		public static Dictionary<string, object> ReadAsObjectDictionary(this JsonReader reader, bool skipStartObjectRead = false)
		{
			if (!skipStartObjectRead && reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				throw new Exception(string.Format("Dictionary must be an object at: {0}", reader.Path));
			}

			var dict = new Dictionary<string, object>();

			while (reader.Read() && reader.TokenType != JsonToken.EndObject)
			{
			   dict.Add(reader.Value.ToString(), ReadDictionaryValue(reader));
			}

			return dict;
		}

		private static object ReadDictionaryValue(JsonReader reader)
		{
			if (!reader.Read())
			{
				return null;
			}

			switch (reader.TokenType)
			{
				case JsonToken.StartArray:
					return reader.ReadObjectList();
				case JsonToken.StartObject:
					return reader.ReadAsObjectDictionary(true);
				default:
					return reader.Value;
			}
		}

		private static List<object> ReadObjectList(this JsonReader reader)
		{

			var list = new List<object>();

			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				list.Add(reader.Value);
			}

			return list;
		}

		public static T ReadStringEnum<T>(this JsonReader reader)
		{
			return (T) Enum.Parse(typeof(T), reader.ReadAsString());
		}
	}
}
