using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	public static class GLTFExportMenu
	{
		private const string MenuPrefix = "Assets/UnityGLTF/";
		private const string MenuPrefixGameObject = "GameObject/UnityGLTF/";

		private const string ExportGlb = "Export selected as one asset";
		private const string ExportGlbBatch = "Export each as separate asset";

	    public static string RetrieveTexturePath(UnityEngine.Texture texture)
	    {
	        var path = AssetDatabase.GetAssetPath(texture);
	        // texture is a subasset
	        if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(Texture2D))
	        {
		        var ext = Path.GetExtension(path);
		        if (string.IsNullOrWhiteSpace(ext)) return texture.name + ".png";
		        path = path.Replace(ext, "-" + texture.name + ext);
	        }
	        return path;
	    }

	    /// <summary>
	    /// This method collects GameObjects into export jobs. It supports multi-selection exports in various useful ways.
	    /// 1. When multiple scene objects are selected, they are exported together as one file.
	    /// 2. When multiple prefabs are selected, each one is exported as individual file.
	    /// 3. When multiple scenes are selected, each scene is exported as individual file.
	    /// </summary>
	    /// <returns>True if the current selection can be exported.</returns>
	    private static bool TryGetExportNameAndRootTransformsFromSelection(out string sceneName, out Transform[] rootTransforms, out Object[] rootResources, bool openSceneIfNeeded)
	    {
		    if (Selection.transforms.Length > 1)
		    {
			    sceneName = SceneManager.GetActiveScene().name;
			    rootTransforms = Selection.transforms;
			    rootResources = null;
			    return true;
		    }
		    if (Selection.transforms.Length == 1)
		    {
			    sceneName = Selection.activeGameObject.name;
			    rootTransforms = Selection.transforms;
			    rootResources = null;
			    return true;
		    }
		    if (Selection.objects.Any() && Selection.objects.All(x => x is GameObject))
		    {
			    sceneName = Selection.objects.First().name;
			    rootTransforms = Array.ConvertAll(Selection.objects, x => (Transform)x);
			    rootResources = null;
			    return true;
		    }

		    if (Selection.objects.Any() && Selection.objects.All(x => x is Material))
		    {
			    sceneName = "Material Library";
			    rootTransforms = null;
			    rootResources = Selection.objects;
			    return true;
		    }
		    
		    if (Selection.objects.Any() && Selection.objects.All(x => x is SceneAsset))
		    {
			    sceneName = Selection.objects.First().name;
			    if (openSceneIfNeeded)
			    {
				    var firstScene = (SceneAsset) Selection.objects.First();
				    var stage = ScriptableObject.CreateInstance<ExportStage>();
				    stage.Setup(AssetDatabase.GetAssetPath(firstScene));
				    StageUtility.GoToStage(stage, true);
				    var roots = stage.scene.GetRootGameObjects();
				    rootTransforms = Array.ConvertAll(roots, x => x.transform);
				    rootResources = null;
				    return true;
			    }
			    rootTransforms = null;
			    rootResources = null;
			    return true;
		    }

		    sceneName = null;
		    rootTransforms = null;
		    rootResources = null;
		    return false;
	    }

	    private class ExportStage: PreviewSceneStage
	    {
		    private static MethodInfo _openPreviewScene;

		    protected override bool OnOpenStage() => true;

		    public void Setup(string scenePath)
		    {
#if !UNITY_2023_1_OR_NEWER
			    if (_openPreviewScene == null) _openPreviewScene = typeof(EditorSceneManager).GetMethod("OpenPreviewScene", (BindingFlags)(-1), null, new[] {typeof(string), typeof(bool)}, null);
			    if (_openPreviewScene == null) return;
			    
			    scene = (Scene) _openPreviewScene.Invoke(null, new object[] { scenePath, false });
#else
			    scene = EditorSceneManager.OpenPreviewScene(scenePath, false);
#endif
		    }
		    
		    protected override GUIContent CreateHeaderContent()
		    {
			    return new GUIContent("Export: " + scene.name);
		    }
	    }
	    
	    private static bool ExportBinary => !SessionState.GetBool(ExportAsBinary, false);

		[MenuItem(MenuPrefix + ExportGlb + " &SPACE", true)]
		[MenuItem(MenuPrefixGameObject + ExportGlb, true)]
		private static bool ExportSelectedValidate()
		{
			return TryGetExportNameAndRootTransformsFromSelection(out _, out _, out _, false);
		}

		[MenuItem(MenuPrefix + ExportGlb + " &SPACE")]
		[MenuItem(MenuPrefixGameObject + ExportGlb, false, 34)]
		private static void ExportSelected(MenuCommand command)
		{
			// The exporter handles multi-selection. We don't want to call export multiple times here.
			if (Selection.gameObjects.Length > 1 && command.context != Selection.gameObjects[0] && !(Selection.activeObject is SceneAsset))
				return;
			
			if (!TryGetExportNameAndRootTransformsFromSelection(out var sceneName, out var rootTransforms, out var rootResources, true))
			{
				Debug.LogError("Can't export: selection is empty");
				return;
			}
			Export(rootTransforms, rootResources, ExportBinary, sceneName);
		}
		
		[MenuItem(MenuPrefix + ExportGlbBatch, true)]
		[MenuItem(MenuPrefixGameObject + ExportGlbBatch, true)]
		private static bool ExportSelectedBatchValidate()
		{
			return TryGetExportNameAndRootTransformsFromSelection(out _, out _, out _, false);
		}
		
		[MenuItem(MenuPrefix + ExportGlbBatch)]
		[MenuItem(MenuPrefixGameObject + ExportGlbBatch, false, 34)]
		private static void ExportSelectedBatch(MenuCommand command)
		{
			// The exporter handles multi-selection. We don't want to call export multiple times here.
			if (Selection.gameObjects.Length > 1 && command.context != Selection.gameObjects[0] && !(Selection.activeObject is SceneAsset))
				return;
			
			if (!TryGetExportNameAndRootTransformsFromSelection(out var sceneName, out var rootTransforms, out var rootResources, true))
			{
				Debug.LogError("Can't export: selection is empty");
				return;
			}
			Export(rootTransforms, rootResources, ExportBinary, sceneName);
		}

		[MenuItem(MenuPrefix + "Export active scene", true)]
		private static bool ExportSceneValidate()
		{
			var activeScene = SceneManager.GetActiveScene();
			if (!activeScene.IsValid()) return false;
			return true;
		}
		
		[MenuItem(MenuPrefix + "Export active scene")]
		private static void ExportScene()
		{
			var scene = SceneManager.GetActiveScene();
			ExportScene(scene);
		}
		
		private static void ExportScene(Scene scene)
		{
			var gameObjects = scene.GetRootGameObjects();
			var transforms = Array.ConvertAll(gameObjects, gameObject => gameObject.transform);

			Export(transforms, null, ExportBinary, scene.name);
		}

		private static void ExportAllScenes()
		{
			var roots = new List<Transform>();
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (!scene.IsValid()) continue;
				roots.AddRange(Array.ConvertAll(scene.GetRootGameObjects(), gameObject => gameObject.transform));
			}
			Export(roots.ToArray(), null, ExportBinary, SceneManager.GetActiveScene().name);
		}

		private static void ExportAllScenesBatch()
		{
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (!scene.IsValid()) continue;
				var roots = Array.ConvertAll(scene.GetRootGameObjects(), gameObject => gameObject.transform);
				Export(roots, null, ExportBinary, scene.name);
			}
		}

		private static void Export(Transform[] transforms, Object[] resources, bool binary, string sceneName)
		{
			var settings = GLTFSettings.GetOrCreateSettings();
			var exportOptions = new ExportContext(settings) { TexturePathRetriever = RetrieveTexturePath };
			var exporter = new GLTFSceneExporter(transforms, exportOptions);

			if (resources != null)
			{
				exportOptions.AfterSceneExport += (sceneExporter, _) =>
				{
					foreach (var resource in resources)
					{
						if (resource is Material material)
							sceneExporter.ExportMaterial(material);
						if (resource is Texture2D texture)
							sceneExporter.ExportTexture(texture, "unknown");
						if (resource is Mesh mesh)
							sceneExporter.ExportMesh(mesh);
					}
				};
			}

			var invokedByShortcut = Event.current?.type == EventType.KeyDown;
			var path = settings.SaveFolderPath;
			if (!invokedByShortcut || !Directory.Exists(path))
				path = EditorUtility.SaveFolderPanel("glTF Export Path", settings.SaveFolderPath, "");

			if (!string.IsNullOrEmpty(path))
			{
				var ext = binary ? ".glb" : ".gltf";
				var resultFile = GLTFSceneExporter.GetFileName(path, sceneName, ext);
				settings.SaveFolderPath = path;
				
				if (binary)
					exporter.SaveGLB(path, sceneName);
				else
					exporter.SaveGLTFandBin(path, sceneName);

				Debug.Log("Exported to " + resultFile);
				EditorUtility.RevealInFinder(resultFile);
			}
		}
		
		const string SettingsMenu = "Open Export Settings";
		[MenuItem(MenuPrefixGameObject + SettingsMenu, true, 3000)]
		private static bool ShowSettingsValidate()
		{
			return TryGetExportNameAndRootTransformsFromSelection(out _, out _, out _, false);
		}

		[InitializeOnLoadMethod]
		private static void Hooks()
		{
			SceneHierarchyHooks.addItemsToSceneHeaderContextMenu += (menu, scene) =>
			{
				menu.AddItem(new GUIContent("UnityGLTF/Export selected scene"), false, () => ExportScene(scene));
			};
			
			SceneHierarchyHooks.addItemsToGameObjectContextMenu += (menu, gameObject) =>
			{
				if (gameObject)
				{
					var current = SessionState.GetBool(ExportAsBinary, false);
					menu.AddItem(new GUIContent("UnityGLTF/Export as binary (GLB)"), !current, () =>
					{
						current = !current;
						SessionState.SetBool(ExportAsBinary, current);
					});
				}
				else
				{
					menu.AddItem(new GUIContent("UnityGLTF/Export active scene"), false, ExportScene);
					if (SceneManager.loadedSceneCount > 1)
					{
						menu.AddItem(new GUIContent("UnityGLTF/Export all scenes as one asset"), false, ExportAllScenes);
						menu.AddItem(new GUIContent("UnityGLTF/Export each scene as separate asset"), false, ExportAllScenesBatch);
					}
				}
			};
		} 
		
		[MenuItem(MenuPrefix + SettingsMenu, false, 3000)]
		[MenuItem(MenuPrefixGameObject + SettingsMenu, false, 3000)]
		private static void ShowSettings()
		{
			SettingsService.OpenProjectSettings("Project/UnityGLTF");
		}
		
		const string ExportAsBinary = MenuPrefix + "Export as binary (GLB)";
		[MenuItem(ExportAsBinary, false, 3001)]
		private static void ToggleExportAsGltf()
		{
			var current = SessionState.GetBool(ExportAsBinary, true);
			current = !current;
			SessionState.SetBool(ExportAsBinary, current);
			Menu.SetChecked(ExportAsBinary, current);
		}
	}

	internal static class GLTFCreateMenu
	{
		[MenuItem("Assets/Create/UnityGLTF/Material", false)]
		private static void CreateNewAsset()
		{
			var filename = "glTF Material Library.gltf";
			var content = @"{
	""asset"": {
		""generator"": ""UnityGLTF"",
		""version"": ""2.0""
	},
	""materials"": [
		{
			""name"": ""Material"",
			""pbrMetallicRoughness"": {
				""metallicFactor"": 0.0
			}
		}
	]
}";

			var importAction = ScriptableObject.CreateInstance<AdjustImporterAction>();
			importAction.fileContent = content;
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, importAction, filename, null, (string) null);
		}

		// Based on DoCreateAssetWithContent.cs
		private class AdjustImporterAction : EndNameEditAction
		{
			public string fileContent;
			public override void Action(int instanceId, string pathName, string resourceFile)
			{
				var templateContent = SetLineEndings(fileContent, EditorSettings.lineEndingsForNewScripts);
				File.WriteAllText(Path.GetFullPath(pathName), templateContent);
				AssetDatabase.ImportAsset(pathName);
				// This is why we're not using ProjectWindowUtil.CreateAssetWithContent directly:
				// We want glTF materials created with UnityGLTF to also use UnityGLTF for importing.
				AssetDatabase.SetImporterOverride<GLTFImporter>(pathName);
				var asset = AssetDatabase.LoadAssetAtPath(pathName, typeof (UnityEngine.Object));
				ProjectWindowUtil.ShowCreatedAsset(asset);
			}
		}
		
		// Unmodified from ProjectWindowUtil.cs:SetLineEndings (internal)
		private static string SetLineEndings(string content, LineEndingsMode lineEndingsMode)
		{
			string replacement;
			switch (lineEndingsMode)
			{
				case LineEndingsMode.OSNative:
					replacement = Application.platform != RuntimePlatform.WindowsEditor ? "\n" : "\r\n";
					break;
				case LineEndingsMode.Unix:
					replacement = "\n";
					break;
				case LineEndingsMode.Windows:
					replacement = "\r\n";
					break;
				default:
					replacement = "\n";
					break;
			}
			content = Regex.Replace(content, "\\r\\n?|\\n", replacement);
			return content;
		}
	}
}
