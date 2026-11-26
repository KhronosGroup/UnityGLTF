using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityGLTF;

/// <summary>
/// A glTF whose material is remapped to a material imported from another glTF (e.g. a material library GLB).
/// Unity doesn't import the remap target first, so the user file can be imported before it and is imported again
/// after it (DependsOnArtifact): on a clean import the remap was silently dropped (#914), and when the library changed
/// while Unity was closed, the import failed with "Opening file VirtualArtifacts/Primary/...: file not found" (#919).
/// Any error logged during an import fails the test.
/// </summary>
public class MaterialRemapImportTests
{
	private const string Folder = "Assets/__MaterialRemapImportTests";

	// the user file sorts before the library, so it is not imported after it by chance
	private const string UserFile = "A_User.glb";
	private const string LibraryFile = "B_Library.glb";
	private const string LibraryMaterial = "LibraryMaterial";

	private string userPath, libraryPath;

	[SetUp]
	public void Setup()
	{
		if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
		Directory.CreateDirectory(Folder);
		userPath = Folder + "/" + UserFile;
		libraryPath = Folder + "/" + LibraryFile;
		File.WriteAllBytes(userPath, TriangleGlb("UserMaterial", new[] { 0f, 0f, 1f, 1f }));
		File.WriteAllBytes(libraryPath, TriangleGlb(LibraryMaterial, new[] { 1f, 0f, 0f, 1f }));
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

		RemapUserMaterial(LoadMaterial(libraryPath));
		AssertUsesLibraryMaterial(userPath, libraryPath);
	}

	[TearDown]
	public void Teardown()
	{
		AssetDatabase.DeleteAsset(Folder);
	}

	[Test]
	public void RemapTargetPaths_ContainLibrary()
	{
		CollectionAssert.AreEqual(new[] { libraryPath }, GLTFImporter.GetRemapTargetPaths(AssetImporter.GetAtPath(userPath)));
		CollectionAssert.IsEmpty(GLTFImporter.GetRemapTargetPaths(AssetImporter.GetAtPath(libraryPath)));
	}

	/// <summary>
	/// #914: both files are new in the same refresh, as with a deleted Library folder.
	/// </summary>
	[Test]
	public void FreshCopiesImportedInOneRefresh_KeepRemappedMaterial()
	{
		var copyFolder = Folder + "/Copy";
		Directory.CreateDirectory(copyFolder);
		var userGuid = AssetDatabase.AssetPathToGUID(userPath);
		var libraryGuid = AssetDatabase.AssetPathToGUID(libraryPath);
		// the user copy also sorts first by GUID
		var newUserGuid = "0" + GUID.Generate().ToString().Substring(1);
		var newLibraryGuid = "f" + GUID.Generate().ToString().Substring(1);
		string RewriteGuids(string meta) => meta.Replace(userGuid, newUserGuid).Replace(libraryGuid, newLibraryGuid);

		var userCopy = copyFolder + "/" + UserFile;
		var libraryCopy = copyFolder + "/" + LibraryFile;
		File.Copy(userPath, userCopy);
		File.WriteAllText(userCopy + ".meta", RewriteGuids(File.ReadAllText(userPath + ".meta")));
		File.Copy(libraryPath, libraryCopy);
		File.WriteAllText(libraryCopy + ".meta", RewriteGuids(File.ReadAllText(libraryPath + ".meta")));

		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

		Assert.AreEqual(userCopy, AssetDatabase.GUIDToAssetPath(newUserGuid));
		Assert.AreEqual(libraryCopy, AssetDatabase.GUIDToAssetPath(newLibraryGuid));
		AssertUsesLibraryMaterial(userCopy, libraryCopy);
	}

	[Test]
	public void ChangedLibrary_Refresh_KeepsRemappedMaterial()
	{
		ChangeLibraryOnDisk();
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		AssertUsesLibraryMaterial(userPath, libraryPath);
	}

