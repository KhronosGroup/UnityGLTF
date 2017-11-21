// Copyright (c) Microsoft Corporation. All rights reserved.

#if WINDOWS_UWP
using System.IO;
using System.Threading.Tasks;
using GLTF.Schema;
using UnityEngine;
using Windows.Storage;
using System;

namespace UnityGLTF.Loader
{
	public class StorageFolderLoader : ILoader
	{
		private StorageFolder _rootFolder;

		public StorageFolderLoader(StorageFolder rootFolder)
		{
			_rootFolder = rootFolder;
		}

		public Task<Stream> LoadStream(string gltfFilePath)
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}

			return LoadStorageFile(gltfFilePath);
		}

		public async Task<Stream> LoadStorageFile(string path)
		{
			StorageFolder parentFolder = _rootFolder;
			string fileName = Path.GetFileName(path);
			if (path != fileName)
			{
				string folderToLoad = path.Substring(0, path.Length - fileName.Length);
				parentFolder = await _rootFolder.GetFolderAsync(folderToLoad);
			}

			StorageFile bufferFile = await parentFolder.GetFileAsync(fileName);
			return await bufferFile.OpenStreamForReadAsync();
		}
	}
}
#endif
