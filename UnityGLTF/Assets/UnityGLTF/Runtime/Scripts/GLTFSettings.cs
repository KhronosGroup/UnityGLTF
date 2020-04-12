using System.IO;
using UnityEditor;
using UnityEngine;


public class GLTFSettings : ScriptableObject
{
	public static string SettingsPath = "Assets/Plugins/Editor/GLTFSettings.asset";

	private static GLTFSettings Singleton = null;

	private static SerializedObject sObj;

	public static SerializedProperty OutputPathSp;
	public static GUIContent ExportNamesGc = new GUIContent("Export names of nodes");
	public static SerializedProperty ExportNamesSp;
	public static GUIContent ExportFullPathGc = new GUIContent("Export using original path");
	public static SerializedProperty ExportFullPathSp;
	public static GUIContent RequireExtensionsGc = new GUIContent("Require extensions");
	public static SerializedProperty RequireExtensionsSp;
	public static GUIContent ExportPhysicsCollidersGc = new GUIContent("Export physics colliders");
	public static SerializedProperty ExportPhysicsCollidersSp;

	public string outputPath = "";
	public bool exportNames = true;
	public bool exportFullPath = true;
	public bool requireExtensions = false;
	public bool exportPhysicsColliders = true;


	public static GLTFSettings CreateInstance()
	{
		if (null == Singleton)
		{
			if (!File.Exists(GLTFSettings.SettingsPath))
			{
				var fileName = Path.GetFileName(SettingsPath);
				var path = SettingsPath.Substring(0, SettingsPath.Length - fileName.Length);
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
				Singleton = ScriptableObject.CreateInstance<GLTFSettings>();
				AssetDatabase.CreateAsset(Singleton, GLTFSettings.SettingsPath);
			}

			Singleton = AssetDatabase.LoadAssetAtPath<GLTFSettings>(GLTFSettings.SettingsPath);

			if (sObj == null)
			{
				sObj = new SerializedObject(Singleton);
			}

			Init();
		}

		return Singleton;
	}

	private GLTFSettings() { }

	public static string OutputPath
	{
		get
		{
			return OutputPathSp.stringValue;
		}

		set
		{
			OutputPathSp.stringValue = value;
			ApplyModifiedProperties();
		}
	}

	private static void Init()
	{
		OutputPathSp = FindProperty("outputPath");
		ExportNamesSp = FindProperty("exportNames");
		ExportFullPathSp = FindProperty("exportFullPath");
		RequireExtensionsSp = FindProperty("requireExtensions");
		ExportPhysicsCollidersSp = FindProperty("exportPhysicsColliders");
	}

	public static void Update()
	{
		sObj.Update();
	}

	public static void ApplyModifiedProperties()
	{
		sObj.ApplyModifiedProperties();
		AssetDatabase.SaveAssets();
	}

	protected static SerializedProperty FindProperty(string propertyPath)
	{
		return sObj.FindProperty(propertyPath);
	}
}
