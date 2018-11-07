using System;
using System.Collections;
using System.IO;
using GLTF;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Net;
using UnityEngine.Networking;
using System.Threading.Tasks;

#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace UnityGLTF.Loader
{
	public class WebRequestLoader : ILoader
	{
		public Stream LoadedStream { get; private set; }

		public bool HasSyncLoadMethod { get; private set; }

		private string _rootURI;

		public WebRequestLoader(string rootURI)
		{
			_rootURI = rootURI;
			HasSyncLoadMethod = false;
		}

		public Task LoadStream(string gltfFilePath)
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}

			return CreateHTTPRequest(_rootURI, gltfFilePath);
		}

		public void LoadStreamSync(string jsonFilePath)
		{
			throw new NotImplementedException();
		}

		private async Task CreateHTTPRequest(string rootUri, string httpRequestPath)
		{
			HttpWebRequest webRequest = WebRequest.CreateHttp(Path.Combine(rootUri, httpRequestPath));
			webRequest.Method = "GET";
			webRequest.Timeout = 5000;
			HttpWebResponse response = (HttpWebResponse)await webRequest.GetResponseAsync();
			if ((int)response.StatusCode >= 400)
			{
				Debug.LogErrorFormat("{0} - {1}", response.StatusCode, response.ResponseUri);
				throw new Exception("Response code invalid");
			}

			Stream stream = response.GetResponseStream();
			if (stream.Length > int.MaxValue)
			{
				throw new Exception("Stream is larger than can be copied into byte array");
			}
			
			LoadedStream = stream;
		}
	}
}
