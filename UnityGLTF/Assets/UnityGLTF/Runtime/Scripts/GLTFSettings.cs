using UnityEditor;
using UnityEngine;

namespace UnityGLTF
{
	// Create a new type of Settings Asset.
	public class GLTFSettings : ScriptableObject
	{
		public const string k_MyCustomSettingsPath = "Assets/GLTFSettings_dontmove.asset";

		public bool ExportNames = true;
		public bool ExportFullPath = true;
		public bool RequireExtensions = false;
		public bool TryExportTexturesFromDisk = true;
		public bool ExportAnimations = true;
		public bool BakeSkinnedMeshes = false;
		public string SaveFolderPath = "";

		internal static GLTFSettings GetOrCreateSettings()
		{
			var settings = AssetDatabase.LoadAssetAtPath<GLTFSettings>(k_MyCustomSettingsPath);
			if (settings == null)
			{
				settings = ScriptableObject.CreateInstance<GLTFSettings>();
				AssetDatabase.CreateAsset(settings, k_MyCustomSettingsPath);
				AssetDatabase.SaveAssets();
			}
			return settings;
		}

		internal static SerializedObject GetSerializedSettings()
		{
			return new SerializedObject(GetOrCreateSettings());
		}
	}
}
