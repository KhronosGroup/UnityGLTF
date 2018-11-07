using System.Collections;
using System.IO;
using System.Threading.Tasks;
using GLTF;
using GLTF.Schema;
#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace UnityGLTF.Loader
{
	public interface ILoader
	{
		Task LoadStream(string relativeFilePath);

		void LoadStreamSync(string jsonFilePath);

		Stream LoadedStream { get; }

		bool HasSyncLoadMethod { get; }
	}
}
