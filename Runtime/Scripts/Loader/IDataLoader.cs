using System.IO;
using System.Threading.Tasks;

namespace UnityGLTF.Loader
{
	public interface IDataLoader
	{
		Task<Stream> LoadStreamAsync(string relativeFilePath);
	}
}
