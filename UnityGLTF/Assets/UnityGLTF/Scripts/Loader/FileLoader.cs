using System.IO;
using GLTF;
using UnityEngine;
using System;
using System.Collections;

#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace UnityGLTF.Loader
{
	public class FileLoader : ILoader
	{
		private string _rootDirectoryPath;
		public Stream LoadedStream { get; private set; }

		public FileLoader(string rootDirectoryPath)
		{
			_rootDirectoryPath = rootDirectoryPath;
		}

#if WINDOWS_UWP
		public async Task<Stream> LoadStream(string gltfFilePath)
#else
		public IEnumerator LoadStream(string gltfFilePath)
#endif
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}

#if WINDOWS_UWP
			return await LoadFileStream(_rootDirectoryPath, gltfFilePath);
#else
			yield return LoadFileStream(_rootDirectoryPath, gltfFilePath);
#endif
		}

#if WINDOWS_UWP
		private Task<Stream> LoadFileStream(string rootPath, string fileToLoad)
#else
		private IEnumerator LoadFileStream(string rootPath, string fileToLoad)
#endif
		{
			string pathToLoad = Path.Combine(_rootDirectoryPath, fileToLoad);
			if (!File.Exists(pathToLoad))
			{
				throw new FileNotFoundException("Buffer file not found", fileToLoad);
			}

#if WINDOWS_UWP
			return Task.Run<Stream>(() =>
			{
				return File.OpenRead(pathToLoad);
			});
#else
			yield return null;
			LoadedStream = File.OpenRead(pathToLoad);
#endif
		}
	}
}
