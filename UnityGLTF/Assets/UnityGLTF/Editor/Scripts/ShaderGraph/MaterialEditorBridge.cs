using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF
{
	internal static class MaterialEditorBridge
	{
		[InitializeOnLoadMethod]
		private static void ConnectGltfExporterToPbrGraphGUI()
		{
			PBRGraphGUI.ImmutableMaterialChanged += OnImmutableMaterialChanged;
		}

		private static void OnImmutableMaterialChanged(Material material)
		{
			var assetPath = AssetDatabase.GetAssetPath(material);

			// TODO handle case where mainAsset is a GameObject; we can still write materials back in that case
			var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
			Transform[] rootTransforms = null;
			var exporter = new GLTFSceneExporter((Transform[]) null, new ExportOptions());

			// load all materials from mainAsset
			var allObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
			foreach (var obj in allObjects)
			{
				if (!(obj is Material mat))
				{
					// TODO warn that there are extra objects we can't store right now
					continue;
				}

				exporter.ExportMaterial(mat);
			}

			var path = Path.GetDirectoryName(assetPath);
			var name = Path.GetFileName(assetPath);
			exporter.SaveGLTFandBin(path, name);
		}
	}
}
