using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF
{
	public partial class GLTFSceneImporter
	{
		private async Task ConstructLods(GLTFRoot gltfRoot, GameObject nodeObj, Node node, int nodeIndex, CancellationToken cancellationToken)
		{
			const string msft_LODExtName = MSFT_LODExtensionFactory.EXTENSION_NAME;
			MSFT_LODExtension lodsExtension = null;
			if (_gltfRoot.ExtensionsUsed != null
			    && _gltfRoot.ExtensionsUsed.Contains(msft_LODExtName)
			    && node.Extensions != null
			    && node.Extensions.ContainsKey(msft_LODExtName))
			{
				lodsExtension = node.Extensions[msft_LODExtName] as MSFT_LODExtension;
				if (lodsExtension != null && lodsExtension.MeshIds.Count > 0)
				{
					int lodCount = lodsExtension.MeshIds.Count + 1;
					if (CullFarLOD)
					{
						lodCount += 1;
					}
					LOD[] lods = new LOD[lodCount];
					List<double> lodCoverage = lodsExtension.GetLODCoverage(node);
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

					// var lodGroupNodeObj = new GameObject(string.IsNullOrEmpty(node.Name) ? ("GLTFNode_LODGroup" + nodeIndex) : node.Name);
					var lodGroupNodeObj = nodeObj;
					lodGroupNodeObj.SetActive(false);
					// nodeObj.transform.SetParent(lodGroupNodeObj.transform, false);
					MeshRenderer[] childRenders = nodeObj.GetComponentsInChildren<MeshRenderer>();
					lods[0] = new LOD(GetLodCoverage(lodCoverage, 0), childRenders);

					LODGroup lodGroup = lodGroupNodeObj.AddComponent<LODGroup>();
					for (int i = 0; i < lodsExtension.MeshIds.Count; i++)
					{
						int lodNodeId = lodsExtension.MeshIds[i];
						var lodNodeObj = await GetNode(lodNodeId, cancellationToken);
						progressStatus.NodeTotal++;
						lodNodeObj.transform.SetParent(lodGroupNodeObj.transform, false);
						childRenders = lodNodeObj.GetComponentsInChildren<MeshRenderer>();
						int lodIndex = i + 1;
						lods[lodIndex] = new LOD(GetLodCoverage(lodCoverage, lodIndex), childRenders);
					}

					if (CullFarLOD)
					{
						//use the last mesh as the LOD
						lods[lodsExtension.MeshIds.Count + 1] = new LOD(0, null);
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
