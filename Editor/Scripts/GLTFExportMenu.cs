using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	public static class GLTFExportMenu
	{
		private const string MenuPrefix = "Assets/UnityGLTF/";
		private const string MenuPrefixGameObject = "GameObject/UnityGLTF/";

		private const string ExportGltf = "Export selected as glTF";
		private const string ExportGlb = "Export selected as GLB";

	    public static string RetrieveTexturePath(UnityEngine.Texture texture)
	    {
	        var path = AssetDatabase.GetAssetPath(texture);
	        // texture is a subasset
	        if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(Texture2D))
	        {
		        var ext = System.IO.Path.GetExtension(path);
		        if (string.IsNullOrWhiteSpace(ext)) return texture.name + ".png";
		        path = path.Replace(ext, "-" + texture.name + ext);
	        }
	        return path;
	    }

	    private static bool TryGetExportNameAndRootTransformsFromSelection(out string sceneName, out Transform[] rootTransforms, out Object[] rootResources)
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
			    rootTransforms = Selection.objects.Select(x => (x as GameObject).transform).ToArray();
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

		    sceneName = null;
		    rootTransforms = null;
		    rootResources = null;
		    return false;
	    }

	    [MenuItem(MenuPrefix + ExportGltf, true)]
	    [MenuItem(MenuPrefixGameObject + ExportGltf, true)]
	    private static bool ExportSelectedValidate()
	    {
		    return TryGetExportNameAndRootTransformsFromSelection(out _, out _, out _);
	    }

	    [MenuItem(MenuPrefix + ExportGltf)]
	    [MenuItem(MenuPrefixGameObject + ExportGltf, false, 33)]
	    private static void ExportSelected(MenuCommand command)
		{
			// The exporter handles multi-selection. We don't want to call export multiple times here.
			if (Selection.gameObjects.Length > 1 && command.context != Selection.gameObjects[0])
				return;
			
			if (!TryGetExportNameAndRootTransformsFromSelection(out var sceneName, out var rootTransforms, out var rootResources))
			{
				Debug.LogError("Can't export: selection is empty");
				return;
			}

			Export(rootTransforms, rootResources, false, sceneName);
		}

		[MenuItem(MenuPrefix + ExportGlb, true)]
		[MenuItem(MenuPrefixGameObject + ExportGlb, true)]
		private static bool ExportGLBSelectedValidate()
		{
			return TryGetExportNameAndRootTransformsFromSelection(out _, out _, out _);
		}

		[MenuItem(MenuPrefix + ExportGlb)]
		[MenuItem(MenuPrefixGameObject + ExportGlb, false, 34)]
		private static void ExportGLBSelected(MenuCommand command)
		{
			// The exporter handles multi-selection. We don't want to call export multiple times here.
			if (Selection.gameObjects.Length > 1 && command.context != Selection.gameObjects[0])
				return;
			
			if (!TryGetExportNameAndRootTransformsFromSelection(out var sceneName, out var rootTransforms, out var rootResources))
			{
				Debug.LogError("Can't export: selection is empty");
				return;
			}
			Export(rootTransforms, rootResources, true, sceneName);
		}

		[MenuItem(MenuPrefix + "Export active scene as glTF")]
		private static void ExportScene()
		{
			var scene = SceneManager.GetActiveScene();
			var gameObjects = scene.GetRootGameObjects();
			var transforms = Array.ConvertAll(gameObjects, gameObject => gameObject.transform);

			Export(transforms, null, false, scene.name);
		}

		[MenuItem(MenuPrefix + "Export active scene as GLB")]
		private static void ExportSceneGLB()
		{
			var scene = SceneManager.GetActiveScene();
			var gameObjects = scene.GetRootGameObjects();
			var transforms = Array.ConvertAll(gameObjects, gameObject => gameObject.transform);

			Export(transforms, null, true, scene.name);
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
