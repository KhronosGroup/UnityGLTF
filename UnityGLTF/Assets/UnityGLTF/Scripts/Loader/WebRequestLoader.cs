using System;
using System.IO;
using System.Net;
#if WINDOWS_UWP
using Windows.Web.Http;
using Windows.Security;
using Windows.Storage.Streams;
#else
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
#endif
using System.Threading;
using System.Threading.Tasks;
#if UNITY_WEBGL
using UnityEngine.Networking;
using System.Collections;
using UnityEngine;
#endif

namespace UnityGLTF.Loader
{
	public class WebRequestLoader : ILoader
	{
		public Stream LoadedStream { get; private set; }

		public bool HasSyncLoadMethod => false;

		private readonly HttpClient httpClient = new HttpClient();
		private Uri baseAddress;

		private AsyncCoroutineHelper asyncCoroutineHelper;

		public WebRequestLoader(string rootUri, AsyncCoroutineHelper asyncCoroutineHelper = null)
		{
#if !WINDOWS_UWP
			ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
#endif
#if UNITY_WEBGL
			this.asyncCoroutineHelper = asyncCoroutineHelper;
#endif
			baseAddress = new Uri(rootUri);
		}

#if UNITY_WEBGL
		public Task LoadStream(string gltfFilePath)
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}
			return asyncCoroutineHelper.RunAsTask(CreateHTTPRequest(baseAddress.AbsoluteUri, gltfFilePath), nameof(CreateHTTPRequest));
		}
#else

		public async Task LoadStream(string gltfFilePath)
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException(nameof(gltfFilePath));
			}

			HttpResponseMessage response;
			try
			{
#if WINDOWS_UWP
				response = await httpClient.GetAsync(new Uri(baseAddress, gltfFilePath));
#else
				var tokenSource = new CancellationTokenSource(30000);
				response = await httpClient.GetAsync(new Uri(baseAddress, gltfFilePath), tokenSource.Token);
#endif
			}
			catch (TaskCanceledException e)
			{
#if WINDOWS_UWP
				throw new Exception($"Connection timeout: {baseAddress}");
#else
				throw new HttpRequestException($"Connection timeout: {baseAddress}");
#endif
			}

			response.EnsureSuccessStatusCode();

			// HACK: Download the whole file before returning the stream
			// Ideally the parsers would wait for data to be available, but they don't.
			LoadedStream = new MemoryStream((int?)response.Content.Headers.ContentLength ?? 5000);
#if WINDOWS_UWP
			await response.Content.WriteToStreamAsync((IOutputStream)LoadedStream);
#else
			await response.Content.CopyToAsync(LoadedStream);
#endif
			response.Dispose();
		}
#endif
		public void LoadStreamSync(string jsonFilePath)
		{
			throw new NotImplementedException();
		}

#if UNITY_WEBGL
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
#endif

#if !WINDOWS_UWP
		// enables HTTPS support
		// https://answers.unity.com/questions/50013/httpwebrequestgetrequeststream-https-certificate-e.html
		private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			bool isOk = true;
			// If there are errors in the certificate chain, look at each error to determine the cause.
			if (errors != SslPolicyErrors.None)
			{
				for (int i = 0; i<chain.ChainStatus.Length; i++)
				{
					if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
					{
						chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
						chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
						chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
						chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
						bool chainIsValid = chain.Build((X509Certificate2)certificate);
						if (!chainIsValid)
						{
							isOk = false;
						}
					}
				}
			}

			return isOk;
		}
#endif
	}
}
