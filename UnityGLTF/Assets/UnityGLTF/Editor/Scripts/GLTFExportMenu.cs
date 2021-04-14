using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityGLTF
{
	public class GLTFExportMenu : EditorWindow
	{
	    public static string RetrieveTexturePath(UnityEngine.Texture texture)
	    {
	        return AssetDatabase.GetAssetPath(texture);
	    }

	    [MenuItem("GLTF/Settings")]
	    static void Init()
	    {
	        GLTFExportMenu window = (GLTFExportMenu)EditorWindow.GetWindow(typeof(GLTFExportMenu), false, "GLTF Settings");
	        window.Show();
	    }

	    void OnGUI()
	    {
	        EditorGUILayout.LabelField("Exporter", EditorStyles.boldLabel);
	        GLTFSceneExporter.ExportFullPath = EditorGUILayout.Toggle("Export using original path", GLTFSceneExporter.ExportFullPath);
	        GLTFSceneExporter.ExportNames = EditorGUILayout.Toggle("Export names of nodes", GLTFSceneExporter.ExportNames);
	        GLTFSceneExporter.RequireExtensions= EditorGUILayout.Toggle("Require extensions", GLTFSceneExporter.RequireExtensions);
	        GLTFSceneExporter.TryExportTexturesFromDisk = EditorGUILayout.Toggle("Try to export textures from disk", GLTFSceneExporter.TryExportTexturesFromDisk);
	        EditorGUILayout.Separator();
	        EditorGUILayout.LabelField("Importer", EditorStyles.boldLabel);
	        EditorGUILayout.Separator();
	        EditorGUILayout.HelpBox("UnityGLTF version 0.1", MessageType.Info);
	        EditorGUILayout.HelpBox("Supported extensions: KHR_material_pbrSpecularGlossiness, ExtTextureTransform", MessageType.Info);
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

	    [MenuItem("GLTF/Export Selected", true)]
	    static bool ExportSelectedValidate()
	    {
		    return TryGetExportNameAndRootTransformsFromSelection(out _, out _);
	    }

	    [MenuItem("GLTF/Export Selected")]
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

		[MenuItem("GLTF/ExportGLB Selected", true)]
		static bool ExportGLBSelectedValidate()
		{
			return TryGetExportNameAndRootTransformsFromSelection(out _, out _);
		}

		[MenuItem("GLTF/ExportGLB Selected")]
		static void ExportGLBSelected()
		{
			if (!TryGetExportNameAndRootTransformsFromSelection(out var name, out var rootTransforms))
			{
				Debug.LogError("Can't export: selection is empty");
				return;
			}

			var exportOptions = new ExportOptions { TexturePathRetriever = RetrieveTexturePath };
			var exporter = new GLTFSceneExporter(rootTransforms, exportOptions);

			var path = EditorUtility.SaveFolderPanel("glTF Export Path", "", "");
			if (!string.IsNullOrEmpty(path))
			{
				GLTFSceneExporter.SaveFolderPath = path;
				exporter.SaveGLB(path, name);
			}
		}

		[MenuItem("GLTF/Export Scene")]
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
