using System;
using UnityEditor;
using UnityGLTF;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;

public class GLTFExportMenu : EditorWindow
{
	public static string SettingsPath = "./GLTFSettings.json";
	public static string OutputPath = "";

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

	private void OnEnable()
	{
		if (!File.Exists(SettingsPath))
		{
			return;
		}

		var settingsStream = File.OpenRead(SettingsPath);
		TextReader textReader = new StreamReader(settingsStream);
		var jsonReader = new JsonTextReader(textReader);

		if (jsonReader.Read() && jsonReader.TokenType != JsonToken.StartObject)
		{
			throw new Exception("GLTF settings must be an object");
		}

		while (jsonReader.Read() && jsonReader.TokenType == JsonToken.PropertyName)
		{
			var curProp = jsonReader.Value.ToString();
			switch (curProp)
			{
				case "OutputPath":
					OutputPath = jsonReader.ReadAsString();
					break;
				case "ExportNames":
					GLTFSceneExporter.ExportNames = jsonReader.ReadAsBoolean().Value;
					break;
				case "ExportFullPath":
					GLTFSceneExporter.ExportFullPath = jsonReader.ReadAsBoolean().Value;
					break;
				case "RequireExtensions":
					GLTFSceneExporter.RequireExtensions = jsonReader.ReadAsBoolean().Value;
					break;
			}
		}

		jsonReader.Close();
		textReader.Close();
	}

	private void OnDisable()
	{
		FileStream settingsStream = null;
		if (!File.Exists(SettingsPath))
		{
			settingsStream = File.Create(SettingsPath);
		}

		if (settingsStream == null)
		{
			settingsStream = File.OpenWrite(SettingsPath);
		}
		
		TextWriter textWriter = new StreamWriter(settingsStream);
		JsonWriter jsonWriter = new JsonTextWriter(textWriter);

		jsonWriter.WriteStartObject();
		jsonWriter.WritePropertyName("OutputPath");
		jsonWriter.WriteValue(OutputPath);
		jsonWriter.WritePropertyName("ExportNames");	
		jsonWriter.WriteValue(GLTFSceneExporter.ExportNames);
		jsonWriter.WritePropertyName("ExportFullPath");
		jsonWriter.WriteValue(GLTFSceneExporter.ExportFullPath);
		jsonWriter.WritePropertyName("RequireExtensions");
		jsonWriter.WriteValue(GLTFSceneExporter.RequireExtensions);
		jsonWriter.WriteEndObject();

		jsonWriter.Flush();
		textWriter.Flush();
		jsonWriter.Close();
		textWriter.Close();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Exporter", EditorStyles.boldLabel);
        GLTFSceneExporter.ExportFullPath = EditorGUILayout.Toggle("Export using original path", GLTFSceneExporter.ExportFullPath);
        GLTFSceneExporter.ExportNames = EditorGUILayout.Toggle("Export names of nodes", GLTFSceneExporter.ExportNames);
        GLTFSceneExporter.RequireExtensions= EditorGUILayout.Toggle("Require extensions", GLTFSceneExporter.RequireExtensions);
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

		var path = EditorUtility.SaveFolderPanel("glTF Export Path", OutputPath, "");
		if (!string.IsNullOrEmpty(path))
		{
			OutputPath = path;
			exporter.SaveGLTFandBin(OutputPath, name);
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

		var path = EditorUtility.SaveFolderPanel("glTF Export Path", OutputPath, "");
		if (!string.IsNullOrEmpty(path))
		{
			OutputPath = path;
			exporter.SaveGLB(OutputPath, name);
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
		var path = EditorUtility.SaveFolderPanel("glTF Export Path", OutputPath, "");
		if (!string.IsNullOrEmpty(path))
		{
			OutputPath = path;
			exporter.SaveGLTFandBin(OutputPath, scene.name);
		}
	}
}
