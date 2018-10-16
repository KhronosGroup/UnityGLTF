#if WINDOWS_UWP
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using System;
using System.Collections;

namespace UnityGLTF.Loader
{
    public class StorageFolderLoader : ILoader
    {
        private StorageFolder _rootFolder;
        public Stream LoadedStream { get; private set; }

        public bool HasSyncLoadMethod => false;

        public StorageFolderLoader(StorageFolder rootFolder)
        {
            _rootFolder = rootFolder;
        }

        public IEnumerator LoadStream(string gltfFilePath)
        {
            if (gltfFilePath == null)
            {
                throw new ArgumentNullException("gltfFilePath");
            }
            
            yield return LoadStorageFile(gltfFilePath).AsCoroutine();
        }

        public void LoadStreamSync(string gltfFilePath)
        {
            throw new NotImplementedException();
        }


        public async Task LoadStorageFile(string path)
        {
            StorageFolder parentFolder = _rootFolder;
            string fileName = Path.GetFileName(path);
            if (path != fileName)
            {
                string folderToLoad = path.Substring(0, path.Length - fileName.Length);
                parentFolder = await _rootFolder.GetFolderAsync(folderToLoad);
            }

            StorageFile bufferFile = await parentFolder.GetFileAsync(fileName);
            LoadedStream = await bufferFile.OpenStreamForReadAsync();
        }
    }
}
#endif
