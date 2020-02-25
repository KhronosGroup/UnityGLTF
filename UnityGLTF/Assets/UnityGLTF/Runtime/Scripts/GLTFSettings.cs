using UnityEditor;
using UnityEngine;

namespace UnityGLTF
{
	// Create a new type of Settings Asset.
	public class GLTFSettings : ScriptableObject
	{
		public const string k_MyCustomSettingsPath = "Assets/GLTFSettings_dontmove.asset";

		[SerializeField]
		public bool exportNames = true;
		[SerializeField]
		public bool exportFullPath = true;
		[SerializeField]
		public bool requireExtensions = false;
		[SerializeField]
		public bool tryExportTexturesFromDisk = true;
		[SerializeField]
		public bool exportAnimations = true;
		[SerializeField]
		public bool bakeSkinnedMeshes = false;
		[SerializeField]
		public string saveFolderPath = "";

		public bool ExportNames { get => exportNames;
			set {
				if(exportNames != value) {
					exportNames = value;
					EditorUtility.SetDirty(this);
				}
			}
		}
		
		public bool ExportFullPath
		{ get => exportFullPath;
			set {
				if(exportFullPath != value) {
					exportFullPath = value;
					EditorUtility.SetDirty(this);
				}
			}
		}
		public bool RequireExtensions
		{ get => requireExtensions;
			set {
				if(requireExtensions != value) {
					requireExtensions = value;
					EditorUtility.SetDirty(this);
				}
			}
		}
		
		public bool TryExportTexturesFromDisk
		{ get => tryExportTexturesFromDisk;
			set {
				if(tryExportTexturesFromDisk != value) {
					tryExportTexturesFromDisk = value;
					EditorUtility.SetDirty(this);
				}
			}
		}
		
		public bool ExportAnimations
		{ get => exportAnimations;
			set {
				if(exportAnimations != value) {
					exportAnimations = value;
					EditorUtility.SetDirty(this);
				}
			}
		}
		
		public bool BakeSkinnedMeshes
		{ get => bakeSkinnedMeshes;
			set {
				if(bakeSkinnedMeshes != value) {
					bakeSkinnedMeshes = value;
					EditorUtility.SetDirty(this);
				}
			}
		}
		
		public string SaveFolderPath
		{ get => saveFolderPath;
			set {
				if(saveFolderPath != value) {
					saveFolderPath = value;
					EditorUtility.SetDirty(this);
				}
			}
		}

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
