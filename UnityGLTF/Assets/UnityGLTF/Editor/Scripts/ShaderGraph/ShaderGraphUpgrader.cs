using GLTF.Schema;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF
{
	internal class ShaderGraphUpgrader : AssetPostprocessor
	{
		[InitializeOnLoadMethod]
		static void OnPackageImport()
		{
			// add a callback when a package imports
			AssetDatabase.importPackageCompleted += OnPackageImportCompleted;
		}

		private static void OnPackageImportCompleted(string packagename)
		{
			Debug.Log("Package imported: " + packagename);
		}

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			// check if this material needs updating
			if (importedAssets.Length == 0)
				return;

			foreach (var asset in importedAssets)
			{
				if (AssetDatabase.GetMainAssetTypeAtPath(asset) != typeof(Material))
					continue;

				var material = AssetDatabase.LoadAssetAtPath<Material>(asset);

				if (TryGetMetadataOfType<UnityGltfShaderUpgradeMeta>(material.shader, out var meta))
				{
					// we have to update this material to UnityGltf/PBRGraph or UnityGltf/UnlitGraph

					Debug.Log("Updating shader from  " + material.shader + " to " + meta.sourceShader +
					          " (transparent: " + meta.isTransparent + ", double sided: " + meta.isDoublesided + ")");

					var isUnlit = meta.sourceShader.name.Contains("Unlit");
					material.shader = meta.sourceShader;

					var mapper = isUnlit ? (IUniformMap) new UnlitMap(material) : new PBRGraphMap(material);
					if (meta.isTransparent)
						mapper.AlphaMode = AlphaMode.BLEND;
					if (meta.isDoublesided)
						mapper.DoubleSided = true;

					EditorUtility.SetDirty(material);
				}
			}
		}

		private static bool TryGetMetadataOfType<T>(Shader shader, out T obj) where T : ScriptableObject
		{
			obj = null;

			var path = AssetDatabase.GetAssetPath(shader);
			foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
			{
				if (asset is T metadataAsset)
				{
					obj = metadataAsset;
					return true;
				}
			}

			return false;
		}
	}
}
