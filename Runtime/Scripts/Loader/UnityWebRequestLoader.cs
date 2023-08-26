using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityGLTF.Loader
{
	public class UnityWebRequestLoader : IDataLoader
	{
		private string dir;

		public UnityWebRequestLoader(string dir)
		{
			this.dir = dir;
		}
#if UNITY_WEBREQUEST
		public async Task<Stream> LoadStreamAsync(string relativeFilePath)
		{
			var path = Path.Combine(dir, relativeFilePath).Replace("\\","/");
			if (File.Exists(path))
				path = "file://" + Path.GetFullPath(path);
			var request = UnityWebRequest.Get(path);
			// request.downloadHandler = new DownloadStreamHandler(new byte[1024 * 1024]);
			var asyncOperation = request.SendWebRequest();

			while (!asyncOperation.isDone) {
				await Task.Yield();
			}

#if UNITY_2020_1_OR_NEWER
			if (request.result != UnityWebRequest.Result.Success)
#else
			if (request.isNetworkError || request.isHttpError)
#endif
			{
				Debug.LogError($"Error when loading {relativeFilePath} ({path}): {request.error}");
				return null;
			}

			var results = request.downloadHandler.data;
			var stream = new MemoryStream(results, 0, results.Length, false, true);
			return stream;
		}
#else
		public async Task<Stream> LoadStreamAsync(string relativeFilePath)
		{
			await Task.CompletedTask;
			throw new System.ApplicationException("The module com.unity.modules.unitywebrequest is required for this functionality. Please install it in your project.");
		}
#endif

		// TODO: figure out how to do this correctly in a streaming fashion.
		// private class DownloadStreamHandler : DownloadHandlerScript
		// {
		// 	private ulong expectedTotalBytes = 0;
		// 	private Stream stream;
		// 	public Stream GetStream()
		// 	{
		// 		return stream;
		// 	}
		//
		// 	public DownloadStreamHandler(byte[] buffer) : base(buffer)
		// 	{
		// 		stream = new MemoryStream(buffer, true);
		// 	}
		//
		// 	protected override bool ReceiveData(byte[] data, int dataLength)
		// 	{
		// 		if(data == null || data.Length < 1)
		// 		{
		// 			Debug.Log("LoggingDownloadHandler :: ReceiveData - received a null/empty buffer");
		// 			return false;
		// 		}
		//
		// 		stream.Write(data, 0, dataLength);
		// 		return true;
		// 	}
		//
		// 	protected override byte[] GetData()
		// 	{
		// 		throw new System.NotSupportedException("This is a stream, can't get all bytes");
		// 	}
		//
		// 	protected override void CompleteContent()
		// 	{
		// 		Debug.Log("LoggingDownloadHandler :: CompleteContent - DOWNLOAD COMPLETE!");
		// 	}
		//
		// 	protected override void ReceiveContentLengthHeader(ulong contentLength)
		// 	{
		// 		Debug.Log(string.Format("LoggingDownloadHandler :: ReceiveContentLength - length {0}", contentLength));
		// 		expectedTotalBytes = contentLength;
		// 		stream.SetLength((long) contentLength);
		// 	}
		// }
	}
}
