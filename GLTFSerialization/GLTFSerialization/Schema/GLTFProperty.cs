using System;
using System.Collections.Generic;
using GLTF.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	public class GLTFProperty
	{
		private static Dictionary<string, ExtensionFactory> _extensionRegistry = new Dictionary<string, ExtensionFactory>();
		private static DefaultExtensionFactory _defaultExtensionFactory = new DefaultExtensionFactory();

		public static void RegisterExtension(ExtensionFactory extensionFactory)
		{
			_extensionRegistry.Add(extensionFactory.ExtensionName, extensionFactory);
		}

		public Dictionary<string, Extension> Extensions;
		public JToken Extras;

		public bool DefaultPropertyDeserializer(GLTFRoot root, JsonReader reader)
		{
            bool shouldSkipRead = false;
			switch (reader.Value.ToString())
			{
				case "extensions":
					Extensions = DeserializeExtensions(root, reader, ref shouldSkipRead);
                    break;
				case "extras":
					Extras = JToken.ReadFrom(reader);
                    shouldSkipRead = true;
                    break;
				default:
					SkipValue(reader);
					break;
			}

            return shouldSkipRead;
		}

		private void SkipValue(JsonReader reader)
		{
			if (!reader.Read())
			{
				throw new Exception("No value found.");
			}

			if (reader.TokenType == JsonToken.StartObject)
			{
				SkipObject(reader);
			}
			else if (reader.TokenType == JsonToken.StartArray)
			{
				SkipArray(reader);
			}
		}

		private void SkipObject(JsonReader reader)
		{
			while (reader.Read() && reader.TokenType != JsonToken.EndObject) {
				if (reader.TokenType == JsonToken.StartArray)
				{
					SkipArray(reader);
				}
				else if (reader.TokenType == JsonToken.StartObject)
				{
					SkipObject(reader);
				}
			}
		}

		private void SkipArray(JsonReader reader)
		{
			while (reader.Read() && reader.TokenType != JsonToken.EndArray) {
				if (reader.TokenType == JsonToken.StartArray)
				{
					SkipArray(reader);
				}
				else if (reader.TokenType == JsonToken.StartObject)
				{
					SkipObject(reader);
				}
			}
		}

        // todo: this should be rewritten so all extensions are a single jproperty and then we parse each jproperty
		private Dictionary<string, Extension> DeserializeExtensions(GLTFRoot root, JsonReader reader, ref bool shouldSkipRead)
		{
			if (reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				throw new Exception("GLTF extensions must be an object");
			}

			var extensions = new Dictionary<string, Extension>();

			bool isOnNextExtension = false;
			while (isOnNextExtension || (reader.Read() && reader.TokenType == JsonToken.PropertyName))
			{
				isOnNextExtension = false;
				var extensionName = reader.Value.ToString();
				ExtensionFactory extensionFactory;

				JToken extensionToken = JToken.ReadFrom(reader);
				if (_extensionRegistry.TryGetValue(extensionName, out extensionFactory))
				{
					extensions.Add(extensionName, extensionFactory.Deserialize(root, (JProperty)extensionToken));
				}
				else
				{
					extensions.Add(extensionName, _defaultExtensionFactory.Deserialize(root, (JProperty)extensionToken));
				}

				// using JToken.ReadFrom can progress the object to be on the next property already. This accounts for that so that we don't read past it
				isOnNextExtension = reader.TokenType == JsonToken.PropertyName;
				if (reader.TokenType == JsonToken.PropertyName)
				{
					isOnNextExtension = true;
				}
				else if (reader.TokenType == JsonToken.EndObject)
				{
                    reader.Read();  // advance to next property
					break;
				}
			}

            shouldSkipRead = extensions.Count != 0;
            return extensions;
		}

		public virtual void Serialize(JsonWriter writer)
		{
			if (Extensions != null && Extensions.Count > 0)
			{
				writer.WritePropertyName("extensions");
				writer.WriteStartObject();
				foreach (var extension in Extensions)
				{
					JToken extensionToken = extension.Value.Serialize();
					extensionToken.WriteTo(writer);
				}
				writer.WriteEndObject();
			}

			if(Extras != null)
			{
				Extras.WriteTo(writer);
			}
		}
	}
}
