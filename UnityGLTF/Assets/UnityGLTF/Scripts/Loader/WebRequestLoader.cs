using System;
using System.IO;
using GLTF;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Net;
#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace UnityGLTF.Loader
{
	public class WebRequestLoader : ILoader
	{
		private string _rootURI;
		
		public WebRequestLoader(string rootURI)
		{
			_rootURI = rootURI;
		}

#if WINDOWS_UWP
		public async Task<Stream> LoadStream(string gltfFilePath)
#else
		public Stream LoadStream(string gltfFilePath)
#endif
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}

#if WINDOWS_UWP
			return await CreateHTTPRequest(_rootURI, gltfFilePath);
#else
			return CreateHTTPRequest(_rootURI, gltfFilePath);
#endif
		}

#if WINDOWS_UWP
		private async Task<MemoryStream> CreateHTTPRequest(string rootUri, string httpRequestPath)
#else
		private MemoryStream CreateHTTPRequest(string rootUri, string httpRequestPath)
#endif
		{
			HttpWebRequest www = (HttpWebRequest)WebRequest.Create(Path.Combine(_rootURI, httpRequestPath));
#if WINDOWS_UWP
			WebResponse webResponse = await www.GetResponseAsync();
			HttpWebResponse httpWebResponse = webResponse as HttpWebResponse;
#else
			www.Timeout = 5000;
			HttpWebResponse httpWebResponse = (HttpWebResponse)www.GetResponse();
#endif
			if ((int)httpWebResponse.StatusCode >= 400)
			{
				Debug.LogErrorFormat("{0} - {1}", httpWebResponse.StatusCode, httpWebResponse.ResponseUri);
				return null;
			}
			
			if (httpWebResponse.ContentLength > int.MaxValue)
			{
				throw new Exception("Stream is larger than can be copied into byte array");
			}

			Stream responseStream = httpWebResponse.GetResponseStream();
			MemoryStream memoryStream = new MemoryStream((int)httpWebResponse.ContentLength);

			byte[] chunk = new byte[4096];
			int bytesRead;
			while((bytesRead = responseStream.Read(chunk, 0, chunk.Length)) > 0)
			{
				memoryStream.Write(chunk, 0, bytesRead);
			}

#if WINDOWS_UWP
			httpWebResponse.Dispose();
#else
			httpWebResponse.Close();
#endif
			return memoryStream;
		}
	}
}
