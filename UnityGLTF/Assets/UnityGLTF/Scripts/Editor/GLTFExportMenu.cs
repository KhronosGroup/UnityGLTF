using System;
using UnityEditor;
using UnityGLTF;
using UnityEngine.SceneManagement;

public class GLTFExportMenu
{
	public static string RetrieveTexturePath(UnityEngine.Texture texture)
	{
		return AssetDatabase.GetAssetPath (texture);
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

		var exporter = new GLTFSceneExporter(Selection.transforms, RetrieveTexturePath);

		var path = EditorUtility.OpenFolderPanel("glTF Export Path", "", "");
		if (!string.IsNullOrEmpty(path)) {
			exporter.SaveGLTFandBin (path, name);
		}
	}

	[MenuItem("GLTF/Export Scene")]
	static void ExportScene()
	{
		var scene = SceneManager.GetActiveScene();
		var gameObjects = scene.GetRootGameObjects();
		var transforms = Array.ConvertAll(gameObjects, gameObject => gameObject.transform);

		var exporter = new GLTFSceneExporter(transforms, RetrieveTexturePath);
		var path = EditorUtility.OpenFolderPanel("glTF Export Path", "", "");
		if (path != "") {
			exporter.SaveGLTFandBin (path, scene.name);
		}
	}
}