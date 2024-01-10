using System.IO;
using System.Threading.Tasks;

namespace UnityGLTF.Loader
{
	public interface IDataLoader
	{
		Task<Stream> LoadStreamAsync(string relativeFilePath);
	}

	/// <summary>
	/// Add this Interface to a IDataLoader to indicate that it supports multithreading.
	/// </summary>
	public interface IMultiThreadingSupported { }
}