	/// <summary>
	/// #919: like an editor start after the library changed. Nothing is loaded, the library is out of date
	/// and the user file is imported first.
	/// </summary>
	[Test]
	public void ChangedLibrary_NothingLoaded_ImportUserFirst_KeepsRemappedMaterial()
	{
		ChangeLibraryOnDisk();
		Selection.activeObject = null;
		GC.Collect();
		GC.WaitForPendingFinalizers();
		EditorUtility.UnloadUnusedAssetsImmediate(true);

		AssetDatabase.ImportAsset(userPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		AssertUsesLibraryMaterial(userPath, libraryPath);
	}

	/// <summary>
	/// #919: import results such as the imported materials belong to the import result (GLTFImportInfo), not to the
	/// import settings. References to the asset's own sub-assets in the .meta file made Unity load the artifact
	/// while it was being replaced.
	/// </summary>
	[Test]
	public void MetaFiles_DontReferenceOwnSubAssets()
	{
		// writes the import settings of the library to its .meta file (the user file's were written by the remap)
		var libraryImporter = AssetImporter.GetAtPath(libraryPath);
		EditorUtility.SetDirty(libraryImporter);
		libraryImporter.SaveAndReimport();

		foreach (var path in new[] { userPath, libraryPath })
		{
			var guid = AssetDatabase.AssetPathToGUID(path);
			var meta = File.ReadAllText(path + ".meta");
			// the only occurrence is the "guid:" line of the asset itself
			var occurrences = (meta.Length - meta.Replace(guid, "").Length) / guid.Length;
			Assert.AreEqual(1, occurrences, $"{path}.meta references the asset's own sub-assets:\n{meta}");
		}
	}

	[Test]
	public void RemapToBuiltinMaterial_ImportsWithoutDependency()
	{
		var builtin = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
		RemapUserMaterial(builtin);

		Assert.AreEqual(builtin, AssetDatabase.LoadAssetAtPath<GameObject>(userPath).GetComponentInChildren<Renderer>().sharedMaterial);
		CollectionAssert.IsEmpty(GLTFImporter.GetRemapTargetPaths(AssetImporter.GetAtPath(userPath)));
	}

	/// <summary>
	/// A remap to a native material asset (.mat) has no import artifact to depend on. Declaring an artifact dependency
	/// made Unity log "dependency isn't used and therefore not registered" on every import. Changing the material
	/// still has to import the file again.
	/// </summary>
	[Test]
	public void RemapToMaterialAsset_NoDependencyWarning_ReimportsWhenMaterialChanges()
	{
		var materialPath = Folder + "/Remapped.mat";
		var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
		AssetDatabase.CreateAsset(new Material(shader) { color = Color.red }, materialPath);

		AssertRemapTargetDependency(materialPath,
			() => RemapUserMaterial(AssetDatabase.LoadAssetAtPath<Material>(materialPath)),
			() =>
			{
				var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
				material.color = Color.green;
				EditorUtility.SetDirty(material);
				AssetDatabase.SaveAssets();
			});
		AssertUsesMaterialOf(userPath, materialPath);
	}

	/// <summary>
	/// A remap to a texture is imported by the texture importer and has an import artifact, so the file depends on
	/// that artifact. Changing the texture has to import the file again, without Unity's dependency warning.
	/// </summary>
	[Test]
	public void RemapToTextureAsset_NoDependencyWarning_ReimportsWhenTextureChanges()
	{
		var texturePath = Folder + "/Remapped.png";
		WriteTexture(texturePath, Color.red);
		AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
		var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
		Assert.IsTrue(texture, "texture did not import");

		AssertRemapTargetDependency(texturePath,
			() =>
			{
				// by name: the file doesn't need to contain a texture to have one remapped
				var importer = AssetImporter.GetAtPath(userPath);
				importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Texture), "RemappedTexture"), texture);
				importer.SaveAndReimport();
			},
			() =>
			{
				WriteTexture(texturePath, Color.green);
				AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
			});
	}

	private static void WriteTexture(string path, Color color)
	{
		var texture = new Texture2D(4, 4);
		texture.SetPixels(Enumerable.Repeat(color, 16).ToArray());
		File.WriteAllBytes(path, texture.EncodeToPNG());
		UnityEngine.Object.DestroyImmediate(texture);
	}

	/// <summary>
	/// Remaps the user file to the asset at <paramref name="targetPath"/> and checks that no "dependency isn't used"
	/// warning is logged and that changing the target imports the user file again.
	/// </summary>
	private void AssertRemapTargetDependency(string targetPath, Action remap, Action changeTarget)
	{
		var warnings = new List<string>();
		var userImports = 0;
		void OnLog(string message, string stackTrace, LogType type)
		{
			if (message.IndexOf("dependency isn't used", StringComparison.Ordinal) >= 0) warnings.Add(message);
		}
		void OnImported(string path)
		{
			if (path == userPath) userImports++;
		}

		Application.logMessageReceived += OnLog;
		ImportCountingPostprocessor.Imported += OnImported;
		LogAssert.ignoreFailingMessages = true;
		try
		{
			remap();
			AssetDatabase.ImportAsset(userPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			CollectionAssert.Contains(GLTFImporter.GetRemapTargetPaths(AssetImporter.GetAtPath(userPath)), targetPath);

			userImports = 0;
			changeTarget();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			Assert.Greater(userImports, 0, $"the file was not imported again after {targetPath} changed");
		}
		finally
		{
			Application.logMessageReceived -= OnLog;
			ImportCountingPostprocessor.Imported -= OnImported;
			LogAssert.ignoreFailingMessages = false;
		}

		CollectionAssert.IsEmpty(warnings);
	}

	/// <summary>
	/// The user file uses the library's material and the library uses the user file's material, so both files
	/// depend on each other (DependsOnArtifact). The imports have to settle instead of reimporting each other forever.
	/// Unity logs "Circular import dependencies detected!" for these, which can't be avoided without dropping
	/// a dependency (and then a remap can get lost on a clean import, #914).
	/// </summary>
	[Test]
	public void CyclicRemaps_TwoFiles_ImportAndSettle()
	{
		using (var imports = new ImportCounter())
		{
			RemapMaterial(libraryPath, LoadMaterial(userPath));
			AssertUsesMaterialOf(userPath, libraryPath);
			AssertUsesMaterialOf(libraryPath, userPath);
			imports.AssertSettled();

			ChangeLibraryOnDisk();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			AssertUsesMaterialOf(userPath, libraryPath);
			AssertUsesMaterialOf(libraryPath, userPath);
			imports.AssertSettled();
		}
	}

	/// <summary>
	/// User file → library → third file → user file.
	/// </summary>
	[Test]
	public void CyclicRemaps_ThreeFiles_ImportAndSettle()
	{
		var thirdPath = Folder + "/C_Third.glb";
		File.WriteAllBytes(thirdPath, TriangleGlb("ThirdMaterial", new[] { 0f, 1f, 0f, 1f }));
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

		using (var imports = new ImportCounter())
		{
			RemapMaterial(libraryPath, LoadMaterial(thirdPath));
			RemapMaterial(thirdPath, LoadMaterial(userPath));
			AssertUsesMaterialOf(userPath, libraryPath);
			AssertUsesMaterialOf(libraryPath, thirdPath);
			AssertUsesMaterialOf(thirdPath, userPath);
			imports.AssertSettled();

			ChangeLibraryOnDisk();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			AssertUsesMaterialOf(userPath, libraryPath);
			AssertUsesMaterialOf(libraryPath, thirdPath);
			AssertUsesMaterialOf(thirdPath, userPath);
			imports.AssertSettled();
		}
	}

	/// <summary>
	/// Both files of a cycle are new in the same refresh, as with a deleted Library folder (#914).
	/// </summary>
	[Test]
	public void CyclicRemaps_FreshCopies_ImportedInOneRefresh_ImportAndSettle()
	{
		using (new ImportCounter())
			RemapMaterial(libraryPath, LoadMaterial(userPath));

		var copyFolder = Folder + "/FreshCopy";
		Directory.CreateDirectory(copyFolder);
		var userGuid = AssetDatabase.AssetPathToGUID(userPath);
		var libraryGuid = AssetDatabase.AssetPathToGUID(libraryPath);
		var newUserGuid = GUID.Generate().ToString();
		var newLibraryGuid = GUID.Generate().ToString();
		string RewriteGuids(string meta) => meta.Replace(userGuid, newUserGuid).Replace(libraryGuid, newLibraryGuid);
		var copyUser = copyFolder + "/" + UserFile;
		var copyLibrary = copyFolder + "/" + LibraryFile;
		foreach (var (from, to) in new[] { (userPath, copyUser), (libraryPath, copyLibrary) })
		{
			File.Copy(from, to);
			File.WriteAllText(to + ".meta", RewriteGuids(File.ReadAllText(from + ".meta")));
		}

		using (var imports = new ImportCounter())
		{
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			Assert.AreEqual(copyUser, AssetDatabase.GUIDToAssetPath(newUserGuid));
			Assert.AreEqual(copyLibrary, AssetDatabase.GUIDToAssetPath(newLibraryGuid));
			AssertUsesMaterialOf(copyUser, copyLibrary);
			AssertUsesMaterialOf(copyLibrary, copyUser);
			imports.AssertSettled();
		}
	}

	/// <summary>
	/// Counts the imports of the test files. Fails on Unity's warning about an import loop and on any error
	/// except Unity's error about the circular dependencies.
	/// </summary>
	private sealed class ImportCounter : IDisposable
	{
		// a file in a cycle is imported again after each file it depends on, but not endlessly
		private const int MaxImportsPerFile = 5;

		private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
		private const string CircularDependencyError = "Circular import dependencies detected!";

		private readonly List<string> unexpectedMessages = new List<string>();

		public ImportCounter()
		{
			ImportCountingPostprocessor.Imported += OnImported;
			Application.logMessageReceived += OnLog;
			// the errors are checked in AssertSettled
			LogAssert.ignoreFailingMessages = true;
		}

		private void OnImported(string path)
		{
			if (!path.StartsWith(Folder + "/", StringComparison.Ordinal)) return;
			counts.TryGetValue(path, out var count);
			counts[path] = count + 1;
		}

		private void OnLog(string message, string stackTrace, LogType type)
		{
			var isError = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
			// Unity logs the circular dependency error and the details as two messages
			var isCircularDependencyError = message.StartsWith(CircularDependencyError, StringComparison.Ordinal) ||
			                                message.StartsWith("Refresh queue details:", StringComparison.Ordinal);
			if (isError && !isCircularDependencyError)
				unexpectedMessages.Add(message);
			else if (message.IndexOf("import loop", StringComparison.OrdinalIgnoreCase) >= 0)
				unexpectedMessages.Add(message);
		}

		public void AssertSettled()
		{
			var summary = string.Join(", ", counts.Select(c => $"{Path.GetFileName(c.Key)}: {c.Value}"));
			CollectionAssert.IsEmpty(unexpectedMessages, "Imports: " + summary);
			foreach (var count in counts)
				Assert.LessOrEqual(count.Value, MaxImportsPerFile, $"{count.Key} was imported too often. Imports: {summary}");
			counts.Clear();
		}

		public void Dispose()
		{
			ImportCountingPostprocessor.Imported -= OnImported;
			Application.logMessageReceived -= OnLog;
			LogAssert.ignoreFailingMessages = false;
			CollectionAssert.IsEmpty(unexpectedMessages);
		}
	}

	private class ImportCountingPostprocessor : AssetPostprocessor
	{
		public static event Action<string> Imported;

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			if (Imported == null) return;
			foreach (var path in importedAssets) Imported(path);
		}
	}

	private void RemapUserMaterial(Material target) => RemapMaterial(userPath, target);

	private static void RemapMaterial(string path, Material target)
	{
		var importer = (GLTFImporter)AssetImporter.GetAtPath(path);
		importer.SetRemap(LoadMaterial(path), target);
		importer.SaveAndReimport();
	}

	private static void AssertUsesMaterialOf(string path, string materialPath)
	{
		var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		Assert.IsTrue(asset, $"{path} did not import");
		var material = asset.GetComponentInChildren<Renderer>().sharedMaterial;
		Assert.IsTrue(material, $"renderer of {path} has no material");
		Assert.AreEqual(materialPath, AssetDatabase.GetAssetPath(material), $"material '{material.name}' of {path} is not remapped to {materialPath}");
	}

	/// <summary>
	/// Rewrites the library with a different color, like a re-export. The material keeps its name, so the remap still points to it.
	/// </summary>
	private void ChangeLibraryOnDisk()
	{
		File.WriteAllBytes(libraryPath, TriangleGlb(LibraryMaterial, new[] { 0f, 1f, 0f, 1f }));
	}

	private static Material LoadMaterial(string path)
	{
		// a remapped material stays in the file (hidden), so this is always the file's own material
		var materials = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToList();
		Assert.AreEqual(1, materials.Count, $"materials in {path}");
		return materials[0];
	}

	private static void AssertUsesLibraryMaterial(string userPath, string libraryPath)
	{
		var user = AssetDatabase.LoadAssetAtPath<GameObject>(userPath);
		Assert.IsTrue(user, $"{userPath} did not import");
		var material = user.GetComponentInChildren<Renderer>().sharedMaterial;
		Assert.IsTrue(material, "renderer has no material");
		Assert.AreEqual(libraryPath, AssetDatabase.GetAssetPath(material), $"material '{material.name}' is not remapped to the library");
		Assert.AreEqual(LibraryMaterial, material.name);
	}

	internal static byte[] TriangleGlb(string materialName, float[] baseColor)
	{
		var floats = new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 };
		var bin = new byte[floats.Length * 4];
		Buffer.BlockCopy(floats, 0, bin, 0, bin.Length);
		var color = string.Join(",", baseColor.Select(c => c.ToString(System.Globalization.CultureInfo.InvariantCulture)));
		var json = "{\"asset\":{\"version\":\"2.0\"},\"scene\":0,\"scenes\":[{\"nodes\":[0]}]," +
		           "\"nodes\":[{\"name\":\"Triangle\",\"mesh\":0}]," +
		           "\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0},\"material\":0}]}]," +
		           $"\"materials\":[{{\"name\":\"{materialName}\",\"pbrMetallicRoughness\":{{\"baseColorFactor\":[{color}]}}}}]," +
		           "\"accessors\":[{\"bufferView\":0,\"componentType\":5126,\"count\":3,\"type\":\"VEC3\",\"min\":[0,0,0],\"max\":[1,1,0]}]," +
		           $"\"bufferViews\":[{{\"buffer\":0,\"byteLength\":{bin.Length}}}]," +
		           $"\"buffers\":[{{\"byteLength\":{bin.Length}}}]}}";
		return GlbTestFile.Write(2, GlbTestFile.Json(json), GlbTestFile.Bin(bin));
	}
}
