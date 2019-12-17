using System.IO;
using System.Threading.Tasks;

namespace UnityGLTF.Loader
{
#pragma warning disable CS0618 // Type or member is obsolete
	/// <summary>
	/// Wrapper utility for exposing an <see cref="ILoader"/> as a <see cref="IDataLoader"/> (and if appropriate, <see cref="IDataLoader2"/>).
	/// </summary>
	public static class LegacyLoaderWrapper
	{
		public static IDataLoader Wrap(ILoader loader)
		{
			if (loader == null)
			{
				return null;
			}
			if (loader.HasSyncLoadMethod)
			{
				return new LegacyLoader2(loader);
			}
			return new LegacyLoader(loader);
		}

		private class LegacyLoader : IDataLoader
		{
			protected readonly ILoader _loader;

			public LegacyLoader(ILoader loader)
			{
				_loader = loader;
			}

			public Task<Stream> LoadStreamAsync(string relativeFilePath)
			{
				_loader.LoadStream(relativeFilePath);
				return Task.FromResult(_loader.LoadedStream);
			}
		}

		private class LegacyLoader2 : LegacyLoader, IDataLoader2
		{
			public LegacyLoader2(ILoader loader) :
				base(loader)
			{
			}

			public Stream LoadStream(string relativeFilePath)
			{
				_loader.LoadStreamSync(relativeFilePath);
				return _loader.LoadedStream;
			}
		}
	}
#pragma warning restore CS0618 // Type or member is obsolete
}
