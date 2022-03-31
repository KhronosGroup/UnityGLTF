#if UNITY_EDITOR && UNITY_IMGUI
#define SHOW_SETTINGS_EDITOR
#endif

using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF
{
#if SHOW_SETTINGS_EDITOR
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
            prop.NextVisible(true);
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
#endif

    public class GLTFSettings : ScriptableObject
    {
	    private const string k_PreferencesPrefix = "UnityGLTF_Preferences_";
	    private const string k_SettingsFileName = "UnityGLTFSettings.asset";
	    public const string k_RuntimeAndEditorSettingsPath = "Assets/Resources/" + k_SettingsFileName;

	    [System.Flags]
	    public enum BlendShapeExportPropertyFlags
	    {
		    None = 0,
		    PositionOnly = 1,
		    Normal = 2,
		    Tangent = 4,
		    All = ~0
	    }

	    [Header("Export Settings")]
		[SerializeField]
		private bool exportNames = true;
		[SerializeField]
		private bool exportFullPath = true;
		[SerializeField]
		private bool requireExtensions = false;
		[Header("Export Visibility")]
		[SerializeField]
		[Tooltip("Uses Camera.main layer settings to filter which objects are exported")]
		private bool useMainCameraVisibility = true;
		[SerializeField]
		[Tooltip("glTF does not support visibility state. If this setting is true, disabled GameObjects will still be exported and be visible in the glTF file.")]
		private bool exportDisabledGameObjects = false;
		[Header("Export Textures")]
		[SerializeField]
		[Tooltip("(Experimental) Exports PNG/JPEG directly from disk instead of re-encoding from Unity's import result. No channel repacking will happen for these textures. Textures in other formats (PSD, TGA etc) not supported by glTF and in-memory textures (e.g. RenderTextures) are always re-encoded.")]
		private bool tryExportTexturesFromDisk = false;
		[SerializeField] [Tooltip("Determines texture export type (PNG or JPEG) based on alpha channel. When false, always exports lossless PNG files.")]
		private bool useTextureFileTypeHeuristic = true;
		[SerializeField] [Tooltip("Quality setting for exported JPEG files.")]
		private int defaultJpegQuality = 90;
		[Header("Export Animation")]
		[SerializeField]
		private bool exportAnimations = true;
		[SerializeField]
		[Tooltip("Some viewers can't distinguish between animation clips that have the same name. This option ensures all exported animation names are unique.")]
		private bool uniqueAnimationNames = false;
		[SerializeField]
		private bool bakeSkinnedMeshes = false;
		[Header("Export Mesh Data")]
		[SerializeField]
		private BlendShapeExportPropertyFlags blendShapeExportProperties = BlendShapeExportPropertyFlags.All;
		[SerializeField]
		[Tooltip("(Experimental) Use Sparse Accessors for blend shape export. Not supported on some viewers.")]
		private bool blendShapeExportSparseAccessors = false;
		[SerializeField]
	    [Tooltip("Vertex Colors aren't supported in some viewers (e.g. Google's SceneViewer).")]
		private bool exportVertexColors = true;

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

		public bool UseTextureFileTypeHeuristic
		{ get => useTextureFileTypeHeuristic;
			set {
				if(useTextureFileTypeHeuristic != value) {
					useTextureFileTypeHeuristic = value;
#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
#endif
				}
			}
		}

		public bool ExportVertexColors
		{ get => exportVertexColors;
			set {
				if(exportVertexColors != value) {
					exportVertexColors = value;
#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
#endif
				}
			}
		}

		public int DefaultJpegQuality
		{ get => defaultJpegQuality;
			set {
				if(defaultJpegQuality != value) {
					defaultJpegQuality = value;
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

		public bool UniqueAnimationNames
		{ get => uniqueAnimationNames;
			set {
				if(uniqueAnimationNames != value) {
					uniqueAnimationNames = value;
#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
#endif
				}
			}
		}

		public bool BlendShapeExportSparseAccessors
		{ get => blendShapeExportSparseAccessors;
			set {
				if (blendShapeExportSparseAccessors != value) {
					blendShapeExportSparseAccessors = value;
#if UNITY_EDITOR
					EditorUtility.SetDirty(this);
#endif
				}
			}
		}

		public BlendShapeExportPropertyFlags BlendShapeExportProperties
		{ get => blendShapeExportProperties;
			set {
				if(blendShapeExportProperties != value) {
					blendShapeExportProperties = value;
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

#if UNITY_EDITOR
		private const string SaveFolderPathPref = k_PreferencesPrefix + "SaveFolderPath";
		public string SaveFolderPath
		{
			get => EditorPrefs.GetString(SaveFolderPathPref, null);
			set => EditorPrefs.SetString(SaveFolderPathPref, value);
		}
#endif

	    internal static GLTFSettings cachedSettings;

	    public static GLTFSettings GetOrCreateSettings()
	    {
		    if (!TryGetSettings(out var settings))
		    {
#if UNITY_EDITOR
			    settings = ScriptableObject.CreateInstance<GLTFSettings>();
			    if (!Directory.Exists(k_RuntimeAndEditorSettingsPath)) Directory.CreateDirectory(k_RuntimeAndEditorSettingsPath);
			    AssetDatabase.CreateAsset(settings, k_RuntimeAndEditorSettingsPath);
			    AssetDatabase.SaveAssets();
#else
				settings = ScriptableObject.CreateInstance<GLTFSettings>();
#endif
		    }
		    cachedSettings = settings;
		    return settings;
	    }

	    public static bool TryGetSettings(out GLTFSettings settings)
	    {
		    settings = cachedSettings;
		    if (settings)
			    return true;

		    settings = Resources.Load<GLTFSettings>(Path.GetFileNameWithoutExtension(k_SettingsFileName));

#if UNITY_EDITOR
		    if (!settings)
		    {
			    settings = AssetDatabase.LoadAssetAtPath<GLTFSettings>(k_RuntimeAndEditorSettingsPath);
		    }
		    if (!settings)
		    {
			    var allSettings = AssetDatabase.FindAssets("t:GLTFSettings");
			    if (allSettings.Length > 0)
			    {
				    settings = AssetDatabase.LoadAssetAtPath<GLTFSettings>(AssetDatabase.GUIDToAssetPath(allSettings[0]));
			    }
		    }
		    cachedSettings = settings;
		    return settings;
#else
			return settings;
#endif
	    }
    }
}
