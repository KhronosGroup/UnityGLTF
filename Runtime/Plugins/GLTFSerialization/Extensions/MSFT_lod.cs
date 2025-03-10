using GLTF.Extensions;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace GLTF.Schema
{
	/// <summary>
	/// glTF extension that defines the LOD
	/// </summary>
	public class MSFT_LODExtension : IExtension
	{
		public List<int> NodeIds { get; set; }
		public MSFT_LODExtension(List<int> nodeIds)
		{
			NodeIds = nodeIds;
		}
		public IExtension Clone(GLTFRoot gltfRoot)
		{
			return new MSFT_LODExtension(NodeIds);
		}
		public JProperty Serialize()
		{
			JProperty jProperty = new JProperty(MSFT_LODExtensionFactory.EXTENSION_NAME,
				new JObject(
					new JProperty(MSFT_LODExtensionFactory.IDS, new JArray(NodeIds))
					)
				);
			return jProperty;
		}

		public List<double> GetLODCoverage(Node node)
		{
			List<double> lodCoverage = null;
			if (node.Extras != null)
			{
				JToken screenCoverageExtras = node.Extras[MSFT_LODExtensionFactory.SCREEN_COVERAGE_EXTRAS];
				if (screenCoverageExtras != null)
				{
					lodCoverage = screenCoverageExtras.CreateReader().ReadDoubleList();
				}
			}

			return lodCoverage;
        }
    }
}
