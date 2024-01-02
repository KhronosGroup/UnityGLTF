using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Plugins;

namespace UnityGLTF
{
	public partial class GLTFSceneImporter
	{
		private async Task ConstructLods(GLTFRoot gltfRoot, GameObject nodeObj, Node node, int nodeIndex, CancellationToken cancellationToken)
		{
			if (!Context.TryGetPlugin<LodsImportContext>(out _)) return;
			
			const string msft_LODExtName = MSFT_LODExtensionFactory.EXTENSION_NAME;
			MSFT_LODExtension lodsExtension = null;
			if (_gltfRoot.ExtensionsUsed != null
			    && _gltfRoot.ExtensionsUsed.Contains(msft_LODExtName)
			    && node.Extensions != null
			    && node.Extensions.ContainsKey(msft_LODExtName))
			{
				lodsExtension = node.Extensions[msft_LODExtName] as MSFT_LODExtension;
				if (lodsExtension != null && lodsExtension.NodeIds.Count > 0)
				{
					int lodCount = lodsExtension.NodeIds.Count + 1;
					if (CullFarLOD)
					{
						lodCount += 1;
					}
					LOD[] lods = new LOD[lodCount];
					List<double> lodCoverage = lodsExtension.GetLODCoverage(node);
					if (lodCoverage == null)
						lodCoverage = new List<double>();

					var hasExplicitLodCoverage = lodCoverage.Count > 0;
					if (lodCoverage.Count != lods.Length)
					{
						if (!hasExplicitLodCoverage)
							lodCoverage.Add(0.5f);

						var count = lodCoverage.Count;
						var length = lods.Length;
						for (var i = count; i < length; i++)
						{
							lodCoverage.Add(lodCoverage[i - 1] / 2);
						}

						if (!CullFarLOD && !hasExplicitLodCoverage)
							lodCoverage[lodCoverage.Count - 1] = 0;
					}

					// sanitize values so that they are strictly decreasing
					for (int i = lodCoverage.Count - 2; i >= 0; i--)
					{
						if (lodCoverage[i] <= lodCoverage[i + 1])
							lodCoverage[i] = lodCoverage[i + 1] + 0.01f;
					}

					var lodGroupNodeObj = nodeObj;
					lodGroupNodeObj.SetActive(false);
					var firstLodChildRenderers = nodeObj.GetComponentsInChildren<Renderer>().ToList();

					LODGroup lodGroup = lodGroupNodeObj.AddComponent<LODGroup>();
					for (int i = 0; i < lodsExtension.NodeIds.Count; i++)
					{
						int lodNodeId = lodsExtension.NodeIds[i];
						var lodNodeObj = await GetNode(lodNodeId, cancellationToken);
						// progressStatus.NodeTotal++;
						lodNodeObj.transform.SetParent(lodGroupNodeObj.transform, false);
						var childRenderers = lodNodeObj.GetComponentsInChildren<Renderer>();
						int lodIndex = i + 1;
						// make sure to kick out the renderers that are used in lower LOD levels from the root LOD level
						foreach (var child in childRenderers)
							firstLodChildRenderers.Remove(child);
						lods[lodIndex] = new LOD(GetLodCoverage(lodCoverage, lodIndex), childRenderers);
					}

					lods[0] = new LOD(GetLodCoverage(lodCoverage, 0), firstLodChildRenderers.ToArray());

					if (CullFarLOD)
					{
						//use the last mesh as the LOD
						lods[lodsExtension.NodeIds.Count + 1] = new LOD(0, null);
					}

					lodGroup.SetLODs(lods);
					lodGroup.RecalculateBounds();
					lodGroupNodeObj.SetActive(true);
					_assetCache.NodeCache[nodeIndex] = lodGroupNodeObj;
				}
			}
		}
	}
}
