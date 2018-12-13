using System;
using System.Collections;
using System.IO;
using GLTF;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Net.Http;
using UnityEngine.Networking;
using System.Threading.Tasks;

namespace UnityGLTF.Loader
{
	public class WebRequestLoader : ILoader
	{
		public Stream LoadedStream { get; private set; }

		public bool HasSyncLoadMethod => false;

		private string _rootURI;
		private HttpClient httpClient;

		public WebRequestLoader(string rootURI)
		{
			httpClient = new HttpClient
			{
				BaseAddress = new Uri(rootURI)
			};
		}

		public async Task LoadStream(string gltfFilePath)
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException(nameof(gltfFilePath));
			}

			var response = await httpClient.GetStreamAsync(gltfFilePath);

			// HACK: Download the whole file before returning the stream
			// Ideally the parsers would wait for data to be available, but they don't.
			LoadedStream = new MemoryStream();
			await response.CopyToAsync(LoadedStream);
		}

		public void LoadStreamSync(string jsonFilePath)
		{
			throw new NotImplementedException();
		}
	}
}
