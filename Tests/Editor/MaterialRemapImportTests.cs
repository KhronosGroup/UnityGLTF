using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityGLTF;

/// <summary>
/// A glTF whose material is remapped to a material imported from another glTF (e.g. a material library GLB).
/// The remap target has to be imported first, so GLTFImporter.GatherDependenciesFromSourceFile reports it up front:
/// on a clean import the remap was silently dropped (#914), and when the library changed while Unity was closed,
/// the import failed with "Opening file VirtualArtifacts/Primary/...: file not found" (#919).
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
	public void MetaFile_ListsSameRemapTargetsAsImporter()
	{
		var importer = AssetImporter.GetAtPath(userPath);
		CollectionAssert.AreEqual(new[] { libraryPath }, ExternalObjectDependencies.FromImporter(importer));
		CollectionAssert.AreEqual(new[] { libraryPath }, ExternalObjectDependencies.FromMetaFile(userPath));
	}

	[Test]
	public void NotInMetaFile_ListsTargetsTheMetaFileScanMissed()
	{
		// the library's .meta has no remaps, like a .meta whose format the scan can't read anymore
		CollectionAssert.AreEqual(new[] { libraryPath }, ExternalObjectDependencies.NotInMetaFile(new List<string> { libraryPath }, libraryPath));
		CollectionAssert.IsEmpty(ExternalObjectDependencies.NotInMetaFile(ExternalObjectDependencies.FromImporter(AssetImporter.GetAtPath(userPath)), userPath));
	}

	/// <summary>
	/// The importer warns when the .meta scan misses a remap target. Neither a file with a remap nor one without may warn.
	/// </summary>
	[Test]
	public void Import_DoesNotWarnAboutMetaFile()
	{
		var warnings = new List<string>();
		Application.LogCallback onLog = (message, stackTrace, type) =>
		{
			if (message.Contains(ExternalObjectDependencies.NotInMetaFileWarning)) warnings.Add(message);
		};
		Application.logMessageReceived += onLog;
		try
		{
			AssetDatabase.ImportAsset(userPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			AssetDatabase.ImportAsset(libraryPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
		}
		finally
		{
			Application.logMessageReceived -= onLog;
		}
		CollectionAssert.IsEmpty(warnings);
	}

	/// <summary>
	/// The import order comes from GatherDependenciesFromSourceFile, which Unity calls before the import.
	/// The import tests below can't tell the order apart: without it, the DependsOnArtifact from OnImportAsset
	/// imports the user file a second time after the library and the end result looks the same.
	/// </summary>
	[Test]
	public void GatherDependencies_ContainsRemapTarget()
	{
		var gather = typeof(GLTFImporter).GetMethod("GatherDependenciesFromSourceFile", BindingFlags.Static | BindingFlags.NonPublic);
		Assert.IsNotNull(gather, "GLTFImporter.GatherDependenciesFromSourceFile not found");
		CollectionAssert.Contains((string[])gather.Invoke(null, new object[] { userPath }), libraryPath);
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

	[Test]
	public void RemapToBuiltinMaterial_ImportsWithoutDependency()
	{
		var builtin = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
		RemapUserMaterial(builtin);

		Assert.AreEqual(builtin, AssetDatabase.LoadAssetAtPath<GameObject>(userPath).GetComponentInChildren<Renderer>().sharedMaterial);
		CollectionAssert.IsEmpty(ExternalObjectDependencies.FromImporter(AssetImporter.GetAtPath(userPath)));
		CollectionAssert.IsEmpty(ExternalObjectDependencies.FromMetaFile(userPath));
	}

	private void RemapUserMaterial(Material target)
	{
		var importer = AssetImporter.GetAtPath(userPath);
		var source = new AssetImporter.SourceAssetIdentifier(LoadMaterial(userPath));
		importer.RemoveRemap(source);
		importer.AddRemap(source, target);
		importer.SaveAndReimport();
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

	private static byte[] TriangleGlb(string materialName, float[] baseColor)
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
