using System.Threading.Tasks;

namespace GLTFJsonSerialization
{
    public interface IGLTFJsonLoader
    {
        GLTFRoot Load(System.IO.Stream stream);
#if WINDOWS_UWP
        Task<GLTFRoot> Load(Windows.Storage.Streams.IRandomAccessStream stream);
#endif
        GLTFRoot Load(byte[] gltfData);
    }
}
