using System;
using System.Collections.Generic;
using GLTF.Extensions;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
    // https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Vendor/EXT_mesh_gpu_instancing/
	[Serializable]
	public class EXT_mesh_gpu_instancing : IExtension
	{
		public const string ATTRIBUTE_TRANSLATION = "TRANSLATION";
		public const string ATTRIBUTE_ROTATION = "ROTATION";
		public const string ATTRIBUTE_SCALE = "SCALE";
		
		public Dictionary<string, AccessorId> attributes = new Dictionary<string, AccessorId>();

		public JProperty Serialize()
		{
			return null;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new EXT_mesh_gpu_instancing()
			{
				attributes = new Dictionary<string, AccessorId>(attributes)
			};
		}
	}

	public class EXT_mesh_gpu_instancing_Factory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "EXT_mesh_gpu_instancing";

		public EXT_mesh_gpu_instancing_Factory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			if (extensionToken != null)
			{
				var extension = new EXT_mesh_gpu_instancing();
				var attributeToken = extensionToken.Value[nameof(EXT_mesh_gpu_instancing.attributes)];
				if (attributeToken != null)
				{
					var reader = attributeToken.CreateReader();
					extension.attributes = reader.ReadAsDictionary(() => new AccessorId
					{
						Id = reader.ReadAsInt32().Value,
						Root = root
					});
				}
				return extension;
			}

			return null;
		}
	}
}
