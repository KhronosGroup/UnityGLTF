using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityGLTF.Plugins;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF
{

    public class GLTFSettings : ScriptableObject
    {
	    private const string k_PreferencesPrefix = "UnityGLTF_Preferences_";
	    private const string k_SettingsFileName = "UnityGLTFSettings.asset";
	    public const string k_RuntimeAndEditorSettingsPath = "Assets/Resources/" + k_SettingsFileName;

	    [Flags]
	    public enum BlendShapeExportPropertyFlags
	    {
		    None = 0,
		    PositionOnly = 1,
		    Normal = 2,
		    Tangent = 4,
		    All = ~0
	    }

	    // Plugins
	    [SerializeField, HideInInspector]
	    public List<GLTFImportPlugin> ImportPlugins = new List<GLTFImportPlugin>();
	    
	    [SerializeField, HideInInspector]
	    public List<GLTFExportPlugin> ExportPlugins = new List<GLTFExportPlugin>();
	    
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
		// [Tooltip("(Experimental) Export animations using KHR_animation_pointer. Requires the viewer to also support this extension.")]
		// [SerializeField]
		// private bool useAnimationPointer = false;
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

		[Obsolete("Add/remove \"AnimationPointerPlugin\" from ExportPlugins instead.")]
		public bool UseAnimationPointer
		{
			get
			{
				return ExportPlugins?.Any(x => x is AnimationPointerExport && x.Enabled) ?? false;
			}
			set
			{
				var plugin = ExportPlugins?.FirstOrDefault(x => x is AnimationPointerExport);
				if (plugin != null)
					plugin.Enabled = value;
				if (!value || plugin != null) return;
				
				if (ExportPlugins == null) ExportPlugins = new List<GLTFExportPlugin>();
				ExportPlugins.Add(CreateInstance<AnimationPointerExport>());
#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
#endif
			}
		}
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
	    
	    public static GLTFSettings GetOrCreateSettings()
	    {
		    #if UNITY_EDITOR
		    var hadSettings = true;
		    #endif
		    if (!TryGetSettings(out var settings))
		    {
#if UNITY_EDITOR
			    hadSettings = false;
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
		    
		    RegisterPlugins(settings);
		    
#if UNITY_EDITOR		    
		    // save again with plugins attached, if needed - the asset was only created in memory
		    if (!hadSettings && !AssetDatabase.Contains(settings))
				UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { settings }, k_RuntimeAndEditorSettingsPath, true);
#endif
		    cachedSettings = settings;
		    return settings;
	    }

	    public static GLTFSettings GetDefaultSettings()
	    {
			var freshSettings = CreateInstance<GLTFSettings>();
		    RegisterPlugins(freshSettings);
		    return freshSettings;
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

	    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	    private static void ClearStatics()
	    {
		    cachedSettings = null;
		    settingsWherePluginsAreRegistered.Clear();
	    }
	    
	    private static GLTFSettings cachedSettings;
	    private static List<GLTFSettings> settingsWherePluginsAreRegistered = new List<GLTFSettings>();
	    
	    private static void RegisterPlugins(GLTFSettings settings)
	    {
		    if (settingsWherePluginsAreRegistered.Contains(settings)) return;
		    
		    static List<Type> GetTypesDerivedFrom<T>()
		    {
#if UNITY_EDITOR
			    return TypeCache.GetTypesDerivedFrom<T>().ToList();
#else
			    var types = new List<Type>();
			    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			    {
				    try
				    {
					    types.AddRange(assembly.GetTypes().Where(t => !(t is T) && typeof(T).IsAssignableFrom(t)));
				    }
				    catch (ReflectionTypeLoadException e)
				    {
					    types.AddRange(e.Types);
				    }
				    catch (Exception)
				    {
					    // ignored
				    }
			    }
			    return types;
#endif
		    }
		    
		    // Initialize
		    if (settings.ImportPlugins == null) settings.ImportPlugins = new List<GLTFImportPlugin>();
		    if (settings.ExportPlugins == null) settings.ExportPlugins = new List<GLTFExportPlugin>();

		    // Cleanup
		    settings.ImportPlugins.RemoveAll(x => x == null);
		    settings.ExportPlugins.RemoveAll(x => x == null);
		    
		    void FindAndRegisterPlugins<T>(List<T> plugins) where T : GLTFPlugin
		    {
			    foreach (var pluginType in GetTypesDerivedFrom<T>())
			    {
				    if (pluginType.IsAbstract) continue;
				    if (plugins.Any(p => p != null && p.GetType() == pluginType))
					    continue;
				    
				    if (typeof(ScriptableObject).IsAssignableFrom(pluginType))
				    {
					    var newInstance = CreateInstance(pluginType) as T;
					    if (!newInstance) continue;

					    newInstance.name = pluginType.Name;
						newInstance.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
					    newInstance.Enabled = newInstance.EnabledByDefault;
					    
					    plugins.Add(newInstance);
#if UNITY_EDITOR
					    if (AssetDatabase.Contains(settings))
					    {
							AssetDatabase.AddObjectToAsset(newInstance, settings);
							EditorUtility.SetDirty(settings);
					    }
#endif
				    }
			    }
		    }
		    
		    // Register with TypeCache
		    FindAndRegisterPlugins(settings.ImportPlugins);
		    FindAndRegisterPlugins(settings.ExportPlugins);
		    
		    settingsWherePluginsAreRegistered.Add(settings);
	    }
    }
}
