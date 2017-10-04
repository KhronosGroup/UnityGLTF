using System.IO;
using GLTF;
using UnityEngine;
using System;

namespace UnityGLTF.Loader
{
	public class FileLoader : ILoader
	{
		private string _rootDirectoryPath;

		public FileLoader(string rootDirectoryPath)
		{
			_rootDirectoryPath = rootDirectoryPath;
		}
		
		public Stream LoadJSON(string gltfFilePath)
		{
			if(gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}

			return LoadFileStream(_rootDirectoryPath, gltfFilePath);
		}

		public Stream LoadBuffer(GLTF.Schema.Buffer buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}

			if (buffer.Uri == null)
			{
				throw new ArgumentException("Cannot load buffer with null URI. Should be loaded via GLB method instead", "buffer");
			}

			return LoadFileStream(_rootDirectoryPath, buffer.Uri);
		}

		public Texture2D LoadImage(GLTF.Schema.Image image)
		{
			if(image == null)
			{
				throw new ArgumentNullException("image");
			}

			if(image.Uri == null)
			{
				throw new ArgumentException("URI property of image should not be null", "image");
			}

			Texture2D texture = null;
			string pathToLoad = Path.Combine(_rootDirectoryPath, image.Uri);
			Stream fileStream = File.OpenRead(pathToLoad);
			if (fileStream.Length > int.MaxValue)
			{
				throw new Exception("Stream is larger than can be copied into byte array");
			}

			byte[] bufferData = new byte[fileStream.Length];
			fileStream.Read(bufferData, 0, (int)fileStream.Length);

#if !WINDOWS_UWP
			fileStream.Close();
#else
			fileStream.Dispose();
#endif
			texture = new Texture2D(0, 0);
			texture.LoadImage(bufferData);
			texture.Apply();
			return texture;
		}

		private Stream LoadFileStream(string rootPath, string fileToLoad)
		{
			string pathToLoad = Path.Combine(_rootDirectoryPath, fileToLoad);
			if (!File.Exists(pathToLoad))
			{
				throw new FileNotFoundException("Buffer file not found", fileToLoad);
			}

			return File.OpenRead(pathToLoad);
		}
	}
}
