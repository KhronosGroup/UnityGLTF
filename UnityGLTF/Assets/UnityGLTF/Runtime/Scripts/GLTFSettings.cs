#if UNITY_EDITOR && UNITY_IMGUI
#define SHOW_SETTINGS_EDITOR
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.UIElements;
using UnityGLTF.Cache;
using UnityGLTF.Plugins;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF
{
#if SHOW_SETTINGS_EDITOR
    internal class GltfSettingsProvider : SettingsProvider
    {
	    internal static Action<GLTFSettings> OnAfterGUI;
        private SerializedProperty showDefaultReferenceNameWarning, showNamingRecommendationHint;

        public override void OnGUI(string searchContext)
        {
	        DrawGLTFSettingsGUI();
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
	        base.OnActivate(searchContext, rootElement);
	        CalculateCacheStats();
        }


        [SettingsProvider]
        public static SettingsProvider CreateGltfSettingsProvider()
        {
	        GLTFSettings.GetOrCreateSettings();
            return new GltfSettingsProvider("Project/UnityGLTF", SettingsScope.Project);;
        }

        public GltfSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }


        private static long exportCacheByteLength = 0;
        private static void CalculateCacheStats()
        {
	        var files = new List<FileInfo>();
	        exportCacheByteLength = ExportCache.CalculateCacheSize(files);
        }


        private static GLTFSettings settings;
        private static SerializedObject m_SerializedObject;

        internal static void DrawGLTFSettingsGUI()
        {
	        EditorGUIUtility.labelWidth = 200;
	        if (settings == null)
	        {
		        if (!settings) settings = GLTFSettings.GetOrCreateSettings();
		        m_SerializedObject = new SerializedObject(settings);
	        }

	        var prop = m_SerializedObject.GetIterator();
	        prop.NextVisible(true);
	        if (prop.NextVisible(true))
	        {
		        do
		        {
			        EditorGUILayout.PropertyField(prop, true);
			        switch (prop.name)
			        {
				        case nameof(GLTFSettings.UseCaching):
					        EditorGUILayout.BeginHorizontal();
					        if (GUILayout.Button($"Clear Cache ({(exportCacheByteLength / (1024f * 1024f)):F2} MB)")) {
						        ExportCache.Clear();
						        CalculateCacheStats();
					        }
					        if (GUILayout.Button("Open Cache Directory ↗"))
						        ExportCache.OpenCacheDirectory();
					        EditorGUILayout.EndHorizontal();
					        break;
			        }
		        }
		        while (prop.NextVisible(false));
	        }

	        if(m_SerializedObject.hasModifiedProperties) {
		        m_SerializedObject.ApplyModifiedProperties();
	        }

	        GUILayout.Space(10);
	        EditorGUILayout.LabelField("Import Plugins", EditorStyles.boldLabel);
	        OnPluginsGUI();
	        OnAfterGUI?.Invoke(settings);
        }

        internal static void OnPluginsGUI()
        {
	        foreach (var plugin in settings.ImportPlugins)
	        {
		        if (!plugin) continue;
		        var displayName = plugin.DisplayName ?? plugin.name;
		        if (string.IsNullOrEmpty(displayName))
			        displayName = ObjectNames.NicifyVariableName(plugin.GetType().Name);
		        using (new GUILayout.HorizontalScope())
		        {
			        var label = new GUIContent(displayName);
			        plugin.Expanded = EditorGUILayout.BeginFoldoutHeaderGroup(plugin.Expanded, label);
			        plugin.Enabled = GUILayout.Toggle(plugin.Enabled, "", GUILayout.Width(20));
		        }
		        if (plugin.Expanded)
		        {
			        EditorGUI.indentLevel += 1;
			        plugin.OnGUI();
			        EditorGUI.indentLevel -= 1;
		        }
		        EditorGUILayout.EndFoldoutHeaderGroup();
	        }
        }

    }

    [CustomEditor(typeof(GLTFSettings))]
    internal class GLTFSettingsEditor : Editor
    {
	    public override void OnInspectorGUI()
	    {
		    GltfSettingsProvider.DrawGLTFSettingsGUI();
	    }
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

	    [SerializeField, HideInInspector]
	    public List<GltfImportPlugin> ImportPlugins;

	    [Header("Export Settings")]
		[SerializeField]
		private bool exportNames = true;
		[SerializeField]
		[Tooltip("If on, the entire texture path will be preserved. If off (default), textures are exported at root level.")]
		private bool exportFullPath = false;
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
		[SerializeField, Tooltip("When enabled the Animator State speed parameter is baked into the exported glTF animation")]
		private bool bakeAnimationSpeed = true;
		[Tooltip("(Experimental) Export animations using KHR_animation_pointer. Requires the viewer to also support this extension.")]
		[SerializeField]
		private bool useAnimationPointer = false;
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
		private bool blendShapeExportSparseAccessors = true;
		[SerializeField]
	    [Tooltip("If off, vertex colors are not exported. Vertex Colors aren't supported in some viewers (e.g. Google's SceneViewer).")]
		private bool exportVertexColors = true;

		[Header("Export Cache")]
		[Tooltip("When enabled textures will be cached to disc for faster export times.\n(The cache size is reduced to stay below 1024 MB when the Editor quits)")]
		public bool UseCaching = true;

		public bool ExportNames { get => exportNames; set  => exportNames = value; }
		public bool ExportFullPath { get => exportFullPath; set => exportFullPath = value; }
		public bool UseMainCameraVisibility { get => useMainCameraVisibility; set => useMainCameraVisibility = value; }
		public bool RequireExtensions { get => requireExtensions; set => requireExtensions = value; }
		public bool TryExportTexturesFromDisk { get => tryExportTexturesFromDisk; set => tryExportTexturesFromDisk = value; }
		public bool UseTextureFileTypeHeuristic { get => useTextureFileTypeHeuristic; set => useTextureFileTypeHeuristic = value; }
		public bool ExportVertexColors { get => exportVertexColors; set => exportVertexColors = value; }
		public int DefaultJpegQuality { get => defaultJpegQuality; set => defaultJpegQuality = value; }
		public bool ExportDisabledGameObjects { get => exportDisabledGameObjects; set => exportDisabledGameObjects = value; }
		public bool ExportAnimations { get => exportAnimations; set => exportAnimations = value; }
		public bool BakeAnimationSpeed { get => bakeAnimationSpeed; set => bakeAnimationSpeed = value; }
		public bool UseAnimationPointer { get => useAnimationPointer; set => useAnimationPointer = value; }
		public bool UniqueAnimationNames { get => uniqueAnimationNames; set => uniqueAnimationNames = value; }
		public bool BlendShapeExportSparseAccessors { get => blendShapeExportSparseAccessors; set => blendShapeExportSparseAccessors = value; }
		public BlendShapeExportPropertyFlags BlendShapeExportProperties { get => blendShapeExportProperties; set => blendShapeExportProperties = value; }
		public bool BakeSkinnedMeshes { get => bakeSkinnedMeshes; set => bakeSkinnedMeshes = value; }


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
			    settings.name = Path.GetFileNameWithoutExtension(k_RuntimeAndEditorSettingsPath);
			    var dir = Path.GetDirectoryName(k_RuntimeAndEditorSettingsPath);
			    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

			    // we can save it here, but we can't call AssetDatabase.CreateAsset as the importer will complain
			    UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { settings }, k_RuntimeAndEditorSettingsPath, true);

			    // so after import, we have to connect the cachedSettings again
			    EditorApplication.delayCall += () =>
			    {
				    // Debug.Log("Deferred settings connection");
				    AssetDatabase.Refresh();
				    cachedSettings = null;
				    TryGetSettings(out var newSettings);
				    cachedSettings = newSettings;
			    };
#else
				settings = ScriptableObject.CreateInstance<GLTFSettings>();
#endif
		    }
#if UNITY_EDITOR
		    RegisterPlugins(settings);
#endif
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

#if UNITY_EDITOR
	    private static void RegisterPlugins(GLTFSettings settings)
	    {
		    if(settings.ImportPlugins == null) settings.ImportPlugins = new List<GltfImportPlugin>();

		    foreach (var pluginType in TypeCache.GetTypesDerivedFrom<GltfImportPlugin>())
		    {
			    if (pluginType.IsAbstract) continue;
			    // If the plugin already exists we dont want to add it again
			    if (settings.ImportPlugins.Any(p => p != null && p.GetType() == pluginType))
				    continue;
			    if (typeof(ScriptableObject).IsAssignableFrom(pluginType))
			    {
					var newInstance = ScriptableObject.CreateInstance(pluginType) as GltfImportPlugin;
				    settings.ImportPlugins.Add(newInstance);
			    }
		    }
	    }
#endif
    }
}
