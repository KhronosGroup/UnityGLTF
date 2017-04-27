using System;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// Metadata about the glTF asset.
    /// </summary>
    public class GLTFAsset : GLTFProperty
    {
        /// <summary>
        /// A copyright message suitable for display to credit the content creator.
        /// </summary>
        public string Copyright;

        /// <summary>
        /// Tool that generated this glTF model. Useful for debugging.
        /// </summary>
        public string Generator;

        /// <summary>
        /// The glTF version.
        /// </summary>
        public string Version;

        /// <summary>
        /// The minimum glTF version that this asset targets.
        /// </summary>
        public string MinVersion;

        public static GLTFAsset Deserialize(GLTFRoot root, JsonTextReader reader)
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
				        asset.Copyright = reader.ReadAsString();
				        break;
			        case "generator":
				        asset.Generator = reader.ReadAsString();
				        break;
			        case "version":
				        asset.Version = reader.ReadAsString();
				        break;
			        case "minVersion":
				        asset.MinVersion = reader.ReadAsString();
				        break;
			        default:
				        asset.DefaultPropertyDeserializer(root, reader);
				        break;
		        }
	        }

	        return asset;
        }
    }
}
