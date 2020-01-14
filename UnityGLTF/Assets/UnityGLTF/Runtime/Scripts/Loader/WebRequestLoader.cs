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
#endif
using System.Threading;
using System.Threading.Tasks;

namespace UnityGLTF.Loader
{
	public class WebRequestLoader : IDataLoader
	{
		private readonly HttpClient httpClient = new HttpClient();
		private readonly Uri baseAddress;

		public WebRequestLoader(string rootUri)
		{
#if !WINDOWS_UWP
			ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
#endif
			baseAddress = new Uri(rootUri);
		}

		public async Task<Stream> LoadStreamAsync(string gltfFilePath)
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
			catch (TaskCanceledException)
			{
#if WINDOWS_UWP
				throw new Exception("Connection timeout");
#else
				throw new HttpRequestException("Connection timeout");
#endif
			}

			response.EnsureSuccessStatusCode();

			// HACK: Download the whole file before returning the stream
			// Ideally the parsers would wait for data to be available, but they don't.
			var result = new MemoryStream((int?)response.Content.Headers.ContentLength ?? 5000);
#if WINDOWS_UWP
			await response.Content.WriteToStreamAsync(result.AsOutputStream());
#else
			await response.Content.CopyToAsync(result);
#endif
			response.Dispose();
			return result;
		}

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
