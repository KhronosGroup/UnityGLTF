using System;
#if HAVE_MESHOPT_DECOMPRESS
using Meshoptimizer;
#endif
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{

	[Serializable]
	public class EXT_meshopt_compression : IExtension
	{
		public BufferView bufferView;
		public int count;
		public bool isFallbackBuffer = false;

#if HAVE_MESHOPT_DECOMPRESS
		public Mode mode;
		public Filter filter;
#endif

		public JProperty Serialize()
		{
			throw new NotSupportedException("EXT_meshopt_compression serialization is not supported yet.");
		}

		public IExtension Clone(GLTFRoot root)
		{
			// TODO: Clone the bufferView
			return new EXT_meshopt_compression
			{
				isFallbackBuffer = isFallbackBuffer,
				bufferView = bufferView,
#if HAVE_MESHOPT_DECOMPRESS
				mode = mode,
				filter = filter,
#endif
				count = count
			};
		}
	}

	public class EXT_meshopt_compression_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "EXT_meshopt_compression";

		public EXT_meshopt_compression_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new EXT_meshopt_compression();

				extension.isFallbackBuffer = extensionToken.Value["fallback"]?.Value<bool>() ?? false;

#if HAVE_MESHOPT_DECOMPRESS
				extension.mode = (Mode)System.Enum.Parse(typeof(Mode), extensionToken.Value["mode"]?.Value<string>() ?? "Undefined", true);
				extension.filter = (Filter)System.Enum.Parse(typeof(Filter), extensionToken.Value["filter"]?.Value<string>() ?? "Undefined", true);
				extension.count = extensionToken.Value["count"]?.Value<int>() ?? 0;

				if (extensionToken.Value["buffer"] != null)
				{
					extension.bufferView = new BufferView();
					extension.bufferView.Buffer  =  new BufferId
					{
						Id = extensionToken.Value["buffer"]?.Value<int>() ?? -1,
						Root = root
					};
					extension.bufferView.ByteOffset =  (uint)System.Math.Round(extensionToken.Value["byteOffset"]?.Value<double>() ?? 0);
					extension.bufferView.ByteLength =  (uint)System.Math.Round(extensionToken.Value["byteLength"]?.Value<double>() ?? 0);
					extension.bufferView.ByteStride =  (uint)System.Math.Round(extensionToken.Value["byteStride"]?.Value<double>() ?? 0);
				}
#endif

				return extension;
			}

			return null;
		}
	}
}
