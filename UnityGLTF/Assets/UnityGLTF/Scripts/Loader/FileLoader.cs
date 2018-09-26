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

		public bool HasSyncLoadMethod { get; private set; }

		public FileLoader(string rootDirectoryPath)
		{
			_rootDirectoryPath = rootDirectoryPath;
			HasSyncLoadMethod = true;
		}

		public IEnumerator LoadStream(string gltfFilePath)
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}

			yield return LoadFileStream(_rootDirectoryPath, gltfFilePath);
		}

		private IEnumerator LoadFileStream(string rootPath, string fileToLoad)
		{
			string pathToLoad = Path.Combine(rootPath, fileToLoad);
			if (!File.Exists(pathToLoad))
			{
				throw new FileNotFoundException("Buffer file not found", fileToLoad);
			}

			yield return null;
			LoadedStream = File.OpenRead(pathToLoad);
		}

		public void LoadStreamSync(string gltfFilePath)
 	    {
 	        if (gltfFilePath == null)
 	        {
 	            throw new ArgumentNullException("gltfFilePath");
 	        }
 
 	        LoadFileStreamSync(_rootDirectoryPath, gltfFilePath);
 	    }
 
 	    private void LoadFileStreamSync(string rootPath, string fileToLoad)
 	    {
 	        string pathToLoad = Path.Combine(rootPath, fileToLoad);
 	        if (!File.Exists(pathToLoad))
 	        {
 	            throw new FileNotFoundException("Buffer file not found", fileToLoad);
 	        }
 
 	        LoadedStream = File.OpenRead(pathToLoad);
 	    }
	}
}
