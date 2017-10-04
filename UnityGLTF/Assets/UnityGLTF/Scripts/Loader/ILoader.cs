using System.IO;
using GLTF;
using GLTF.Schema;

namespace UnityGLTF.Loader
{
	public interface ILoader
	{
		Stream LoadJSON(string gltfFilePath);
		Stream LoadBuffer(Buffer buffer);
		UnityEngine.Texture2D LoadImage(Image image);
	}
}
