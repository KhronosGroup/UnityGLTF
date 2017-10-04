using System;
using System.IO;
using GLTF;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Net;

namespace UnityGLTF.Loader
{
	public class WebRequestLoader : ILoader
	{
		private const string Base64StringInitializer = "^data:[a-z-]+/[a-z-]+;base64,";
		private string _rootURI;
		
		public WebRequestLoader(string rootURI)
		{
			_rootURI = rootURI;
		}

		public Stream LoadJSON(string gltfFilePath)
		{
			if(gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}

			return CreateHTTPRequest(_rootURI, gltfFilePath);
		}

		public Stream LoadBuffer(GLTF.Schema.Buffer buffer)
		{
			if(buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}

			if(buffer.Uri == null)
			{
				throw new ArgumentException("Cannot load buffer with null URI. Should be loaded via GLB method instead", "buffer");
			}

			Stream bufferDataStream = null;
			var uri = buffer.Uri;

			Regex regex = new Regex(Base64StringInitializer);
			Match match = regex.Match(uri);
			if (match.Success)
			{
				var base64Data = uri.Substring(match.Length);
				byte[] bufferData = Convert.FromBase64String(base64Data);
				bufferDataStream = new MemoryStream(bufferData, 0, bufferData.Length, false, true);
			}
			else
			{
				bufferDataStream = CreateHTTPRequest(_rootURI, uri);
			}

			return bufferDataStream;
		}

		public Texture2D LoadImage(GLTF.Schema.Image image)
		{
			if(image == null)
			{
				throw new ArgumentNullException("image");
			}

			Texture2D texture = null;
			var uri = image.Uri;

			Regex regex = new Regex(Base64StringInitializer);
			Match match = regex.Match(uri);
			if (match.Success)
			{
				var base64Data = uri.Substring(match.Length);
				var textureData = Convert.FromBase64String(base64Data);
				texture = new Texture2D(0, 0);
				texture.LoadImage(textureData);
			}
			else
			{
				MemoryStream responseStream = CreateHTTPRequest(_rootURI, uri);
				
				if (responseStream != null)
				{
					texture = new Texture2D(0, 0);
					if(responseStream.Length > int.MaxValue)
					{
						throw new Exception("Stream is larger than can be copied into byte array");
					}
		
					texture.LoadImage(responseStream.ToArray());
					texture.Apply();
				}

				responseStream.Close();
			}

			return texture;
		}

		private MemoryStream CreateHTTPRequest(string rootUri, string httpRequestPath)
		{
			HttpWebRequest www = (HttpWebRequest)WebRequest.Create(Path.Combine(_rootURI, httpRequestPath));
			www.Timeout = 5000;
			HttpWebResponse webResponse = (HttpWebResponse)www.GetResponse();
			
			if ((int)webResponse.StatusCode >= 400)
			{
				Debug.LogErrorFormat("{0} - {1}", webResponse.StatusCode, webResponse.ResponseUri);
				return null;
			}
			
			if (webResponse.ContentLength > int.MaxValue)
			{
				throw new Exception("Stream is larger than can be copied into byte array");
			}

			Stream responseStream = webResponse.GetResponseStream();
			MemoryStream memoryStream = new MemoryStream((int)webResponse.ContentLength);

			byte[] chunk = new byte[4096];
			int bytesRead;
			while((bytesRead = responseStream.Read(chunk, 0, chunk.Length)) > 0)
			{
				memoryStream.Write(chunk, 0, bytesRead);
			}

			webResponse.Close();
			return memoryStream;
		}
	}
}
