using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityGLTF;

/// <summary>
/// A material or texture remapped to another material or texture of the same file. An external object remap would store
/// a reference to the file's own sub-asset in the .meta file, which points into the previous import (#919), so these
/// remaps are stored by name (GLTFImporterRemaps.SetRemap) and resolved on import.
/// Any error logged during an import fails the test, e.g. "AssetImporter is referencing an asset from the previous import".
/// </summary>
public class RemapInFileTests
{
	private const string Folder = "Assets/__RemapInFileTests";
	private string path;

	[SetUp]
	public void Setup()
	{
		if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
		Directory.CreateDirectory(Folder);
		path = Folder + "/TwoMaterials.glb";
		File.WriteAllBytes(path, MaterialsGlb("MaterialA", "MaterialB"));
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
	}

	[TearDown]
	public void Teardown()
	{
		AssetDatabase.DeleteAsset(Folder);
	}

	[Test]
	public void MaterialRemappedInFile_UsesOtherMaterialOfTheFile()
	{
		SetRemap(Material("MaterialA"), Material("MaterialB"));

		AssertRendererMaterial("A", "MaterialB");
		AssertRendererMaterial("B", "MaterialB");
		AssertMetaFileHasNoSelfReference();
		CollectionAssert.IsEmpty(GLTFImporter.GetRemapTargetPaths(Importer));

		// resolved against the new sub-assets of every import
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
		AssertRendererMaterial("A", "MaterialB");
	}

	[Test]
	public void MaterialRemappedInFile_IsHiddenAndCanBeRestored()
	{
		SetRemap(Material("MaterialA"), Material("MaterialB"));
		Assert.IsTrue(Material("MaterialA").hideFlags.HasFlag(HideFlags.HideInHierarchy));
		Assert.AreEqual("MaterialB", Importer.RemapsInFile.GetTargetName(Material("MaterialA")));

		SetRemap(Material("MaterialA"), null);
		AssertRendererMaterial("A", "MaterialA");
		Assert.IsNull(Importer.RemapsInFile.GetTargetName(Material("MaterialA")));
		Assert.AreEqual(0, Importer.RemapsInFile.Count(typeof(Material)));
	}

	[Test]
	public void RemapToItself_RemovesRemap()
	{
		SetRemap(Material("MaterialA"), Material("MaterialB"));
		SetRemap(Material("MaterialA"), Material("MaterialA"));

		AssertRendererMaterial("A", "MaterialA");
		Assert.AreEqual(0, Importer.RemapsInFile.Count(typeof(Material)));
		CollectionAssert.IsEmpty(Importer.GetExternalObjectMap());
	}

	[Test]
	public void TextureRemappedInFile_UsesOtherTextureOfTheFile()
	{
		SetRemap(Texture("TextureA"), Texture("TextureB"));

		Assert.AreEqual(Texture("TextureB"), BaseColorTexture(Material("MaterialA")));
		Assert.AreEqual(Texture("TextureB"), BaseColorTexture(Material("MaterialB")));
		AssertMetaFileHasNoSelfReference();

		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
		Assert.AreEqual(Texture("TextureB"), BaseColorTexture(Material("MaterialA")));
	}

	/// <summary>
	/// A remap to another file's material still is an external object remap.
	/// </summary>
	[Test]
	public void RemapToOtherFile_IsExternalRemap()
	{
		var otherPath = Folder + "/Other.glb";
		File.WriteAllBytes(otherPath, MaterialRemapImportTests.TriangleGlb("OtherMaterial", new[] { 1f, 1f, 0f, 1f }));
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		var other = AssetDatabase.LoadAllAssetsAtPath(otherPath).OfType<Material>().Single();

		SetRemap(Material("MaterialA"), other);

		Assert.AreEqual(other, Renderer("A").sharedMaterial);
		Assert.AreEqual(0, Importer.RemapsInFile.Count(typeof(Material)));
		CollectionAssert.AreEqual(new[] { otherPath }, GLTFImporter.GetRemapTargetPaths(Importer));
	}

