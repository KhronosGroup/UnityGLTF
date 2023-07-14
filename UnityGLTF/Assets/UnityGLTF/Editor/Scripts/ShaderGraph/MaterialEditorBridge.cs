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
			if (!material) return;
			if (!AssetDatabase.Contains(material)) return;

			var assetPath = AssetDatabase.GetAssetPath(material);

			// TODO handle case where mainAsset is a GameObject; we can still write materials back in that case
			var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
			// Transform[] rootTransforms = null;
			var exporter = new GLTFSceneExporter((Transform[]) null, new ExportOptions());

			// load all materials from mainAsset
			var importer = AssetImporter.GetAtPath(assetPath) as GLTFImporter;
			if (!importer) return;

			// var allObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
			foreach (var obj in importer.ImportedMaterials)
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

			// Save file and make sure we reimport it
			exporter.SaveGLTFandBin(path, name);
			AssetDatabase.ImportAsset(path);

			// TODO we should get rid of this, but without it currently the inspector doesn't repaint
			// after importing a changed material, which can be confusing. Could be caching inside PBRGraphGUI
			AssetDatabase.Refresh();

			EditorApplication.update += () =>
			{
				// Repaint Inspector, newly imported values can be different if we're not perfectly round tripping
				foreach (var editor in ActiveEditorTracker.sharedTracker.activeEditors)
				{
					editor.Repaint();
				}
			};
		}
	}
}
