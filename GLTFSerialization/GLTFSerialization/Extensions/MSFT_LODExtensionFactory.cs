using System;
using Newtonsoft.Json.Linq;
using GLTF.Math;
using Newtonsoft.Json;
using GLTF.Extensions;
using System.Collections.Generic;

namespace GLTF.Schema
{
	public class MSFT_LODExtensionFactory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "MSFT_lod";
		public const string SCREEN_COVERAGE_EXTRAS = "MSFT_screencoverage";
		public const string IDS = "ids";
	
		public MSFT_LODExtensionFactory()
		{
			ExtensionName = EXTENSION_NAME;
		}


		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			List<int> meshIds = new List<int>();
			if (extensionToken != null)
			{
#if DEBUG
				// Broken on il2cpp. Don't ship debug DLLs there.
				System.Diagnostics.Debug.WriteLine(extensionToken.Value.ToString());
				System.Diagnostics.Debug.WriteLine(extensionToken.Value.Type);
#endif
				JToken meshIdsToken = extensionToken.Value[IDS];
				meshIds = meshIdsToken.CreateReader().ReadInt32List();
			}

			return new MSFT_LODExtension(meshIds);
		}
	}
}
