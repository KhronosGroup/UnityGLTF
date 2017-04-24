using System;
using UnityEngine;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// Metadata about the glTF asset.
    /// </summary>
    [System.Serializable]
    public class GLTFAsset : GLTFProperty
    {
        /// <summary>
        /// A copyright message suitable for display to credit the content creator.
        /// </summary>
        public string copyright;

        /// <summary>
        /// Tool that generated this glTF model. Useful for debugging.
        /// </summary>
        public string generator;

        /// <summary>
        /// The glTF version.
        /// </summary>
        public string version;

        /// <summary>
        /// The minimum glTF version that this asset targets.
        /// </summary>
        public string minVersion;

        public static GLTFAsset Deserialize(JsonTextReader reader)
        {
            var asset = new GLTFAsset();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Asset must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "copyright":
                        asset.copyright = reader.ReadAsString();
                        break;
                    case "generator":
                        asset.generator = reader.ReadAsString();
                        break;
                    case "version":
                        asset.version = reader.ReadAsString();
                        break;
                    case "minVersion":
                        asset.minVersion = reader.ReadAsString();
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return asset;
        }
    }
}
