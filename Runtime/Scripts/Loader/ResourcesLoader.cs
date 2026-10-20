using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityGLTF.Loader
{
    public class ResourcesLoader : IDataLoader
    {
        public ResourcesLoader() { }



        public async Task<Stream> LoadStreamAsync(string relativeFilePath)
        {
            var handle = Resources.LoadAsync<TextAsset>(relativeFilePath);

            while (!handle.isDone)
                await Task.Yield();

            return new MemoryStream((handle.asset as TextAsset).bytes);
        }
    }
}
