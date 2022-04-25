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

		public async Task<Stream> LoadStreamAsync(string relativeFilePath)
		{
			var path = Path.Combine(dir, relativeFilePath);
			var request = UnityWebRequest.Get(path);
			var asyncOperation = request.SendWebRequest();

			while (!asyncOperation.isDone) {
				await Task.Yield();
			}

			var results = request.downloadHandler.data;
			Debug.Log("got stream, length " + results.Length);
			var stream = new MemoryStream(results, 0, results.Length, false, true);
			return stream;
		}
	}
}
