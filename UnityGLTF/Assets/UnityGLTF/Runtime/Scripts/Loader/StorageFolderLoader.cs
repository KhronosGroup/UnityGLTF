#if WINDOWS_UWP
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using System;
using System.Collections;

namespace UnityGLTF.Loader
{
    public class StorageFolderLoader : IDataLoader
    {
        private StorageFolder _rootFolder;

        public StorageFolderLoader(StorageFolder rootFolder)
        {
            _rootFolder = rootFolder;
        }

        public async Task<Stream> LoadStreamAsync(string relativeFilePath)
        {
            if (relativeFilePath == null)
            {
                throw new ArgumentNullException("relativeFilePath");
            }

            StorageFolder parentFolder = _rootFolder;
            string fileName = Path.GetFileName(relativeFilePath);
            if (relativeFilePath != fileName)
            {
                string folderToLoad = relativeFilePath.Substring(0, relativeFilePath.Length - fileName.Length);
                parentFolder = await _rootFolder.GetFolderAsync(folderToLoad);
            }

            StorageFile bufferFile = await parentFolder.GetFileAsync(fileName);
            return await bufferFile.OpenStreamForReadAsync();
        }
    }
}
#endif
