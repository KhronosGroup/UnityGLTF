using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityGLTF
{
	public class GLTFExportMenu : EditorWindow
	{
		private const string MenuPrefix = "Assets/UnityGLTF/";

	    public static string RetrieveTexturePath(UnityEngine.Texture texture)
	    {
	        return AssetDatabase.GetAssetPath(texture);
	    }

	    [MenuItem(MenuPrefix + "Settings", priority = 10000)]
	    static void Init()
	    {
	        GLTFExportMenu window = (GLTFExportMenu)EditorWindow.GetWindow(typeof(GLTFExportMenu), false, "GLTF Settings");
	        window.Show();
	    }

	    void OnGUI()
	    {
		    EditorGUILayout.HelpBox("This Window is deprecated and will be removed in a future release. Please use ProjectSettings/UnityGLTF instead.", MessageType.Warning);
		    if (GUILayout.Button("Open Project Settings/UnityGLTF"))
		    {
			    SettingsService.OpenProjectSettings("Project/UnityGLTF");
		    }
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

			var path = EditorUtility.SaveFolderPanel("glTF Export Path", GLTFSceneExporter.SaveFolderPath, "");
			if (!string.IsNullOrEmpty(path))
			{
				GLTFSceneExporter.SaveFolderPath = path;
				exporter.SaveGLTFandBin (path, name);
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

			var path = EditorUtility.SaveFolderPanel("glTF Export Path", GLTFSceneExporter.SaveFolderPath, "");
			if (!string.IsNullOrEmpty(path))
			{
				GLTFSceneExporter.SaveFolderPath = path;
				exporter.SaveGLB(path, name);
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
			var path = EditorUtility.SaveFolderPanel("glTF Export Path", "", "");
			if (path != "")
			{
				GLTFSceneExporter.SaveFolderPath = path;
				exporter.SaveGLTFandBin (path, scene.name);
			}
		}
	}
}
