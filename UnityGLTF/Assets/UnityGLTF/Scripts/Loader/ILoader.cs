using System.Collections;
using System.IO;
using GLTF;
using GLTF.Schema;
#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace UnityGLTF.Loader
{
	public interface ILoader
	{
		IEnumerator LoadStream(string relativeFilePath);

		Stream LoadedStream { get; }
	}
}
