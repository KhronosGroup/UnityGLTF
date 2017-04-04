using System;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace GLTF
{
    /// <summary>
    /// A wrapper around the GLTF URI string with utility functions to load
    /// or parse its data.
    /// </summary>
    public class GLTFUri
    {
        public static string BASE64_STR = "data:application/octet-stream;base64,";

        /// <summary>
        /// The path to the .gltf file.
        /// </summary>
        public string gltfPath;

        /// <summary>
        /// The GLTF uri string.
        /// This can either be a url or base64 encoded string.
        /// </summary>
        public string uri;

        /// <summary>
        /// The fetched uri data.
        /// This will be null until it is done downloading.
        /// </summary>
        public byte[] data;

        public GLTFUri(string gltfPath, string uri)
        {
            this.gltfPath = gltfPath;
            this.uri = uri;
        }

        /// <summary>
        /// Load the remote URI data into a byte array.
        /// </summary>
        public IEnumerator Load()
        {
            if (data == null)
            {
				if (uri.StartsWith(BASE64_STR))
				{
					string base64Data = uri.Substring(BASE64_STR.Length);
					data = Convert.FromBase64String(base64Data);
				}
				else
				{
					UnityWebRequest www = UnityWebRequest.Get(AbsolutePath(gltfPath, uri));

					yield return www.Send();

					data = www.downloadHandler.data;
				}
			}
        }

        /// <summary>
        ///  Get the absolute path to a gltf uri reference.
        /// </summary>
        /// <param name="gltfUrl">The gltf file path.</param>
        /// <param name="relativePath">The relative path stored in the uri.</param>
        /// <returns></returns>
        public static string AbsolutePath(string gltfUrl, string relativePath)
        {
            var uri = new Uri(gltfUrl);
            var partialPath = uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments[uri.Segments.Length - 1].Length);
            return partialPath + relativePath;
        }
    }

    /// <summary>
    /// Converts a JSON string to a GLTFUri object.
    /// It also stores the path to the gltf file so we can load files from relative paths.
    /// </summary>
    public class GLTFUriConverter : JsonConverter
    {
        /// <summary>
        /// The path to the GLTF file.
        /// </summary>
        string gltfPath;

        public GLTFUriConverter(string gltfPath)
        {
            this.gltfPath = gltfPath;
        }

        /// <summary>
        /// This converter will be used if the referenced object is of the type: GLTFUri
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(GLTFUri);
        }

        /// <summary>
        /// Deserialize the JSON string into a GLTFUri instance.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
            {
                throw new Exception("Invalid URI type.");
            }

            string uri = serializer.Deserialize<string>(reader);

            return new GLTFUri(gltfPath, uri);
        }

        /// <summary>
        /// Serialize the GLTFUri instance into a JSON string.
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            GLTFUri gltfUri = (GLTFUri)value;
            serializer.Serialize(writer, gltfUri.uri);
        }
    }
}
