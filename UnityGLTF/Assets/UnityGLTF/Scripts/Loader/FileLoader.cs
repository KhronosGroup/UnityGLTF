using System.IO;
using GLTF;
using UnityEngine;
using System;
#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace UnityGLTF.Loader
{
	public class FileLoader : ILoader
	{
		private string _rootDirectoryPath;

		public FileLoader(string rootDirectoryPath)
		{
			_rootDirectoryPath = rootDirectoryPath;
		}

#if WINDOWS_UWP
		public async Task<Stream> LoadStream(string gltfFilePath)
#else
		public Stream LoadStream(string gltfFilePath)
#endif
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}

#if WINDOWS_UWP
			return await LoadFileStream(_rootDirectoryPath, gltfFilePath);
#else
			return LoadFileStream(_rootDirectoryPath, gltfFilePath);
#endif
		}

#if WINDOWS_UWP
		private Task<Stream> LoadFileStream(string rootPath, string fileToLoad)
#else
		private Stream LoadFileStream(string rootPath, string fileToLoad)
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
			return File.OpenRead(pathToLoad);
#endif
		}
	}
}
