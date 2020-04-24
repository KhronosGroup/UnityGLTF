using System;
using Newtonsoft.Json.Linq;
using GLTF.Math;
using Newtonsoft.Json;
using GLTF.Extensions;

namespace GLTF.Schema
{
	public class KHR_materials_unlitExtensionFactory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_materials_unlit";

		public KHR_materials_unlitExtensionFactory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			return new KHR_materials_unlitExtension();
		}
	}
}
