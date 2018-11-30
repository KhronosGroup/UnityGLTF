using System;
using System.Collections;
using System.IO;
using GLTF;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Net;
using UnityEngine.Networking;
using System.Threading.Tasks;

namespace UnityGLTF.Loader
{
	public class WebRequestLoader : ILoader
	{
		public Stream LoadedStream { get; private set; }

		public bool HasSyncLoadMethod { get; private set; }

		private string _rootURI;
		private AsyncCoroutineHelper _asyncCoroutineHelper;

		public WebRequestLoader(string rootURI, AsyncCoroutineHelper asyncCoroutineHelper)
		{
			_rootURI = rootURI;
			_asyncCoroutineHelper = asyncCoroutineHelper;
			HasSyncLoadMethod = false;
		}

		public Task LoadStream(string gltfFilePath)
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}

			return _asyncCoroutineHelper.RunAsTask(CreateHTTPRequest(_rootURI, gltfFilePath), nameof(CreateHTTPRequest));
		}

		public void LoadStreamSync(string jsonFilePath)
		{
			throw new NotImplementedException();
		}

		private IEnumerator CreateHTTPRequest(string rootUri, string httpRequestPath)
		{
			UnityWebRequest www = new UnityWebRequest(Path.Combine(rootUri, httpRequestPath), "GET", new DownloadHandlerBuffer(), null);
			www.timeout = 5000;
#if UNITY_2017_2_OR_NEWER
			yield return www.SendWebRequest();
#else
			yield return www.Send();
#endif
			if ((int)www.responseCode >= 400)
			{
				Debug.LogErrorFormat("{0} - {1}", www.responseCode, www.url);
				throw new Exception("Response code invalid");
			}

			if (www.downloadedBytes > int.MaxValue)
			{
				throw new Exception("Stream is larger than can be copied into byte array");
			}

			LoadedStream = new MemoryStream(www.downloadHandler.data, 0, www.downloadHandler.data.Length, true, true);
		}
	}
}
