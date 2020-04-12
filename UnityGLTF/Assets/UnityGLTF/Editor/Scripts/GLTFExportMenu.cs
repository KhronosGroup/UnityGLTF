using System;
using UnityEditor;
using UnityGLTF;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GLTFExportMenu : EditorWindow
{
	public static string RetrieveTexturePath(Texture texture)
    {
        return AssetDatabase.GetAssetPath(texture);
    }

    [MenuItem("GLTF/Settings")]
    static void Init()
    {
        GLTFExportMenu window = (GLTFExportMenu)EditorWindow.GetWindow(typeof(GLTFExportMenu), false, "GLTF Settings");
        window.Show();
    }

	private void OnEnable()
	{
		GLTFSceneExporter.InitSettings();
	}

	private void OnDisable()
	{
		AssetDatabase.SaveAssets();
    }

    void OnGUI()
    {
		GLTFSettings.Update();

		EditorGUILayout.LabelField("Exporter", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(GLTFSettings.ExportNamesSp, GLTFSettings.ExportNamesGc, true);
		EditorGUILayout.PropertyField(GLTFSettings.ExportFullPathSp, GLTFSettings.ExportFullPathGc, true);
		EditorGUILayout.PropertyField(GLTFSettings.RequireExtensionsSp, GLTFSettings.RequireExtensionsGc, true);
		EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Importer", EditorStyles.boldLabel);
        EditorGUILayout.Separator();
        EditorGUILayout.HelpBox("UnityGLTF version 0.1", MessageType.Info);
		EditorGUILayout.HelpBox("Supported extensions: KHR_material_pbrSpecularGlossiness, ExtTextureTransform", MessageType.Info);

		GLTFSettings.ApplyModifiedProperties();
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

		var path = EditorUtility.SaveFolderPanel("glTF Export Path", GLTFSettings.OutputPath, "");
		if (!string.IsNullOrEmpty(path))
		{
			GLTFSettings.OutputPath = path;
			exporter.SaveGLTFandBin(GLTFSettings.OutputPath, name);
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

		var path = EditorUtility.SaveFolderPanel("glTF Export Path", GLTFSettings.OutputPath, "");
		if (!string.IsNullOrEmpty(path))
		{
			GLTFSettings.OutputPath = path;
			exporter.SaveGLB(GLTFSettings.OutputPath, name);
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
		var path = EditorUtility.SaveFolderPanel("glTF Export Path", GLTFSettings.OutputPath, "");
		if (!string.IsNullOrEmpty(path))
		{
			GLTFSettings.OutputPath = path;
			exporter.SaveGLTFandBin(GLTFSettings.OutputPath, scene.name);
		}
	}
}
