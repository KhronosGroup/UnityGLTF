using GLTF.Schema;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityGLTF
{
	public interface ITextureLoader
	{
		bool CanLoadTexture(GLTFImage image);
		Task<Texture2D> LoadTextureAsync(Stream stream, bool markGpuOnly, bool isLinear, GLTFImage image, AsyncCoroutineHelper asyncCoroutineHelper);
	}
}
