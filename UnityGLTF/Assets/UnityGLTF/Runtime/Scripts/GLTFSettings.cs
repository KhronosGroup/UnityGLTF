using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityGLTF
{
    internal class GltfSettingsProvider : SettingsProvider
    {
	    private GLTFSettings settings;
        private SerializedObject m_SerializedObject;
        private SerializedProperty showDefaultReferenceNameWarning, showNamingRecommendationHint;

        public override void OnGUI(string searchContext)
        {
	        EditorGUIUtility.labelWidth = 200;
	        if (m_SerializedObject == null)
	        {
		        if (!settings) settings = GLTFSettings.GetOrCreateSettings();
	            m_SerializedObject = new SerializedObject(settings);
            }

            SerializedProperty prop = m_SerializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
	            do
	            {
			        EditorGUILayout.PropertyField(prop, true);
	            }
	            while (prop.NextVisible(false));
            }

            if(m_SerializedObject.hasModifiedProperties) {
                m_SerializedObject.ApplyModifiedProperties();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateGltfSettingsProvider()
        {
	        GLTFSettings.GetOrCreateSettings();
            return new GltfSettingsProvider("Project/UnityGLTF", SettingsScope.Project);
        }

        public GltfSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }
    }

    public class GLTFSettings : ScriptableObject
    {
	    private const string k_PreferencesPrefix = "UnityGLTF_Preferences_";
	    private const string k_SettingsFileName = "UnityGLTFSettings.asset";
	    public const string k_MyCustomSettingsPath = "Assets/Resources/" + k_SettingsFileName;

	    [Header("Export")]
		[SerializeField]
		private bool exportNames = true;
		[SerializeField]
		private bool exportFullPath = true;
		[SerializeField]
		[Tooltip("Uses Camera.main layer settings to filter which objects are exported")]
		private bool useMainCameraVisibility = true;
		[SerializeField]
		private bool requireExtensions = false;
		[SerializeField]
		[Tooltip("Exports PNG/JPEG directly from disk instead of re-encoding from Unity's import result. Textures in other formats (PSD, TGA etc) not supported by glTF and in-memory textures (e.g. RenderTextures) are always re-encoded.")]
		private bool tryExportTexturesFromDisk = true;
		[SerializeField]
		[Tooltip("glTF does not support visibility state. If this setting is true, disabled GameObjects will still be exported and be visible in the glTF file.")]
		private bool exportDisabledGameObjects = false;
		[SerializeField]
		private bool exportAnimations = true;
		[SerializeField]
		private bool bakeSkinnedMeshes = false;

		public bool ExportNames { get => exportNames;
			set {
				if(exportNames != value) {
					exportNames = value;
					#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
					#endif
				}
			}
		}

		public bool ExportFullPath
		{ get => exportFullPath;
			set {
				if(exportFullPath != value) {
					exportFullPath = value;
					#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
					#endif
				}
			}
		}

		public bool UseMainCameraVisibility
		{ get => useMainCameraVisibility;
			set {
				if(useMainCameraVisibility != value) {
					useMainCameraVisibility = value;
#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
#endif
				}
			}
		}

		public bool RequireExtensions
		{ get => requireExtensions;
			set {
				if(requireExtensions != value) {
					requireExtensions = value;
					#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
					#endif
				}
			}
		}

		public bool TryExportTexturesFromDisk
		{ get => tryExportTexturesFromDisk;
			set {
				if(tryExportTexturesFromDisk != value) {
					tryExportTexturesFromDisk = value;
					#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
					#endif
				}
			}
		}

		public bool ExportDisabledGameObjects
		{ get => exportDisabledGameObjects;
			set {
				if(exportDisabledGameObjects != value) {
					exportDisabledGameObjects = value;
#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
#endif
				}
			}
		}

		public bool ExportAnimations
		{ get => exportAnimations;
			set {
				if(exportAnimations != value) {
					exportAnimations = value;
					#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
					#endif
				}
			}
		}

		public bool BakeSkinnedMeshes
		{ get => bakeSkinnedMeshes;
			set {
				if(bakeSkinnedMeshes != value) {
					bakeSkinnedMeshes = value;
					#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
					#endif
				}
			}
		}

		private const string SaveFolderPathPref = k_PreferencesPrefix + "SaveFolderPath";
		public string SaveFolderPath
		{
			get => EditorPrefs.GetString(SaveFolderPathPref, null);
			set => EditorPrefs.SetString(SaveFolderPathPref, value);
		}

		internal static GLTFSettings GetOrCreateSettings()
		{
			var settings = Resources.Load<GLTFSettings>(k_SettingsFileName);
#if UNITY_EDITOR
			if(!settings)
			{
				settings = AssetDatabase.LoadAssetAtPath<GLTFSettings>(k_MyCustomSettingsPath);
			}
			if (!settings)
			{
				var allSettings = AssetDatabase.FindAssets("t:GLTFSettings");
				if (allSettings.Length > 0)
				{
					settings = AssetDatabase.LoadAssetAtPath<GLTFSettings>(AssetDatabase.GUIDToAssetPath(allSettings[0]));
				}
			}
			if (!settings)
			{
				settings = ScriptableObject.CreateInstance<GLTFSettings>();
				AssetDatabase.CreateAsset(settings, k_MyCustomSettingsPath);
				AssetDatabase.SaveAssets();
			}
			return settings;
#else
			if(!settings)
			{
				settings = ScriptableObject.CreateInstance<GLTFSettings>();
			}
			return settings;
#endif
		}
	}
}
