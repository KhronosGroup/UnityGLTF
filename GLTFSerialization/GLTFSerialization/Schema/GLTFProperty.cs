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
        private static KHR_materials_pbrSpecularGlossinessExtensionFactory _KHRExtensionFactory = new KHR_materials_pbrSpecularGlossinessExtensionFactory();

        public static void RegisterExtension(ExtensionFactory extensionFactory)
		{
			_extensionRegistry.Add(extensionFactory.ExtensionName, extensionFactory);
		}

		public Dictionary<string, Extension> Extensions;
		public JToken Extras;

		public void DefaultPropertyDeserializer(GLTFRoot root, JsonReader reader)
		{
			switch (reader.Value.ToString())
			{
				case "extensions":
                    // trace
					Extensions = DeserializeExtensions(root, reader);
					break;
				case "extras":
					Extras = JToken.ReadFrom(reader);
					break;
				default:
					SkipValue(reader);
					break;
			}
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

		private Dictionary<string, Extension> DeserializeExtensions(GLTFRoot root, JsonReader reader)
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
                // TODO: move hardcoded string to static string in material extension
                else if (_extensionRegistry.TryGetValue("KHR_materials_pbrSpecularGlossiness", out extensionFactory))
                {
                    // make sure my KHR_materials_pbrSpecularGlossiness fields get filled in by the Deserializer
                    extensions.Add("KHR_materials_pbrSpecularGlossiness", _defaultExtensionFactory.Deserialize(root, (JProperty)extensionToken));
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
					break;
				}
			}

            // make sure my material is in here - next step is to get the client to create a unity material from this
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
