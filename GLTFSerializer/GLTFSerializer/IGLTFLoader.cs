using System.Threading.Tasks;

namespace GLTFSerializer
{
    public interface IGLTFLoader
    {
        Task<GLTFRoot> Load(string gltfUrl);
        Task<GLTFRoot> Load(System.IO.Stream stream);
#if WINDOWS_UWP
        Task<GLTFRoot> Load(Windows.Storage.Streams.IRandomAccessStream stream);
#endif
        Task<GLTFRoot> Load(byte[] gltfData);
    }
}
