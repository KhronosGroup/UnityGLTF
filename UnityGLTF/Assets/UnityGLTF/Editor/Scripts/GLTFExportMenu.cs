using System;
using UnityEditor;
using UnityGLTF;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [MenuItem("GLTF/Export Selected")]
	static void ExportSelected()
	{
		string name;
		if (Selection.transforms.Length > 1)
			name = SceneManager.GetActiveScene().name;
		else if (Selection.transforms.Length == 1)
			name = Selection.activeGameObject.name;
		else
			throw new Exception("No objects selected, cannot export.");

		var exportOptions = new ExportOptions { TexturePathRetriever = RetrieveTexturePath };
		var exporter = new GLTFSceneExporter(Selection.transforms, exportOptions);

		var path = EditorUtility.SaveFolderPanel("glTF Export Path", GLTFSceneExporter.SaveFolderPath, "");
		if (!string.IsNullOrEmpty(path))
		{
			GLTFSceneExporter.SaveFolderPath = path;
			exporter.SaveGLTFandBin (path, name);
		}
	}
	
	[MenuItem("GLTF/ExportGLB Selected")]
	static void ExportGLBSelected()
	{
		string name;
		if (Selection.transforms.Length > 1)
			name = SceneManager.GetActiveScene().name;
		else if (Selection.transforms.Length == 1)
			name = Selection.activeGameObject.name;
		else
			throw new Exception("No objects selected, cannot export.");

		var exportOptions = new ExportOptions { TexturePathRetriever = RetrieveTexturePath };
		var exporter = new GLTFSceneExporter(Selection.transforms, exportOptions);

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
