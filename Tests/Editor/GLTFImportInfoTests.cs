using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityGLTF;

/// <summary>
/// The import results of a GLTFImporter (imported materials and textures, extensions, ...) are stored in a hidden
/// GLTFImportInfo sub-asset, not in the importer, whose fields end up in the .meta file (#919).
/// </summary>
public class GLTFImportInfoTests
{
	private const string Folder = "Assets/__GLTFImportInfoTests";
	private string path;

	[SetUp]
	public void Setup()
	{
		if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
		Directory.CreateDirectory(Folder);
		path = Folder + "/Triangle.glb";
		File.WriteAllBytes(path, MaterialRemapImportTests.TriangleGlb("TriangleMaterial", new[] { 1f, 1f, 1f, 1f }));
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
	}

	[TearDown]
	public void Teardown()
	{
		AssetDatabase.DeleteAsset(Folder);
	}

	[Test]
	public void Import_CreatesImportInfo()
	{
		var info = GLTFImportInfo.Load(path);
		Assert.IsTrue(info, "no import info");

		var material = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Single();
		CollectionAssert.AreEqual(new[] { material }, info.materials);
		CollectionAssert.IsEmpty(info.textures);
		Assert.IsTrue(info.hasSceneData);
		Assert.IsTrue(info.hasMaterialData);
		Assert.IsFalse(info.hasTextureData);
		Assert.IsFalse(info.hasAnimationData);
		StringAssert.StartsWith("scenes/", info.mainAssetIdentifier);
	}

	/// <summary>
	/// Each import creates a new artifact, so there is exactly one import info, also after reimports. A sub-asset
	/// named like the import info's identifier gets a unique identifier instead of replacing it.
	/// </summary>
	[Test]
	public void ImportInfo_ExistsOncePerAsset()
	{
		File.WriteAllBytes(path, MaterialRemapImportTests.TriangleGlb(GLTFImportInfo.Identifier, new[] { 1f, 1f, 1f, 1f }));
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

		var all = AssetDatabase.LoadAllAssetsAtPath(path);
		Assert.AreEqual(1, all.OfType<GLTFImportInfo>().Count());
		var material = all.OfType<Material>().Single();
		Assert.AreEqual(GLTFImportInfo.Identifier, material.name);
		CollectionAssert.AreEqual(new[] { material }, GLTFImportInfo.Load(path).materials);
	}

	[Test]
	public void ImportInfo_IsHiddenAndNotInBuilds()
	{
		var info = GLTFImportInfo.Load(path);
		Assert.IsTrue(info, "no import info");
		Assert.IsTrue(info.hideFlags.HasFlag(HideFlags.HideInHierarchy));
		Assert.IsTrue(info.hideFlags.HasFlag(HideFlags.DontSaveInBuild));
		CollectionAssert.DoesNotContain(AssetDatabase.LoadAllAssetRepresentationsAtPath(path), info);
		Assert.IsInstanceOf<GameObject>(AssetDatabase.LoadMainAssetAtPath(path));
	}

	/// <summary>
	/// Unity discards the import result of a failed import, so the inspector reads the extensions from the file.
	/// </summary>
	[Test]
	public void FailedImport_HasNoImportInfo_ExtensionsAreReadFromFile()
	{
		var brokenPath = Folder + "/Broken.gltf";
		// the accessor points to a buffer view that doesn't exist
		File.WriteAllText(brokenPath, "{\"asset\":{\"version\":\"2.0\"},\"extensionsUsed\":[\"KHR_materials_unlit\"],\"extensionsRequired\":[\"KHR_materials_unlit\"]," +
		                              "\"scene\":0,\"scenes\":[{\"nodes\":[0]}],\"nodes\":[{\"mesh\":0}],\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0}}]}]," +
		                              "\"accessors\":[{\"bufferView\":5,\"componentType\":5126,\"count\":3,\"type\":\"VEC3\"}]}");
		UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
		AssetDatabase.ImportAsset(brokenPath, ImportAssetOptions.ForceSynchronousImport);

		Assert.IsFalse(GLTFImportInfo.Load(brokenPath), "a failed import has an import info");
		var extensions = GLTFImporter.ReadExtensions(brokenPath);
		Assert.AreEqual(1, extensions.Count);
		Assert.AreEqual("KHR_materials_unlit", extensions[0].name);
		Assert.IsTrue(extensions[0].required);
	}

	[Test]
	public void ImporterSettings_HaveNoImportResults()
	{
		using (var serializedImporter = new SerializedObject(AssetImporter.GetAtPath(path)))
		{
			foreach (var name in new[] { "m_Materials", "m_Textures", "_textures", "_extensions", "_gltfAsset", "_mainAssetIdentifier", "_deduplicatedStatistics", "m_HasSceneData" })
				Assert.IsNull(serializedImporter.FindProperty(name), $"the importer still has {name}");
		}
	}
}
