using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace UnityGLTF.Loader
{
	public class WebRequestLoader : ILoader
	{
		public Stream LoadedStream { get; private set; }

		public bool HasSyncLoadMethod => false;

		private readonly HttpClient httpClient;

		public WebRequestLoader(string rootUri)
		{
			if (!ServerValidationRegistered)
			{
				ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
				ServerValidationRegistered = true;
			}

			var handler = new HttpClientHandler()
			{
				AllowAutoRedirect = false
			};
			httpClient = new HttpClient(handler)
			{
				BaseAddress = new Uri(rootUri)
			};
		}

		public async Task LoadStream(string gltfFilePath)
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException(nameof(gltfFilePath));
			}

			HttpResponseMessage response;
			var fileUrl = new Uri(httpClient.BaseAddress, gltfFilePath);
			do
			{
				UnityEngine.Debug.Log($"GET {fileUrl}");
				response = await httpClient.GetAsync(fileUrl);
				UnityEngine.Debug.Log(response.StatusCode.ToString());
				if ((int) response.StatusCode >= 300 && (int) response.StatusCode < 400)
				{
					fileUrl = response.Headers.Location;
				}
			} while ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400);
			response.EnsureSuccessStatusCode();

			// HACK: Download the whole file before returning the stream
			// Ideally the parsers would wait for data to be available, but they don't.
			LoadedStream = new MemoryStream((int?)response.Content.Headers.ContentLength ?? 5000);
			await response.Content.CopyToAsync(LoadedStream);

			response.Dispose();
		}

		public void LoadStreamSync(string jsonFilePath)
		{
			throw new NotImplementedException();
		}

		private static bool ServerValidationRegistered = false;

		// enables HTTPS support
		// https://answers.unity.com/questions/50013/httpwebrequestgetrequeststream-https-certificate-e.html
		private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			UnityEngine.Debug.Log($"Validating {sender as string}");
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
	}
}
