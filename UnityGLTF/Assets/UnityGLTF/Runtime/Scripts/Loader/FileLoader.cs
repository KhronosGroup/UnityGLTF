using System.IO;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityGLTF.Loader
{
	public class FileLoader : IDataLoader, IDataLoader2
	{
		private readonly string _rootDirectoryPath;

		public FileLoader(string rootDirectoryPath)
		{
			_rootDirectoryPath = rootDirectoryPath;
		}

		public Task<Stream> LoadStreamAsync(string relativeFilePath)
		{
			// seems the Editor locks up in some cases when directly using Task.Run(() => {})
			if (Application.isPlaying)
			{
				return Task.Run(() => LoadStream(relativeFilePath));
			}
			return Task.FromResult(LoadStream(relativeFilePath));
		}

		public Stream LoadStream(string relativeFilePath)
		{
			if (relativeFilePath == null)
			{
				throw new ArgumentNullException("relativeFilePath");
			}

			string pathToLoad = Path.Combine(_rootDirectoryPath, relativeFilePath);
			if (!File.Exists(pathToLoad))
			{
				throw new FileNotFoundException("Buffer file not found", relativeFilePath);
			}

			return File.OpenRead(pathToLoad);
		}
	}
}
