using System;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace GLTF
{
    /// <summary>
    /// A wrapper around the GLTF URI string with utility functions to load
    /// or parse its data.
    /// </summary>
    [Serializable]
    public class GLTFUri
    {

        public static string Base64StringInitializer = "data:application/octet-stream;base64,";

	    private readonly GLTFRoot _root;

        /// <summary>
        /// The GLTF uri string. 
        /// This can either be a url or base64 encoded string.
        /// </summary>
        public string Uri;

	    /// <summary>
	    /// The fetched buffer data.
	    /// This will be null until it is done downloading.
	    /// </summary>
		public byte[] Data { get; private set; }

		/// <summary>
		/// The fetched uri texture.
		/// This will be null until it is done downloading.
		/// </summary>
		public Texture2D Texture { get; private set; }

		public GLTFUri(GLTFRoot root, string uri)
        {
            _root = root;
            Uri = uri;
        }

        /// <summary>
        /// Load the remote URI data into a byte array.
        /// </summary>
        public IEnumerator LoadBuffer()
        {
            if (Data == null)
            {
				if (Uri.StartsWith(Base64StringInitializer))
				{
					var base64Data = Uri.Substring(Base64StringInitializer.Length);
					Data = Convert.FromBase64String(base64Data);
				}
				else
				{
					var www = UnityWebRequest.Get(AbsolutePath(_root.Url, Uri));

					yield return www.Send();

					Data = www.downloadHandler.data;
				}
			}
        }

        /// <summary>
        /// Load a texture from the remote URI.
        /// </summary>
        public IEnumerator LoadTexture()
        {
            if (Data == null)
            {
                if (Uri.StartsWith(Base64StringInitializer))
                {
                    var base64Data = Uri.Substring(Base64StringInitializer.Length);
                    var textureData = Convert.FromBase64String(base64Data);
                    Texture = new Texture2D(0, 0);
                    Texture.LoadImage(textureData);
                }
                else
                {
                    var www = UnityWebRequest.Get(AbsolutePath(_root.Url, Uri));
                    www.downloadHandler = new DownloadHandlerTexture();

                    yield return www.Send();

                    Texture = DownloadHandlerTexture.GetContent(www);
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

        public static GLTFUri Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFUri(root, reader.ReadAsString());
        }
    }
}
