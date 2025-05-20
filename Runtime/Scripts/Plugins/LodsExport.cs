using System.Collections.Generic;
using System.Linq;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
	[NonRatifiedPlugin]
	public class LodsExport : GLTFExportPlugin
	{
		public override string DisplayName => "MSFT_lod";
		public override string Description => "Exports LODGroup components as MSFT_lod extension.";
		public override GLTFExportPluginContext CreateInstance(ExportContext context)
		{
			return new MSFT_lods_Extension();
		}
	}
	
	public class MSFT_lods_Extension: GLTFExportPluginContext
	{
		public override void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfroot, Transform transform, Node node)
		{
			if (!transform) return;

			var lodGroup = transform.GetComponent<LODGroup>();
			if (!lodGroup) return;

			var lods = lodGroup.GetLODs();
			var usesCulling = lods[lods.Length - 1].renderers.Length == 0;
			var nodeIds = new int[lods.Length - 1 - (usesCulling ? 1 : 0)];
			var coverages = new float[nodeIds.Length + 1];
			for (var index = 0; index < nodeIds.Length; index++)
			{
				var lod = lods[index + 1];

				// TODO multiple renderers could be supported if the user ensures
				// that all renderers in a LOD level are child of one node – then we could export that node's ID
				if (lod.renderers.Length != 1)
				{
					Debug.LogWarning("Can't export LODGroup with MSFT_lods with more than one renderer per LOD level. Skipping", lodGroup);
					return;
				}
				var meshFilter = lod.renderers[0].GetComponent<MeshFilter>();
				if (!meshFilter)
				{
					Debug.LogWarning("At least one renderer in LODGroup doesn't have a mesh. Skipping", lodGroup);
					return;
				}

				var lodNode = exporter.ExportNode(lod.renderers[0].gameObject);
				if (lodNode != null) nodeIds[index] = lodNode.Id;

				coverages[index] = lod.screenRelativeTransitionHeight;
			}
			// if (usesCulling) coverages[coverages.Length - 1] = 0;

			// TODO implement coverage export too
			var ext = new MSFT_LODExtension(nodeIds.ToList());
			// var coverageExt = new MSFT_LODExtension(coverages.ToList());
			// for (int i = 0; i < ext.Hints.Length; i++)
			// {
			// 	ext.Hints[i] = new MSFT_lodsHint();
			// 	ext.Hints[i].ScreenCoverage = lodGroup.GetLODs()[i].screenRelativeTransitionHeight;
			// }

			// TODO according to the MSFT_lod docs, nodes should be kicked out of the scene again
			// to ensure that softwares that can't read MSFT_lods don't render them.

			if (node.Extensions == null) node.Extensions = new Dictionary<string, IExtension>();
			node.Extensions.Add(MSFT_LODExtensionFactory.EXTENSION_NAME, ext);
			exporter.DeclareExtensionUsage(MSFT_LODExtensionFactory.EXTENSION_NAME, false);
		}
	}
}
