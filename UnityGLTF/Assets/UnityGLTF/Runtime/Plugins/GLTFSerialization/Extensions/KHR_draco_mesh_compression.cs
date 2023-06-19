using System;
using System.Collections.Generic;
using GLTF.Extensions;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	[Serializable]
	public class KHR_draco_mesh_compression : IExtension
	{
		public Dictionary<string, int> attributes = new Dictionary<string, int>();
		public BufferViewId bufferView;

		public JProperty Serialize()
		{
			throw new NotSupportedException("KHR_draco_mesh_compression serialization is not supported yet.");
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_draco_mesh_compression
			{
				attributes = new Dictionary<string, int>(attributes)
			};
		}
	}

	public class KHR_draco_mesh_compression_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_draco_mesh_compression";

		public KHR_draco_mesh_compression_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new KHR_draco_mesh_compression();
				var attributeToken = extensionToken.Value[nameof(KHR_draco_mesh_compression.attributes)];
				if (attributeToken != null)
				{
					var reader = attributeToken.CreateReader();
					extension.attributes = reader.ReadAsDictionary(() => reader.ReadAsInt32().Value);
				}

				var bufferViewToken = extensionToken.Value[nameof(KHR_draco_mesh_compression.bufferView)];
				if (bufferViewToken != null)
					extension.bufferView = BufferViewId.Deserialize(root, bufferViewToken.CreateReader());
				return extension;
			}

			return null;
		}
	}
}
