using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityGLTF
{
	public static class GLTFExportMenu
	{
		private const string MenuPrefix = "Assets/UnityGLTF/";

	    public static string RetrieveTexturePath(UnityEngine.Texture texture)
	    {
	        var path = AssetDatabase.GetAssetPath(texture);
	        // texture is a subasset
	        if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(Texture2D))
	        {
		        var ext = System.IO.Path.GetExtension(path);
		        if (string.IsNullOrWhiteSpace(ext)) return texture.name + ".png";
		        path = path.Replace(ext, "-" + texture.name + ext);
	        }
	        return path;
	    }

	    private static bool TryGetExportNameAndRootTransformsFromSelection(out string sceneName, out Transform[] rootTransforms)
	    {
		    if (Selection.transforms.Length > 1)
		    {
			    sceneName = SceneManager.GetActiveScene().name;
			    rootTransforms = Selection.transforms;
			    return true;
		    }
		    if (Selection.transforms.Length == 1)
		    {
			    sceneName = Selection.activeGameObject.name;
			    rootTransforms = Selection.transforms;
			    return true;
		    }
		    if (Selection.objects.Any() && Selection.objects.All(x => x is GameObject))
		    {
			    sceneName = Selection.objects.First().name;
			    rootTransforms = Selection.objects.Select(x => (x as GameObject).transform).ToArray();
			    return true;
		    }

		    sceneName = null;
		    rootTransforms = null;
		    return false;
	    }

	    [MenuItem(MenuPrefix + "Export selected as glTF", true)]
	    private static bool ExportSelectedValidate()
	    {
		    return TryGetExportNameAndRootTransformsFromSelection(out _, out _);
	    }

	    [MenuItem(MenuPrefix + "Export selected as glTF")]
	    private static void ExportSelected()
		{
			if (!TryGetExportNameAndRootTransformsFromSelection(out var sceneName, out var rootTransforms))
			{
				Debug.LogError("Can't export: selection is empty");
				return;
			}

			Export(rootTransforms, false, sceneName);
		}

		[MenuItem(MenuPrefix + "Export selected as GLB", true)]
		private static bool ExportGLBSelectedValidate()
		{
			return TryGetExportNameAndRootTransformsFromSelection(out _, out _);
		}

		[MenuItem(MenuPrefix + "Export selected as GLB")]
		private static void ExportGLBSelected()
		{
			if (!TryGetExportNameAndRootTransformsFromSelection(out var sceneName, out var rootTransforms))
			{
				Debug.LogError("Can't export: selection is empty");
				return;
			}
			Export(rootTransforms, true, sceneName);
		}

		[MenuItem(MenuPrefix + "Export active scene as glTF")]
		private static void ExportScene()
		{
			var scene = SceneManager.GetActiveScene();
			var gameObjects = scene.GetRootGameObjects();
			var transforms = Array.ConvertAll(gameObjects, gameObject => gameObject.transform);

			Export(transforms, false, scene.name);
		}

		[MenuItem(MenuPrefix + "Export active scene as GLB")]
		private static void ExportSceneGLB()
		{
			var scene = SceneManager.GetActiveScene();
			var gameObjects = scene.GetRootGameObjects();
			var transforms = Array.ConvertAll(gameObjects, gameObject => gameObject.transform);

			Export(transforms, true, scene.name);
		}

		private static void Export(Transform[] transforms, bool binary, string sceneName)
		{
			var settings = GLTFSettings.GetOrCreateSettings();
			var exportOptions = new ExportOptions { TexturePathRetriever = RetrieveTexturePath };
			var exporter = new GLTFSceneExporter(transforms, exportOptions);

			var invokedByShortcut = Event.current?.type == EventType.KeyDown;
			var path = settings.SaveFolderPath;
			if (!invokedByShortcut || !Directory.Exists(path))
				path = EditorUtility.SaveFolderPanel("glTF Export Path", settings.SaveFolderPath, "");

			if (!string.IsNullOrEmpty(path))
			{
				var ext = binary ? ".glb" : ".gltf";
				var resultFile = GLTFSceneExporter.GetFileName(path, sceneName, ext);
				settings.SaveFolderPath = path;
				if(binary)
					exporter.SaveGLB(path, sceneName);
				else
					exporter.SaveGLTFandBin(path, sceneName);

				Debug.Log("Exported to " + resultFile);
				EditorUtility.RevealInFinder(resultFile);
			}
		}
	}

	internal static class GLTFCreateMenu
	{
		[MenuItem("Assets/Create/UnityGLTF/Material", false)]
		private static void CreateNewAsset()
		{
			ProjectWindowUtil.CreateAssetWithContent("New glTF Material.gltf", @"{
	""asset"": {
		""generator"": ""UnityGLTF"",
		""version"": ""2.0""
	},
	""materials"": [
		{
			""name"": ""Material"",
			""pbrMetallicRoughness"": {
				""metallicFactor"": 0.0
			}
		}
	]
}");
		}
	}
}
