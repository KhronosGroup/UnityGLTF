using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		
		internal static void OnImmutableMaterialChanged(Material material)
		{
			if (!material) return;
			if (!AssetDatabase.Contains(material)) return;

			var assetPath = AssetDatabase.GetAssetPath(material);

			// TODO handle case where mainAsset is a GameObject; we can still write materials back in that case
			// var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);

			// Transform[] rootTransforms = null;
			var importer = AssetImporter.GetAtPath(assetPath) as GLTFImporter;
			if (!importer) return;
			var materialsToExport = importer.m_Materials.Where(x => x is Material).Cast<Material>().ToList();

			SaveAssetWithMaterials(assetPath, materialsToExport);
		}
		
		internal static void SaveAssetWithMaterials(string assetPath, List<Material> materials)
		{
			var importer = AssetImporter.GetAtPath(assetPath) as GLTFImporter;
			var exporter = new GLTFSceneExporter((Transform[]) null, new ExportContext());
			// load all materials from mainAsset

			// var allObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
			foreach (var mat in materials)
			{
				if (!mat) continue;
				exporter.ExportMaterial(mat);
			}

			var path = Path.GetDirectoryName(assetPath);
			var name = Path.GetFileName(assetPath);

			// Save file and make sure we reimport it
			exporter.SaveGLTFandBin(path, name, false);
			AssetDatabase.ImportAsset(path);

			// add texture remaps, so that the textures we just exported with stay valid.
			// We want to always have default paths in the file, and remapping is done on the Unity side to a project specific path,
			// to avoid breaking references all the time when paths change.
			var exportedTextures = exporter.GetRoot().Textures;
			var importedTextures = importer.m_Textures;
			// If these don't match, we could only try by name... not ideal.
			// They may not match due to different sampler settings etc.
			if (exportedTextures?.Count == importedTextures.Length)
			{
				for (int i = 0; i < exportedTextures.Count; i++)
				{
					var exported = exportedTextures[i];
					var imported = importedTextures[i];
					// TODO could we also use the imported texture definitions instead of the actual imported textures?
					// Then we wouldn't need to create mock textures on import for all missing/remapped ones.
					if (exported.Name != imported.name)
					{
						Debug.LogWarning($"Texture name mismatch: {exported.Name} != {imported.name}");
					}

					var sourceTextureForExportedTexture = exporter.GetSourceTextureForExportedTexture(exported);

					// we should not remap if that is actually the same texture
					if (imported == sourceTextureForExportedTexture) continue;

					importer.AddRemap(new AssetImporter.SourceAssetIdentifier(imported), sourceTextureForExportedTexture);
				}
				importer.SaveAndReimport();
			}

			// TODO we should get rid of this, but without it currently the inspector doesn't repaint
			// after importing a changed material, which can be confusing. Could be caching inside PBRGraphGUI
			AssetDatabase.Refresh();

			EditorApplication.delayCall += () =>
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