	/// <summary>
	/// An external object remap to the file's own material, as it could be made before remaps in the same file were
	/// stored by name. Unity always resolves it to null ("AssetImporter is referencing an asset from the previous
	/// import"), also outside of the import, so it never worked. It is reported as remap with a missing target
	/// (inspector warning) and can be removed, which also removes the reference to the own sub-asset from the .meta file.
	/// </summary>
	[Test]
	public void OldExternalRemapToOwnMaterial_IsReportedAndCanBeRemoved()
	{
		LogAssert.ignoreFailingMessages = true;
		var importer = Importer;
		importer.AddRemap(new AssetImporter.SourceAssetIdentifier(Material("MaterialA")), Material("MaterialB"));
		importer.SaveAndReimport();
		LogAssert.ignoreFailingMessages = false;

		importer = Importer;
		AssertRendererMaterial("A", "MaterialA");
		var missing = importer.GetRemapsWithMissingTarget(typeof(Material));
		CollectionAssert.AreEqual(new[] { "MaterialA" }, missing.Select(x => x.name));
		CollectionAssert.IsEmpty(importer.GetRemapsWithMissingTarget(typeof(Texture)));

		importer.RemoveAllRemaps(typeof(Material));
		importer.SaveAndReimport();
		CollectionAssert.IsEmpty(Importer.GetExternalObjectMap());
		AssertMetaFileHasNoSelfReference();
	}

	[Test]
	public void RemapsInFile_AreStoredByNameInMetaFile()
	{
		SetRemap(Material("MaterialA"), Material("MaterialB"));
		SetRemap(Texture("TextureB"), Texture("TextureA"));

		var meta = File.ReadAllText(path + ".meta");
		StringAssert.Contains("source: MaterialA", meta);
		StringAssert.Contains("target: MaterialB", meta);
		StringAssert.Contains("source: TextureB", meta);
		StringAssert.Contains("target: TextureA", meta);
		AssertMetaFileHasNoSelfReference();
	}

	[Test]
	public void MaterialRemapsInFile_CanSwapTwoMaterials()
	{
		SetRemap(Material("MaterialA"), Material("MaterialB"));
		SetRemap(Material("MaterialB"), Material("MaterialA"));

		AssertRendererMaterial("A", "MaterialB");
		AssertRendererMaterial("B", "MaterialA");
		Assert.AreEqual(2, Importer.RemapsInFile.Count(typeof(Material)));
	}

	/// <summary>
	/// Like external object remaps, a remap is one step: A → B and B → C doesn't remap A to C.
	/// </summary>
	[Test]
	public void MaterialRemapsInFile_AreOneStep()
	{
		WriteFile("MaterialA", "MaterialB", "MaterialC");
		SetRemap(Material("MaterialA"), Material("MaterialB"));
		SetRemap(Material("MaterialB"), Material("MaterialC"));

		AssertRendererMaterial("A", "MaterialB");
		AssertRendererMaterial("B", "MaterialC");
		AssertRendererMaterial("C", "MaterialC");
	}

	/// <summary>
	/// The remap target was renamed in the file, e.g. by a re-export: the remap is kept but not applied, without errors.
	/// </summary>
	[Test]
	public void RemapTargetRenamedInFile_UsesOwnMaterialUntilTheNameIsBack()
	{
		SetRemap(Material("MaterialA"), Material("MaterialB"));

		WriteFile("MaterialA", "MaterialC");
		AssertRendererMaterial("A", "MaterialA");
		Assert.IsFalse(Material("MaterialA").hideFlags.HasFlag(HideFlags.HideInHierarchy));
		Assert.AreEqual("MaterialB", Importer.RemapsInFile.GetTargetName(Material("MaterialA")));

		WriteFile("MaterialA", "MaterialB");
		AssertRendererMaterial("A", "MaterialB");
	}

	[Test]
	public void SwitchingBetweenRemapInFileAndExternalRemap_KeepsOneRemap()
	{
		var otherPath = Folder + "/Other.glb";
		File.WriteAllBytes(otherPath, MaterialRemapImportTests.TriangleGlb("OtherMaterial", new[] { 1f, 1f, 0f, 1f }));
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		var other = AssetDatabase.LoadAllAssetsAtPath(otherPath).OfType<Material>().Single();

		SetRemap(Material("MaterialA"), Material("MaterialB"));
		SetRemap(Material("MaterialA"), other);
		Assert.AreEqual(other, Renderer("A").sharedMaterial);
		Assert.AreEqual(0, Importer.RemapsInFile.Count(typeof(Material)));
		Assert.AreEqual(1, Importer.GetExternalObjectMap().Count);

		SetRemap(Material("MaterialA"), Material("MaterialB"));
		AssertRendererMaterial("A", "MaterialB");
		Assert.AreEqual(1, Importer.RemapsInFile.Count(typeof(Material)));
		CollectionAssert.IsEmpty(Importer.GetExternalObjectMap());
		CollectionAssert.IsEmpty(GLTFImporter.GetRemapTargetPaths(Importer));
	}

