using System;
using UnityEditor;
using UnityGLTFSerialization;
using UnityEngine.SceneManagement;

public class GLTFExportMenu
{
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

		var exporter = new GLTFSceneExporter(Selection.transforms);
		var path = EditorUtility.OpenFolderPanel("glTF Export Path", "", "");
		exporter.SaveGLTFandBin(path, name);
	}

	[MenuItem("GLTF/Export Scene")]
	static void ExportScene()
	{
		var scene = SceneManager.GetActiveScene();
		var gameObjects = scene.GetRootGameObjects();
		var transforms = Array.ConvertAll(gameObjects, gameObject => gameObject.transform);

		var exporter = new GLTFSceneExporter(transforms);
		var path = EditorUtility.OpenFolderPanel("glTF Export Path", "", "");
		exporter.SaveGLTFandBin(path, scene.name);
	}
}