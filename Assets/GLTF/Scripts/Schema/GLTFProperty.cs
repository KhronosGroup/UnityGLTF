using System;
using System.Collections.Generic;
using GLTF.JsonExtensions;
using Newtonsoft.Json;

namespace GLTF
{
    public class GLTFProperty
    {
        private static Dictionary<string, GLTFExtensionFactory> _extensionRegistry = new Dictionary<string, GLTFExtensionFactory>();

        public static void RegisterExtension(GLTFExtensionFactory extensionFactory)
        {
            _extensionRegistry.Add(extensionFactory.ExtensionName, extensionFactory);
        }

        public Dictionary<string, GLTFExtension> Extensions;
        public Dictionary<string, object> Extras;

        public void DefaultPropertyDeserializer(GLTFRoot root, JsonTextReader reader)
        {
            switch (reader.Value.ToString())
            {
                case "extensions":
                    Extensions = DeserializeExtensions(root, reader);
                    break;
                case "extras":
                    Extras = reader.ReadAsObjectDictionary();
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

        private Dictionary<string, GLTFExtension> DeserializeExtensions(GLTFRoot root, JsonTextReader reader)
        {
            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("GLTF extensions must be an object");
            }

            var extensions = new Dictionary<string, GLTFExtension>();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var extensionName = reader.Value.ToString();
                GLTFExtensionFactory extensionFactory;

                if (_extensionRegistry.TryGetValue(extensionName, out extensionFactory))
                {
                    extensions.Add(extensionName, extensionFactory.Deserialize(root, reader));
                }
                else
                {
                    SkipValue(reader);
                }
            }

            return extensions;
        }

        public virtual void Serialize(JsonWriter writer)
        {
            if (Extensions != null && Extensions.Count > 0)
            {
                writer.WritePropertyName("extensions");
                writer.WriteStartArray();
                foreach (var extension in Extensions)
                {
                    writer.WritePropertyName(extension.Key);
                    extension.Value.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            // TODO: Extras serialization.
        }
    }
}