	[Test]
	public void MaterialAndTextureRemapInFile_Together()
	{
		SetRemap(Material("MaterialA"), Material("MaterialB"));
		SetRemap(Texture("TextureB"), Texture("TextureA"));

		AssertRendererMaterial("A", "MaterialB");
		AssertRendererMaterial("B", "MaterialB");
		Assert.AreEqual(Texture("TextureA"), BaseColorTexture(Material("MaterialB")));
		Assert.AreEqual(Texture("TextureA"), BaseColorTexture(Material("MaterialA")));
		Assert.IsTrue(Texture("TextureB").hideFlags.HasFlag(HideFlags.HideInHierarchy));
	}

	/// <summary>
	/// "Restore all" in the inspector: removes the remaps of one type, in the file and to other files.
	/// </summary>
	[Test]
	public void RemoveAllRemaps_RemovesOnlyRemapsOfTheType()
	{
		var otherPath = Folder + "/Other.glb";
		File.WriteAllBytes(otherPath, MaterialRemapImportTests.TriangleGlb("OtherMaterial", new[] { 1f, 1f, 0f, 1f }));
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		var other = AssetDatabase.LoadAllAssetsAtPath(otherPath).OfType<Material>().Single();

		SetRemap(Material("MaterialA"), Material("MaterialB"));
		SetRemap(Material("MaterialB"), other);
		SetRemap(Texture("TextureB"), Texture("TextureA"));

		var importer = Importer;
		importer.RemoveAllRemaps(typeof(Material));
		importer.SaveAndReimport();

		AssertRendererMaterial("A", "MaterialA");
		AssertRendererMaterial("B", "MaterialB");
		Assert.AreEqual(0, Importer.RemapsInFile.Count(typeof(Material)));
		CollectionAssert.IsEmpty(Importer.GetExternalObjectMap());
		Assert.AreEqual(1, Importer.RemapsInFile.Count(typeof(Texture)));
		Assert.AreEqual(Texture("TextureA"), BaseColorTexture(Material("MaterialB")));
	}

	[Test]
	public void RemapsInFile_WithMaterialImportDisabled_ImportWithoutErrors()
	{
		SetRemap(Material("MaterialA"), Material("MaterialB"));
		SetRemap(Texture("TextureB"), Texture("TextureA"));

		var importer = Importer;
		importer._importMaterials = false;
		EditorUtility.SetDirty(importer);
		importer.SaveAndReimport();

		Assert.IsTrue(AssetDatabase.LoadAssetAtPath<GameObject>(path), "did not import");
		CollectionAssert.IsEmpty(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>());
	}

	private void WriteFile(params string[] materialNames)
	{
		File.WriteAllBytes(path, MaterialsGlb(materialNames));
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
	}

	private GLTFImporter Importer => (GLTFImporter)AssetImporter.GetAtPath(path);

	private void SetRemap(UnityEngine.Object source, UnityEngine.Object target)
	{
		var importer = Importer;
		importer.SetRemap(source, target);
		importer.SaveAndReimport();
	}

	private Material Material(string name) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Single(x => x.name == name);

	private Texture2D Texture(string name) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Texture2D>().Single(x => x.name == name);

	private Renderer Renderer(string nodeName)
	{
		var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		Assert.IsTrue(root, $"{path} did not import");
		return root.GetComponentsInChildren<Renderer>(true).Single(r => r.name == nodeName);
	}

	private void AssertRendererMaterial(string nodeName, string materialName)
	{
		var material = Renderer(nodeName).sharedMaterial;
		Assert.IsTrue(material, $"renderer {nodeName} has no material");
		// the material of the current import, not one of a previous import
		Assert.AreEqual(Material(materialName), material, $"renderer {nodeName} uses '{material.name}'");
	}

	private static Texture BaseColorTexture(Material material)
	{
		var textures = material.GetTexturePropertyNameIDs().Select(material.GetTexture).Where(t => t).Distinct().ToList();
		Assert.AreEqual(1, textures.Count, $"textures of {material.name}");
		return textures[0];
	}

