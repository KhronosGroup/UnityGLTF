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

		private const string ExportGlb = "Export selected";
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
	    
	    private struct ExportBatch
	    {
		    public string sceneName;
		    public Transform[] rootTransforms;
		    public Object[] rootResources;
		    public SceneAsset[] sceneAssets;
		    public TransformData?[] rootTransformOverride;
	    }

	    private struct TransformData
	    {
		    public Vector3 position;
		    public Quaternion rotation;
		    public Vector3 scale;
	    }
	    
	    private static GLTFSettings _cachedSettings;
	    
	    private static GLTFSettings settings
	    { 
		    get { 
			    if (!_cachedSettings)
			    {
				    _cachedSettings = GLTFSettings.GetOrCreateSettings();
			    }
			    return _cachedSettings;
			} 
	    }

	    private static TransformData? GetOverride(Transform transform)
	    {
		    switch (settings.EditorExportTransformMode)
		    {
			    // Heuristic for correcting the placement of the object in the scene
			    // Keep world scale
			    //   This is questionable; alternative: keep local UNIFORM scale, so when the parent is stretched we don't want to inherit that stretch.
			    //   Otherwise, exporting a child of a stretched object will result in an exported object that looks visually different from the scene. 
			    // Keep local rotation
			    // Adjust local position
			    // - easy case for now: set to 0
			    // - better heuristic might be: if current local position in any axis fits into the bounds: keep that axis; if it doesn't fit into the bounds: set to 0
			    case GLTFSettings.TransformMode.Auto:
				    return new TransformData
				    {
					    position = Vector3.zero, 
					    rotation = transform.localRotation, 
					    scale = transform.lossyScale
				    };
			    case GLTFSettings.TransformMode.WorldTransforms:
				    return new TransformData
				    {
					    position = transform.position, 
					    rotation = transform.rotation, 
					    scale = transform.lossyScale
				    };
			    case GLTFSettings.TransformMode.Reset:
				    var normalizedScale = transform.lossyScale;
				    var maxEdge = Math.Max(normalizedScale.x, Math.Max(normalizedScale.y, normalizedScale.z));
				    if (maxEdge < 0.00001f)
					    normalizedScale = Vector3.one;
				    else
						normalizedScale /= maxEdge;
				    return new TransformData()
				    {
					    position = Vector3.zero,
					    rotation = Quaternion.identity,
					    scale = normalizedScale,
				    };
			    // This is the built-in case for GLTFExporter: it exports the local transforms of nodes into glTF.
			    case GLTFSettings.TransformMode.LocalTransforms:
			    default:
				    return null;
		    }
	    }
	    
	    /// <summary>
	    /// This method collects GameObjects into export jobs. It supports multi-selection exports in various useful ways.
	    /// 1. When multiple scene objects are selected, they are exported together as one file.
	    /// 2. When multiple prefabs are selected, each one gets exported as individual file.
	    /// 3. When multiple scenes are selected, each scene gets exported as individual file.
	    /// </summary>
	    /// <returns>True if the current selection can be exported.</returns>
	    private static bool TryGetExportBatchesFromSelection(out List<ExportBatch> batches, bool separateBatches)
	    {
		    if (Selection.transforms.Length > 1)
		    {
			    if (!separateBatches)
			    {
				    // A better Auto heuristic for rootTransformOverride here is more complicated; we would need to determine what the shared root of those objects is, and
				    // decide if we want to keep world position or keep local position in the shared root.
				    batches = new List<ExportBatch>()
				    {
					    new()
					    {
						    sceneName = SceneManager.GetActiveScene().name,
						    rootTransforms = Selection.transforms,
						    rootTransformOverride = Selection.transforms.Select(GetOverride).ToArray(),
					    }
				    };
			    }
			    else
			    {
				    batches = new List<ExportBatch>();
				    foreach (var transform in Selection.transforms)
				    {
					    batches.Add(new ExportBatch()
					    {
						    sceneName = transform.name,
						    rootTransforms = new[] { transform },
						    rootTransformOverride = new[] { GetOverride(transform) },
					    });
				    }
			    }
			    return true;
		    }
		    
		    // Special case for one selected object: use the object's name as the scene name
		    if (Selection.transforms.Length == 1)
		    {
			    var transform = Selection.transforms[0];
			    batches = new List<ExportBatch>()
			    {
				    new()
				    {
					    sceneName = Selection.activeGameObject.name,
					    rootTransforms = new[] { transform },
					    rootTransformOverride = new[] { GetOverride(transform) },
				    }
			    };
			    return true;
		    }
		    
		    // Project object selection
		    if (Selection.objects.Any() && Selection.objects.All(x => x is GameObject))
		    {
			    if (Selection.objects.Length <= 1) separateBatches = true;
			    if (!separateBatches)
			    {
				    batches = new List<ExportBatch>()
				    {
					    new()
					    {
						    sceneName = Selection.objects.First().name,
						    rootTransforms = Array.ConvertAll(Selection.objects, x => (Transform)x),
					    }
				    };
			    }
			    else
			    {
				    batches = new List<ExportBatch>();
				    foreach (var obj in Selection.objects)
				    {
					    var go = (GameObject) obj;
					    batches.Add(new ExportBatch()
					    {
						    sceneName = go.name,
						    rootTransforms = new[] { go.transform },
					    });
				    }
			    }
			    return true;
		    }

		    // Project material selection
		    if (Selection.objects.Any() && Selection.objects.All(x => x is Material))
		    {
			    if (Selection.objects.Length <= 1) separateBatches = true;
			    if (!separateBatches)
			    {
				    batches = new List<ExportBatch>()
				    {
					    new() {
						    sceneName = "Material Library",
						    rootResources = Selection.objects,
					    }
				    };
			    }
			    else
			    {
				    batches = new List<ExportBatch>();
				    foreach (var obj in Selection.objects)
				    {
					    var material = (Material) obj;
					    batches.Add(new ExportBatch()
					    {
						    sceneName = material.name,
						    rootResources = new Object[] { material },
					    });
				    }
			    }
			    return true;
		    }
		    
		    // Project scene selection
		    if (Selection.objects.Any() && Selection.objects.All(x => x is SceneAsset))
		    {
			    if (Selection.objects.Length <= 1) separateBatches = true;
			    batches = new List<ExportBatch>();
			    if (!separateBatches)
			    {
				    batches.Add(new ExportBatch()
				    {
					    sceneName = "Scenes",
					    sceneAssets = Selection.objects.Cast<SceneAsset>().ToArray(),
				    });
			    }
			    else
			    {
				    foreach (var obj in Selection.objects)
				    {
					    var scene = (SceneAsset) obj;
					    batches.Add(new()
					    {
						    sceneName = scene.name,
						    sceneAssets = new[] { scene },
					    });
				    }
			    }
			    /*
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
			    */
			    return true;
		    }

		    batches = null;
		    return false;
	    }

	    private class ExportStage: PreviewSceneStage
	    {
		    private static MethodInfo _openPreviewScene;

		    protected override bool OnOpenStage() => true;

// 		    public void Setup(string scenePath)
// 		    {
// #if !UNITY_2023_1_OR_NEWER
// 			    if (_openPreviewScene == null) _openPreviewScene = typeof(EditorSceneManager).GetMethod("OpenPreviewScene", (BindingFlags)(-1), null, new[] {typeof(string), typeof(bool)}, null);
// 			    if (_openPreviewScene == null) return;
// 			    
// 			    scene = (Scene) _openPreviewScene.Invoke(null, new object[] { scenePath, false });
// #else
// 			    scene = EditorSceneManager.OpenPreviewScene(scenePath, false);
// #endif
// 		    }
		    
		    protected override GUIContent CreateHeaderContent()
		    {
			    return new GUIContent("Export: " + scene.name);
		    }
	    }
	    
	    private static bool ExportBinary => settings.EditorExportFileFormat == GLTFSettings.ExportFileFormat.Glb;
	    private const int Priority = 34;

		[MenuItem(MenuPrefix + ExportGlb + " &SPACE", true, Priority)]
		[MenuItem(MenuPrefixGameObject + ExportGlb, true, Priority)]
		private static bool ExportSelectedValidate()
		{
			return TryGetExportBatchesFromSelection(out _, false);
		}

		[MenuItem(MenuPrefix + ExportGlb + " &SPACE", false, Priority)]
		[MenuItem(MenuPrefixGameObject + ExportGlb, false, Priority)]
		private static void ExportSelected(MenuCommand command)
		{
			// The exporter handles multi-selection. We don't want to call export multiple times here.
			if (command.context && Selection.objects.Length > 1 && command.context != Selection.objects[0])
				return;
			
			_ExportSelected(false);
		}
		
		[MenuItem(MenuPrefix + ExportGlbBatch, true, Priority + 1)]
		[MenuItem(MenuPrefixGameObject + ExportGlbBatch, true, Priority + 1)]
		private static bool ExportSelectedBatchValidate()
		{
			if (Selection.objects.Length < 2) return false;
			return TryGetExportBatchesFromSelection(out _, false);
		}
		
		[MenuItem(MenuPrefix + ExportGlbBatch, false, Priority + 1)]
		[MenuItem(MenuPrefixGameObject + ExportGlbBatch, false, Priority + 1)]
		private static void ExportSelectedBatch(MenuCommand command)
		{
			// The exporter handles multi-selection. We don't want to call export multiple times here.
			if (command.context && Selection.objects.Length > 1 && command.context != Selection.objects[0])
				return;
		
			_ExportSelected(true);
		}

		private static void _ExportSelected(bool separateBatches)
		{
			if (!TryGetExportBatchesFromSelection(out var batches, separateBatches))
			{
				Debug.LogError("Can't export: selection is empty");
				return;
			}

			for (var i = 0; i < batches.Count; i++)
			{
				var success = Export(batches[i], ExportBinary, i == 0);
				if (!success) break;
			}
		}

		[MenuItem(MenuPrefix + "Export active scene", true, Priority + 2)]
		private static bool ExportSceneValidate()
		{
			var activeScene = SceneManager.GetActiveScene();
			if (!activeScene.IsValid()) return false;
			return true;
		}
		
		[MenuItem(MenuPrefix + "Export active scene", false, Priority + 2)]
		private static void ExportScene()
		{
			var scene = SceneManager.GetActiveScene();
			ExportScene(scene);
		}
		
		private static void ExportScene(Scene scene)
		{
			var gameObjects = scene.GetRootGameObjects();
			var transforms = Array.ConvertAll(gameObjects, gameObject => gameObject.transform);

			Export(new ExportBatch() {
				rootTransforms = transforms,
				sceneName = scene.name,
				rootResources = null,
			}, ExportBinary, true);
		}

		private static void ExportAllScenes()
		{
			var roots = new List<Transform>();
			var scenesThatWereNotLoaded = new List<Scene>();
			
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (!scene.IsValid()) continue;
				var sceneWasLoaded = scene.isLoaded;
				if (!sceneWasLoaded)
				{
					EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
					scenesThatWereNotLoaded.Add(scene);
				}
				roots.AddRange(Array.ConvertAll(scene.GetRootGameObjects(), gameObject => gameObject.transform));
			}				
			
			Export(new ExportBatch() {
				rootTransforms = roots.ToArray(),
				sceneName = SceneManager.GetActiveScene().name,
				rootResources = null,
			}, ExportBinary, true);
			
			foreach (var scene in scenesThatWereNotLoaded)
			{
				if (!scene.isLoaded) continue;
				EditorSceneManager.CloseScene(scene, false);
			}
		}

		private static void ExportAllScenesBatch()
		{
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (!scene.IsValid()) continue;
				
				var sceneWasLoaded = scene.isLoaded;
				if (!sceneWasLoaded) EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
				
				var roots = Array.ConvertAll(scene.GetRootGameObjects(), gameObject => gameObject.transform);
				var success = Export(new ExportBatch() {
					rootTransforms = roots,
					sceneName = scene.name,
					rootResources = null,
				}, ExportBinary, i == 0);
				
				if (!sceneWasLoaded) EditorSceneManager.CloseScene(scene, false);
				if (!success) break;
			}
		}

		private static bool Export(ExportBatch batch, bool binary, bool askForLocation)
		{
			var currentlyOpenScene = SceneManager.GetActiveScene().path;
			
			if (batch.sceneAssets != null)
			{
				if (batch.sceneAssets.Length == 0)
				{
					// Successful export of empty batch
					return true;
				}
				
				if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				{
					var first = batch.sceneAssets.First();
					EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(first), OpenSceneMode.Single);
					foreach (var scene in batch.sceneAssets)
					{
						if (scene == first) continue;
						EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scene), OpenSceneMode.Additive);
					}
					
					var sceneRoots = new List<Transform>();
					for (var i = 0; i < SceneManager.sceneCount; i++)
					{
						var scene = SceneManager.GetSceneAt(i);
						if (!scene.IsValid()) continue;
						sceneRoots.AddRange(Array.ConvertAll(scene.GetRootGameObjects(), gameObject => gameObject.transform));
					}
					batch.rootTransforms = sceneRoots.ToArray();
				}
			}
			
			var exportOptions = new ExportContext(settings) { TexturePathRetriever = RetrieveTexturePath };
			
			var haveOverrides = batch.rootTransformOverride != null && batch.rootTransformOverride.Length > 0 && batch.rootTransforms != null;
			var originalTransforms = batch.rootTransforms?.Select(x => (x.localPosition, x.localRotation, x.localScale)).ToArray();
			if (haveOverrides)
			{
				var overrideLength = batch.rootTransformOverride.Length;
				for (int i = 0; i < batch.rootTransforms.Length; i++)
				{
					var overrideData = batch.rootTransformOverride[i % overrideLength];
					if (!overrideData.HasValue) continue;
					batch.rootTransforms[i].localPosition = overrideData.Value.position;
					batch.rootTransforms[i].localRotation = overrideData.Value.rotation;
					batch.rootTransforms[i].localScale = overrideData.Value.scale;
				}
			}
			var exporter = new GLTFSceneExporter(batch.rootTransforms, exportOptions);

			if (batch.rootResources != null)
			{
				exportOptions.AfterSceneExport += (sceneExporter, _) =>
				{
					foreach (var resource in batch.rootResources)
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
			if ((askForLocation && !invokedByShortcut) || !Directory.Exists(path))
				path = EditorUtility.SaveFolderPanel("glTF Export Path", settings.SaveFolderPath, "");

			var havePath = !string.IsNullOrEmpty(path);
			if (havePath)
			{
				var sceneName = batch.sceneName;
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
			
			if (haveOverrides)
			{
				for (var i = 0; i < batch.rootTransforms.Length; i++)
				{
					var (position, rotation, scale) = originalTransforms[i];
					batch.rootTransforms[i].localPosition = position;
					batch.rootTransforms[i].localRotation = rotation;
					batch.rootTransforms[i].localScale = scale;
				}
			}
			
			if (currentlyOpenScene != SceneManager.GetActiveScene().path)
				EditorSceneManager.OpenScene(currentlyOpenScene, OpenSceneMode.Single);

			return havePath;
		}
		
		const string SettingsMenu = "Open Export Settings";
		[MenuItem(MenuPrefixGameObject + SettingsMenu, true, 3000)]
		private static bool ShowSettingsValidate()
		{
			return TryGetExportBatchesFromSelection(out _, false);
		}

		[InitializeOnLoadMethod]
		private static void Hooks()
		{
			SceneHierarchyHooks.addItemsToSceneHeaderContextMenu += (menu, scene) =>
			{
				menu.AddItem(new GUIContent("UnityGLTF/Export selected scene"), false, () => ExportScene(scene));
				if (SceneManager.sceneCount > 1)
				{
					menu.AddItem(new GUIContent("UnityGLTF/Export each scene as separate asset"), false, ExportAllScenesBatch);
					menu.AddItem(new GUIContent("UnityGLTF/Export all scenes as one asset"), false, ExportAllScenes);
				}
			};
			
			SceneHierarchyHooks.addItemsToGameObjectContextMenu += (menu, gameObject) =>
			{
				if (gameObject)
				{
					var current = settings.EditorExportFileFormat == GLTFSettings.ExportFileFormat.Glb;
					menu.AddItem(new GUIContent("UnityGLTF/Export as binary (GLB)"), current, () =>
					{
						current = !current;
						settings.EditorExportFileFormat = current ? GLTFSettings.ExportFileFormat.Glb : GLTFSettings.ExportFileFormat.Gltf;
					});
				}
				else
				{
					menu.AddItem(new GUIContent("UnityGLTF/Export active scene"), false, ExportScene);
					if (SceneManager.sceneCount > 1)
					{
						menu.AddItem(new GUIContent("UnityGLTF/Export each scene as separate asset"), false, ExportAllScenesBatch);
						menu.AddItem(new GUIContent("UnityGLTF/Export all scenes as one asset"), false, ExportAllScenes);
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
			var current = settings.EditorExportFileFormat == GLTFSettings.ExportFileFormat.Glb;
			current = !current;
			settings.EditorExportFileFormat = current ? GLTFSettings.ExportFileFormat.Glb : GLTFSettings.ExportFileFormat.Gltf;
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
