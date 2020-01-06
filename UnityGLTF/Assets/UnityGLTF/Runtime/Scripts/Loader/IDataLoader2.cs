using System.IO;

namespace UnityGLTF.Loader
{
	public interface IDataLoader2 : IDataLoader
	{
		Stream LoadStream(string relativeFilePath);
	}
}