	private void AssertMetaFileHasNoSelfReference()
	{
		var guid = AssetDatabase.AssetPathToGUID(path);
		var meta = File.ReadAllText(path + ".meta");
		var occurrences = (meta.Length - meta.Replace(guid, "").Length) / guid.Length;
		Assert.AreEqual(1, occurrences, $"{path}.meta references the asset's own sub-assets:\n{meta}");
	}

	/// <summary>
	/// One triangle per material, named "A", "B", ... Each material uses its own embedded texture "TextureA",
	/// "TextureB", ... as base color.
	/// </summary>
	private static byte[] MaterialsGlb(params string[] materialNames)
	{
		var bin = new List<byte>();
		var bufferViews = new List<string>();

		int AddBufferView(byte[] data)
		{
			while (bin.Count % 4 != 0) bin.Add(0);
			bufferViews.Add($"{{\"buffer\":0,\"byteOffset\":{bin.Count},\"byteLength\":{data.Length}}}");
			bin.AddRange(data);
			return bufferViews.Count - 1;
		}

		var positions = new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 };
		var positionBytes = new byte[positions.Length * 4];
		Buffer.BlockCopy(positions, 0, positionBytes, 0, positionBytes.Length);
		var uvs = new float[] { 0, 0, 1, 0, 0, 1 };
		var uvBytes = new byte[uvs.Length * 4];
		Buffer.BlockCopy(uvs, 0, uvBytes, 0, uvBytes.Length);
		AddBufferView(positionBytes);
		AddBufferView(uvBytes);

		var nodes = new List<string>();
		var meshes = new List<string>();
		var materials = new List<string>();
		var textures = new List<string>();
		var images = new List<string>();
		for (var i = 0; i < materialNames.Length; i++)
		{
			var letter = (char)('A' + i);
			var imageView = AddBufferView(Png(Color.HSVToRGB(i / (float)materialNames.Length, 1, 1)));
			nodes.Add($"{{\"name\":\"{letter}\",\"mesh\":{i},\"translation\":[{i * 2},0,0]}}");
			meshes.Add($"{{\"primitives\":[{{\"attributes\":{{\"POSITION\":0,\"TEXCOORD_0\":1}},\"material\":{i}}}]}}");
			materials.Add($"{{\"name\":\"{materialNames[i]}\",\"pbrMetallicRoughness\":{{\"baseColorTexture\":{{\"index\":{i}}}}}}}");
			textures.Add($"{{\"source\":{i}}}");
			images.Add($"{{\"name\":\"Texture{letter}\",\"bufferView\":{imageView},\"mimeType\":\"image/png\"}}");
		}

		var json = new StringBuilder();
		json.Append("{\"asset\":{\"version\":\"2.0\"},\"scene\":0,");
		json.Append("\"scenes\":[{\"nodes\":[" + string.Join(",", Enumerable.Range(0, materialNames.Length)) + "]}],");
		json.Append("\"nodes\":[" + string.Join(",", nodes) + "],");
		json.Append("\"meshes\":[" + string.Join(",", meshes) + "],");
		json.Append("\"materials\":[" + string.Join(",", materials) + "],");
		json.Append("\"textures\":[" + string.Join(",", textures) + "],");
		json.Append("\"images\":[" + string.Join(",", images) + "],");
		json.Append("\"accessors\":[{\"bufferView\":0,\"componentType\":5126,\"count\":3,\"type\":\"VEC3\",\"min\":[0,0,0],\"max\":[1,1,0]},");
		json.Append("{\"bufferView\":1,\"componentType\":5126,\"count\":3,\"type\":\"VEC2\"}],");
		json.Append("\"bufferViews\":[" + string.Join(",", bufferViews) + "],");
		while (bin.Count % 4 != 0) bin.Add(0);
		json.Append($"\"buffers\":[{{\"byteLength\":{bin.Count}}}]}}");
		return GlbTestFile.Write(2, GlbTestFile.Json(json.ToString()), GlbTestFile.Bin(bin.ToArray()));
	}

	private static byte[] Png(Color color)
	{
		var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
		try
		{
			texture.SetPixels(Enumerable.Repeat(color, 4).ToArray());
			texture.Apply();
			return texture.EncodeToPNG();
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(texture);
		}
	}
}
