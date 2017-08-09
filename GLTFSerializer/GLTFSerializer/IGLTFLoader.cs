using System.Threading.Tasks;

namespace GLTFSerializer
{
    public interface IGLTFLoader
    {
        Task<GLTFRoot> Load(string gltfUrl);
        Task<GLTFRoot> Load(System.IO.Stream stream);
        Task<GLTFRoot> Load(byte[] gltfData);
    }
}
