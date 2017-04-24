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
    [System.Serializable]
    public class GLTFUri
    {

        public static string BASE64_STR = "data:application/octet-stream;base64,";

	    private GLTFRoot root;

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

        /// <summary>
        /// The fetched uri texture.
        /// This will be null until it is done downloading.
        /// </summary>
        public Texture2D texture;

        public GLTFUri(GLTFRoot root, string uri)
        {
            this.root = root;
            this.uri = uri;
        }

        /// <summary>
        /// Load the remote URI data into a byte array.
        /// </summary>
        public IEnumerator LoadBuffer()
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
					UnityWebRequest www = UnityWebRequest.Get(AbsolutePath(root.Url, uri));

					yield return www.Send();

					data = www.downloadHandler.data;
				}
			}
        }

        /// <summary>
        /// Load a texture from the remote URI.
        /// </summary>
        public IEnumerator LoadTexture()
        {
            if (data == null)
            {
                if (uri.StartsWith(BASE64_STR))
                {
                    string base64Data = uri.Substring(BASE64_STR.Length);
                    var textureData = Convert.FromBase64String(base64Data);
                    texture = new Texture2D(0, 0);
                    texture.LoadImage(textureData);
                }
                else
                {
                    UnityWebRequest www = UnityWebRequest.Get(AbsolutePath(root.Url, uri));
                    www.downloadHandler = new DownloadHandlerTexture();

                    yield return www.Send();

                    texture = DownloadHandlerTexture.GetContent(www);
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
