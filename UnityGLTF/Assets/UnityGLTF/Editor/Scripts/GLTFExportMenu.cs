using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
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
	        if(AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(Texture2D))
	        {
		        var ext = System.IO.Path.GetExtension(path);
		        path = path.Replace(ext, "-" + texture.name + ext);
	        }
	        return path;
	    }

	    static bool TryGetExportNameAndRootTransformsFromSelection(out string name, out Transform[] rootTransforms)
	    {
		    if (Selection.transforms.Length > 1)
		    {
			    name = SceneManager.GetActiveScene().name;
			    rootTransforms = Selection.transforms;
			    return true;
		    }
		    if (Selection.transforms.Length == 1)
		    {
			    name = Selection.activeGameObject.name;
			    rootTransforms = Selection.transforms;
			    return true;
		    }
		    if (Selection.objects.Any() && Selection.objects.All(x => x is GameObject))
		    {
			    name = Selection.objects.First().name;
			    rootTransforms = Selection.objects.Select(x => (x as GameObject).transform).ToArray();
			    return true;
		    }

		    name = null;
		    rootTransforms = null;
		    return false;
	    }

	    [MenuItem(MenuPrefix + "Export selected as glTF", true)]
	    static bool ExportSelectedValidate()
	    {
		    return TryGetExportNameAndRootTransformsFromSelection(out _, out _);
	    }

	    [MenuItem(MenuPrefix + "Export selected as glTF")]
		static void ExportSelected()
		{
			if (!TryGetExportNameAndRootTransformsFromSelection(out var name, out var rootTransforms))
			{
				Debug.LogError("Can't export: selection is empty");
				return;
			}

			var exportOptions = new ExportOptions { TexturePathRetriever = RetrieveTexturePath };
			var exporter = new GLTFSceneExporter(rootTransforms, exportOptions);

			var invokedByShortcut = Event.current?.type == EventType.KeyDown;
			var path = GLTFSceneExporter.SaveFolderPath;
			if (!invokedByShortcut || !Directory.Exists(path))
				path = EditorUtility.SaveFolderPanel("glTF Export Path", GLTFSceneExporter.SaveFolderPath, "");

			if (!string.IsNullOrEmpty(path))
			{
				GLTFSceneExporter.SaveFolderPath = path;
				exporter.SaveGLTFandBin (path, name);

				var resultPath = $"{path}/{name}.gltf";
				Debug.Log("Exported to " + resultPath);
				EditorUtility.RevealInFinder(resultPath);
			}

		}

		[MenuItem(MenuPrefix + "Export selected as GLB", true)]
		static bool ExportGLBSelectedValidate()
		{
			return TryGetExportNameAndRootTransformsFromSelection(out _, out _);
		}

		[MenuItem(MenuPrefix + "Export selected as GLB")]
		static void ExportGLBSelected()
		{
			if (!TryGetExportNameAndRootTransformsFromSelection(out var name, out var rootTransforms))
			{
				Debug.LogError("Can't export: selection is empty");
				return;
			}

			var exportOptions = new ExportOptions { TexturePathRetriever = RetrieveTexturePath };
			var exporter = new GLTFSceneExporter(rootTransforms, exportOptions);

			var invokedByShortcut = Event.current?.type == EventType.KeyDown;
			var path = GLTFSceneExporter.SaveFolderPath;
			if (!invokedByShortcut || !Directory.Exists(path))
				path = EditorUtility.SaveFolderPanel("glTF Export Path", GLTFSceneExporter.SaveFolderPath, "");

			if (!string.IsNullOrEmpty(path))
			{
				GLTFSceneExporter.SaveFolderPath = path;
				exporter.SaveGLB(path, name);

				var resultPath = $"{path}/{name}.glb";
				Debug.Log("Exported to " + resultPath);
				EditorUtility.RevealInFinder(resultPath);
			}
		}

		[MenuItem(MenuPrefix + "Export active scene as glTF")]
		static void ExportScene()
		{
			var scene = SceneManager.GetActiveScene();
			var gameObjects = scene.GetRootGameObjects();
			var transforms = Array.ConvertAll(gameObjects, gameObject => gameObject.transform);

			var exportOptions = new ExportOptions { TexturePathRetriever = RetrieveTexturePath };
			var exporter = new GLTFSceneExporter(transforms, exportOptions);

			var invokedByShortcut = Event.current?.type == EventType.KeyDown;
			var path = GLTFSceneExporter.SaveFolderPath;
			if (!invokedByShortcut || !Directory.Exists(path))
				path = EditorUtility.SaveFolderPanel("glTF Export Path", GLTFSceneExporter.SaveFolderPath, "");

			if (!string.IsNullOrEmpty(path))
			{
				GLTFSceneExporter.SaveFolderPath = path;
				exporter.SaveGLTFandBin (path, scene.name);

				var resultPath = $"{path}/{scene.name}.gltf";
				Debug.Log("Exported to " + resultPath);
				EditorUtility.RevealInFinder(resultPath);
			}
		}
	}
}
