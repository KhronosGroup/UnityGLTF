using System.IO;
using System;
using System.Threading.Tasks;

namespace UnityGLTF.Loader
{
	public class FileLoader : IDataLoader2
	{
		private readonly string _rootDirectoryPath;

		public FileLoader(string rootDirectoryPath)
		{
			_rootDirectoryPath = rootDirectoryPath;
		}

		public Task<Stream> LoadStreamAsync(string relativeFilePath)
		{
#if UNITY_EDITOR
			string path = Path.Combine(_rootDirectoryPath, relativeFilePath).Replace("\\", "/");

			if (!File.Exists(path))
			{
				path = Path.Combine(_rootDirectoryPath, Uri.UnescapeDataString(relativeFilePath)).Replace("\\", "/");
			}

			if (UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(UnityEngine.Texture2D))
			{
				var stream = new GLTFSceneImporter.AssetDatabaseStream(path);
				return Task.FromResult((Stream) stream);
			}
#endif

#if !WINDOWS_UWP && !UNITY_WEBGL
			// seems the Editor locks up in some cases when directly using Task.Run(() => {})
			if (UnityEngine.Application.isPlaying)
			{
				return Task.Run(() => LoadStream(relativeFilePath));
			}
#endif
			return Task.FromResult(LoadStream(relativeFilePath));
		}

		public Stream LoadStream(string relativeFilePath)
		{
			if (relativeFilePath == null)
			{
				throw new ArgumentNullException(nameof(relativeFilePath));
			}

			if (File.Exists(relativeFilePath))
				return File.OpenRead(relativeFilePath);

			string pathToLoad = Path.Combine(_rootDirectoryPath, relativeFilePath);
			if (!File.Exists(pathToLoad))
			{
				pathToLoad = Path.Combine(_rootDirectoryPath, Uri.UnescapeDataString(relativeFilePath));
			}

			if (!File.Exists(pathToLoad))
			{
				if (relativeFilePath.ToLowerInvariant().EndsWith(".bin"))
					throw new FileNotFoundException("Buffer file " + relativeFilePath + " not found in " + _rootDirectoryPath + ", complete path: " + pathToLoad, relativeFilePath);
				
				UnityEngine.Debug.LogError("Buffer file " + relativeFilePath + " not found in " + _rootDirectoryPath + ", complete path: " + pathToLoad);
				return InvalidStream;
			}

			return File.OpenRead(pathToLoad);
		}

		internal static readonly Stream InvalidStream = new MemoryStream();
	}
}
