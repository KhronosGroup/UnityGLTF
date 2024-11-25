using System.IO;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityGLTF.Loader
{
	public class FileLoader : IDataLoader2
	{
		private readonly string _rootDirectoryPath;

		public FileLoader(string rootDirectoryPath)
		{
			_rootDirectoryPath = rootDirectoryPath;
		}

		public static string CombinePaths(string basePath, string relativePath)
		{
			basePath = basePath.Replace("\\", "/");
			relativePath = relativePath.Replace("\\", "/");

			string[] baseParts = basePath.Split('/');
			string[] relativeParts = relativePath.Split('/');
			if (relativeParts.Length == 0)
			{
				return Path.Combine(basePath, relativePath);
			}

			int baseIndex = baseParts.Length - 1;
			int relativeIndex = 0;

			while (relativeIndex < relativeParts.Length)
			{
				if (relativeParts[relativeIndex] == "..")
				{
					if (baseIndex > 0)
					{
						baseIndex--;
					}
					else
					{
						relativeIndex++;
						return string.Join("/", relativeParts, relativeIndex, relativeParts.Length - relativeIndex);
					}
				}
				else
				{
					baseIndex++;
					if (baseIndex > baseParts.Length - 1)
					{
						Array.Resize(ref baseParts, baseParts.Length + 1);
					}
					baseParts[baseIndex] = relativeParts[relativeIndex];
				}

				relativeIndex++;
			}

			return string.Join("/", baseParts, 0, Mathf.Min(  baseParts.Length,baseIndex+1));
		}
		public Task<Stream> LoadStreamAsync(string relativeFilePath)
		{
			if (relativeFilePath.StartsWith("http://") || relativeFilePath.StartsWith("https://"))
			{
				return new UnityWebRequestLoader("").LoadStreamAsync(relativeFilePath);
			}
			
#if UNITY_EDITOR
			string path = CombinePaths(_rootDirectoryPath, relativeFilePath);

			if (!File.Exists(path))
			{
				// Manual combine path with relativeFilePath
				path = CombinePaths(_rootDirectoryPath, Uri.UnescapeDataString(relativeFilePath)).Replace("\\", "/");
			}

			if (UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(UnityEngine.Texture2D))
			{
				var stream = new GLTFSceneImporter.AssetDatabaseStream(path);
				return Task.FromResult((Stream)stream);
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
				pathToLoad = Uri.UnescapeDataString(Path.Combine(_rootDirectoryPath, relativeFilePath));
			}

			if (!File.Exists(pathToLoad))
			{
				if (relativeFilePath.ToLowerInvariant().EndsWith(".bin"))
					throw new FileNotFoundException("Buffer file " + relativeFilePath + " not found in " + _rootDirectoryPath + ", complete path: " + pathToLoad, relativeFilePath);

				// One exception here: we don't want to log an error if we're already knowing that the texture
				// has been remapped on import - that's fine! A missing texture can be remapped to a valid one.
				return new InvalidStream(relativeFilePath, _rootDirectoryPath, pathToLoad);
			}

			return File.OpenRead(pathToLoad);
		}

		internal class InvalidStream: MemoryStream
		{
			public readonly string RelativeFilePath;
			public readonly string RootDirectory;
			public readonly string AbsoluteFilePath;

			public InvalidStream(string relativeFilePath, string rootDirectory, string pathToLoad)
			{
				RelativeFilePath = relativeFilePath;
				RootDirectory = rootDirectory;
				AbsoluteFilePath = pathToLoad;
			}
		}
	}
}
