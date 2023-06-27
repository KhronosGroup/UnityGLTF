using System;
using System.Collections.Generic;
using GLTF.Extensions;
#if HAVE_MESHOPT_DECOMPRESS
using Meshoptimizer;
#endif

using Newtonsoft.Json.Linq;namespace GLTF.Schema
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

				// var readerFallback = extensionToken.CreateReader();
				// readerFallback.ReadAsDictionary()
				//
				// if (extensionToken.First. () ("fallback") extension.attributes.ContainsKey("fallback"))
				// {
				// 	extension.isFallbackBuffer = extension.attributes["fallback"].ToString() == "true";
				// }

				// var attributeToken = extensionToken.Value[nameof(EXT_meshopt_compression.attributes)];
				// if (attributeToken != null)
				// {
				// 	var reader = attributeToken.CreateReader();
				// 	extension.attributes = reader.ReadAsDictionary(() => reader.ReadAsInt32().Value);
				//
				// }


#if HAVE_MESHOPT_DECOMPRESS
				extension.mode = (Mode)System.Enum.Parse(typeof(Mode), extensionToken.Value["mode"]?.Value<string>() ?? "Undefined", true); ;
				extension.filter = (Filter)System.Enum.Parse(typeof(Filter), extensionToken.Value["filter"]?.Value<string>() ?? "Undefined", true); ;
				if (extensionToken.Value["buffer"] != null)
				{

				extension.bufferView = BufferView.Deserialize(root, extensionToken.Value["buffer"].CreateReader());
				}
#endif


				return extension;
			}

			return null;
		}
	}
}
