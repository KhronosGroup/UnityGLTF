using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Loader;
using Object = UnityEngine.Object;

/// <summary>
/// glTF URIs that spell a file name in another Unicode normalization form than the file on disk.
/// glTF files written on macOS often contain decomposed (NFD) names, while the file itself is precomposed (NFC),
/// e.g. after a git checkout on Windows. Windows (NTFS) does not normalize file names, so the exact spelling has to be found.
/// Per the glTF spec such URIs are valid but not normalized ("may be unsupported"), so this is optional support;
/// spec-conforming URIs have to resolve exactly as before (see URIHelper.GetUriPathVariants).
///
/// Every non-ASCII string in this file is written with escapes and its forms are derived with string.Normalize,
/// so an editor or git normalizing this source file can't turn the tests into comparisons of identical strings.
/// </summary>
public class UnicodeUriPathTests
{
	// Names with characters that change between NFC and NFD
	private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
	{
		{ "Korean", "buffer\uC548\uB155\uD558\uC138\uC694, \uC138\uACC4" }, // Hangul syllables decompose into jamo
		{ "Latin", "Caf\u00E9 cr\u00E8me" }, // one combining mark per letter
		{ "Vietnamese", "Xin ch\u00E0o, th\u1EBF gi\u1EDBi" }, // two combining marks per letter, their order matters
		{ "Japanese", "\u30AC\u30E9\u30B9 \u3053\u3093\u306B\u3061\u306F" }, // katakana with dakuten
	};

	private static string Nfc(string s) => s.Normalize(NormalizationForm.FormC);
	private static string Nfd(string s) => s.Normalize(NormalizationForm.FormD);

	/// <summary>Percent-encodes each path segment, as exporters write URIs.</summary>
	private static string Escape(string path) => string.Join("/", path.Split('/').Select(Uri.EscapeDataString));

	private static string Name(string label)
	{
		var name = Names[label];
		Assert.AreNotEqual(Nfc(name), Nfd(name), $"test data: \"{label}\" has to differ between NFC and NFD");
		Assert.AreEqual(Nfc(name), name, $"test data: \"{label}\" has to be written in NFC");
		return name;
	}

	private string tempDirectory;

	[SetUp]
	public void Setup()
	{
		tempDirectory = Path.Combine(Path.GetTempPath(), "UnityGLTF_UnicodeUriPathTests_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDirectory);
	}

	[TearDown]
	public void Teardown()
	{
		if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
		if (AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.DeleteAsset(AssetFolder);
	}

	private string WriteFile(string relativePath, string content) => WriteFile(tempDirectory, relativePath, Encoding.UTF8.GetBytes(content));

	private static string WriteFile(string directory, string relativePath, byte[] content)
	{
		var path = Path.Combine(directory, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		File.WriteAllBytes(path, content);
		return path;
	}

	/// <summary>
	/// True if the file system stores NFC and NFD spellings as different files (Windows/NTFS, Linux/ext4),
	/// false if it treats them as the same name (macOS/APFS, HFS+).
	/// </summary>
	private static bool FileSystemKeepsNormalizationFormsApart(string directory)
	{
		var probe = Path.Combine(directory, Nfc("probe \u00E9"));
		File.WriteAllText(probe, "");
		try { return !File.Exists(Path.Combine(directory, Nfd("probe \u00E9"))); }
		finally { File.Delete(probe); }
	}

	private void AssumeFileSystemKeepsNormalizationFormsApart() =>
		Assume.That(FileSystemKeepsNormalizationFormsApart(tempDirectory), "the file system treats NFC and NFD spellings as the same file");

	/// <summary>
	/// Makes sure the tests below exercise the bug on Windows instead of passing because the file system matches both spellings.
	/// </summary>
	[Test]
	public void Windows_FileSystemKeepsNormalizationFormsApart()
	{
		Assume.That(Application.platform == RuntimePlatform.WindowsEditor, "Windows only");
		Assert.IsTrue(FileSystemKeepsNormalizationFormsApart(tempDirectory));
	}

	#region URI variants

	/// <summary>
	/// Plain URIs must not cause additional file system lookups.
	/// </summary>
	[TestCase("buffer.bin")]
	[TestCase("textures/BaseColor.png")]
	[TestCase("../Parent Folder/a b.png")]
	[TestCase("C:/absolute/path.bin")]
	public void AsciiUri_HasNoOtherVariants(string uri)
	{
		CollectionAssert.AreEqual(new[] { uri }, URIHelper.GetUriPathVariants(uri));
	}

	[TestCase("Korean")]
	[TestCase("Latin")]
	[TestCase("Vietnamese")]
	[TestCase("Japanese")]
	public void UriAsGiven_IsTriedFirst(string label)
	{
		var name = Name(label);
		foreach (var uri in new[] { Nfc(name), Nfd(name), Escape(Nfc(name)), Escape(Nfd(name)) })
			Assert.AreEqual(uri, URIHelper.GetUriPathVariants(uri)[0], uri);
	}

	[TestCase("Korean")]
	[TestCase("Latin")]
	[TestCase("Vietnamese")]
	[TestCase("Japanese")]
	public void NfdUri_HasNfcVariant(string label)
	{
		var name = Name(label);
		CollectionAssert.AreEqual(new[] { Nfd(name), Nfc(name) }, URIHelper.GetUriPathVariants(Nfd(name)));
	}

	[TestCase("Korean")]
	[TestCase("Latin")]
	[TestCase("Vietnamese")]
	[TestCase("Japanese")]
	public void NfcUri_HasNfdVariant(string label)
	{
		var name = Name(label);
		CollectionAssert.AreEqual(new[] { Nfc(name), Nfd(name) }, URIHelper.GetUriPathVariants(Nfc(name)));
	}

	/// <summary>
	/// Exact spellings come before normalized ones: the URI as given, then unescaped, then the normalized forms.
	/// </summary>
	[TestCase("Korean")]
	[TestCase("Vietnamese")]
	public void EscapedNfdUri_HasUnescapedVariantsInOrder(string label)
	{
		var name = Name(label);
		var uri = Escape(Nfd(name));
		CollectionAssert.AreEqual(new[] { uri, Nfd(name), Nfc(name) }, URIHelper.GetUriPathVariants(uri));
	}

	[TestCase("Korean")]
	[TestCase("Latin")]
	public void Variants_AreDistinct(string label)
	{
		var name = Name(label);
		foreach (var uri in new[] { Nfc(name), Nfd(name), Escape(Nfc(name)), Escape(Nfd(name)), "plain.bin" })
		{
			var variants = URIHelper.GetUriPathVariants(uri);
			CollectionAssert.AllItemsAreUnique(variants, uri);
		}
	}

	/// <summary>
	/// Only canonical forms (NFC/NFD) may be tried. Compatibility forms (NFKC/NFKD) would map to other, existing file names,
	/// e.g. the fullwidth comma to "," or the "fi" ligature to "fi".
	/// </summary>
	[TestCase("\u4F60\u597D\uFF0C\u4E16\u754C.jpg")] // fullwidth comma
	[TestCase("\uFB01le.bin")] // "fi" ligature
	[TestCase("\u2460.png")] // circled digit one
	[TestCase("x\u00B2.png")] // superscript two
	public void CompatibilityCharacters_AreNotReplaced(string uri)
	{
		CollectionAssert.AreEqual(new[] { uri }, URIHelper.GetUriPathVariants(uri));
	}

	/// <summary>
	/// '+' is a literal character in URI paths, and '%' without two hex digits is not an escape.
	/// </summary>
	[TestCase("a+b.bin")]
	[TestCase("100%.bin")]
	[TestCase("a%zz.bin")]
	[TestCase("a%2.bin")]
	public void NonEscapes_AreKeptLiterally(string uri)
	{
		CollectionAssert.AreEqual(new[] { uri }, URIHelper.GetUriPathVariants(uri));
	}

	/// <summary>
	/// Invalid UTF-16 (a lone surrogate) or an escaped invalid UTF-8 sequence can't be normalized, but must not fail the import.
	/// </summary>
	[TestCase("a\uD800b.bin")]
	[TestCase("a%ED%A0%80b.bin")]
	[TestCase("a%FF.bin")]
	public void InvalidUnicode_DoesNotThrow(string uri)
	{
		List<string> variants = null;
		Assert.DoesNotThrow(() => variants = URIHelper.GetUriPathVariants(uri));
		Assert.AreEqual(uri, variants[0]);
	}

	#endregion

	#region Finding files

	[TestCase("Korean")]
	[TestCase("Latin")]
	[TestCase("Vietnamese")]
	[TestCase("Japanese")]
	public void NfdUri_FindsNfcFile(string label)
	{
		var name = Name(label);
		var onDisk = WriteFile(Nfc(name) + ".bin", "content");

		var found = URIHelper.FindExistingFile(tempDirectory, Nfd(name) + ".bin", Path.Combine);

		Assert.IsNotNull(found);
		Assert.AreEqual("content", File.ReadAllText(found));
		// The spelling on disk is returned, the AssetDatabase only knows that one
		if (FileSystemKeepsNormalizationFormsApart(tempDirectory)) Assert.AreEqual(onDisk, found);
	}

	[TestCase("Korean")]
	[TestCase("Latin")]
	[TestCase("Vietnamese")]
	[TestCase("Japanese")]
	public void NfcUri_FindsNfdFile(string label)
	{
		var name = Name(label);
		var onDisk = WriteFile(Nfd(name) + ".bin", "content");

		var found = URIHelper.FindExistingFile(tempDirectory, Nfc(name) + ".bin", Path.Combine);

		Assert.IsNotNull(found);
		Assert.AreEqual("content", File.ReadAllText(found));
		if (FileSystemKeepsNormalizationFormsApart(tempDirectory)) Assert.AreEqual(onDisk, found);
	}

	[TestCase("Korean")]
	[TestCase("Vietnamese")]
	public void EscapedNfdUri_FindsNfcFile(string label)
	{
		var name = Name(label);
		var onDisk = WriteFile(Nfc(name) + ".png", "content");

		var found = URIHelper.FindExistingFile(tempDirectory, Escape(Nfd(name) + ".png"), Path.Combine);

		Assert.IsNotNull(found);
		Assert.AreEqual("content", File.ReadAllText(found));
		if (FileSystemKeepsNormalizationFormsApart(tempDirectory)) Assert.AreEqual(onDisk, found);
	}

	/// <summary>
	/// Folder names are normalized too, and relative paths are resolved by FileLoader.CombinePaths.
	/// </summary>
	[TestCase("Korean")]
	[TestCase("Japanese")]
	public void NfdUriWithFolders_FindsNfcFolderAndFile(string label)
	{
		var name = Name(label);
		WriteFile(Nfc(name) + "/textures/" + Nfc(name) + ".png", "child");
		WriteFile(Nfc(name) + ".png", "parent");
		var gltfDirectory = Path.Combine(tempDirectory, "glTF").Replace("\\", "/");
		Directory.CreateDirectory(gltfDirectory);

		var child = URIHelper.FindExistingFile(tempDirectory, Escape(Nfd(name) + "/textures/" + Nfd(name) + ".png"), FileLoader.CombinePaths);
		var parent = URIHelper.FindExistingFile(gltfDirectory, "../" + Escape(Nfd(name) + ".png"), FileLoader.CombinePaths);

		Assert.IsNotNull(child, "file in a child folder");
		Assert.AreEqual("child", File.ReadAllText(child));
		Assert.IsNotNull(parent, "file in the parent folder");
		Assert.AreEqual("parent", File.ReadAllText(parent));
	}

	/// <summary>
	/// If both spellings exist, they are different files and the URI's own spelling has to win.
	/// </summary>
	[TestCase("Korean")]
	[TestCase("Latin")]
	public void BothFormsOnDisk_ExactSpellingWins(string label)
	{
		AssumeFileSystemKeepsNormalizationFormsApart();
		var name = Name(label);
		WriteFile(Nfc(name) + ".bin", "NFC");
		WriteFile(Nfd(name) + ".bin", "NFD");

		Assert.AreEqual("NFC", File.ReadAllText(URIHelper.FindExistingFile(tempDirectory, Nfc(name) + ".bin", Path.Combine)));
		Assert.AreEqual("NFD", File.ReadAllText(URIHelper.FindExistingFile(tempDirectory, Nfd(name) + ".bin", Path.Combine)));
		Assert.AreEqual("NFC", File.ReadAllText(URIHelper.FindExistingFile(tempDirectory, Escape(Nfc(name) + ".bin"), Path.Combine)));
		Assert.AreEqual("NFD", File.ReadAllText(URIHelper.FindExistingFile(tempDirectory, Escape(Nfd(name) + ".bin"), Path.Combine)));
	}

	/// <summary>
	/// A file name that contains a literal "%20" is used as is, before the unescaped name.
	/// </summary>
	[Test]
	public void LiteralPercentInFileName_WinsOverUnescapedName()
	{
		WriteFile("a%20b.bin", "literal");
		WriteFile("a b.bin", "unescaped");

		Assert.AreEqual("literal", File.ReadAllText(URIHelper.FindExistingFile(tempDirectory, "a%20b.bin", Path.Combine)));
		Assert.AreEqual("unescaped", File.ReadAllText(URIHelper.FindExistingFile(tempDirectory, "a b.bin", Path.Combine)));
	}

	/// <summary>
	/// Only the URI is unescaped and normalized, never the directory of the glTF file: that one is a real path already.
	/// </summary>
	[Test]
	public void RootDirectory_IsNotUnescapedOrNormalized()
	{
		var root = Path.Combine(tempDirectory, "root%20" + Nfd("Caf\u00E9"));
		WriteFile(root, "a b.bin", Encoding.UTF8.GetBytes("content"));

		var found = URIHelper.FindExistingFile(root, "a%20b.bin", Path.Combine);

		Assert.IsNotNull(found);
		Assert.AreEqual("content", File.ReadAllText(found));
		StringAssert.StartsWith(root, found);
	}

	[TestCase("Korean")]
	public void MissingFile_ReturnsNull(string label)
	{
		var name = Name(label);
		WriteFile("other.bin", "content");
		foreach (var uri in new[] { Nfc(name) + ".bin", Nfd(name) + ".bin", Escape(Nfd(name) + ".bin"), "missing.bin" })
			Assert.IsNull(URIHelper.FindExistingFile(tempDirectory, uri, Path.Combine), uri);
	}

	/// <summary>
	/// Known limitation: the whole URI is normalized at once. A path whose folder and file name are stored in different
	/// forms on disk is not found. That doesn't happen when the files come from one checkout or one archive.
	/// </summary>
	[Test]
	public void MixedFormsAcrossPathSegments_AreNotFound()
	{
		AssumeFileSystemKeepsNormalizationFormsApart();
		var name = Name("Korean");
		WriteFile(Nfc(name) + "/" + Nfd(name) + ".bin", "content");

		Assert.IsNull(URIHelper.FindExistingFile(tempDirectory, Nfc(name) + "/" + Nfc(name) + ".bin", Path.Combine));
	}

	#endregion

	#region FileLoader

	[TestCase("Korean")]
	[TestCase("Vietnamese")]
	public void LoadStream_NfdBufferUri_ReadsNfcFile(string label)
	{
		var name = Name(label);
		WriteFile(Nfc(name) + ".bin", "buffer");

		using (var stream = new FileLoader(tempDirectory).LoadStream(Nfd(name) + ".bin"))
			Assert.AreEqual("buffer", new StreamReader(stream).ReadToEnd());
	}

	[TestCase("Korean")]
	public void LoadStreamAsync_EscapedNfdUri_ReadsNfcFile(string label)
	{
		var name = Name(label);
		WriteFile("child/" + Nfc(name) + ".png", "image");

		using (var stream = new FileLoader(tempDirectory).LoadStreamAsync(Escape("child/" + Nfd(name) + ".png")).Result)
			Assert.AreEqual("image", new StreamReader(stream).ReadToEnd());
	}

	[Test]
	public void LoadStream_BothFormsOnDisk_ReadsExactSpelling()
	{
		AssumeFileSystemKeepsNormalizationFormsApart();
		var name = Name("Korean");
		WriteFile(Nfc(name) + ".bin", "NFC");
		WriteFile(Nfd(name) + ".bin", "NFD");
		var loader = new FileLoader(tempDirectory);

		using (var stream = loader.LoadStream(Nfd(name) + ".bin"))
			Assert.AreEqual("NFD", new StreamReader(stream).ReadToEnd());
		using (var stream = loader.LoadStream(Nfc(name) + ".bin"))
			Assert.AreEqual("NFC", new StreamReader(stream).ReadToEnd());
	}

	/// <summary>
	/// The error for a missing buffer still names the URI as written in the file.
	/// </summary>
	[Test]
	public void LoadStream_MissingBuffer_ThrowsWithUriAsGiven()
	{
		var uri = Nfd(Name("Korean")) + ".bin";

		var exception = Assert.Throws<FileNotFoundException>(() => new FileLoader(tempDirectory).LoadStream(uri));

		Assert.AreEqual(uri, exception.FileName);
		StringAssert.Contains(uri, exception.Message);
	}

	/// <summary>
	/// A missing image is not an error here: it can be remapped on import.
	/// </summary>
	[Test]
	public void LoadStream_MissingImage_ReturnsInvalidStream()
	{
		var stream = new FileLoader(tempDirectory).LoadStream(Escape(Nfd(Name("Korean")) + ".png"));
		Assert.IsInstanceOf<FileLoader.InvalidStream>(stream);
	}

	[Test]
	public void LoadStream_AbsoluteUri_IsStillReadDirectly()
	{
		var path = WriteFile("absolute.bin", "absolute");
		using (var stream = new FileLoader(Path.Combine(tempDirectory, "elsewhere")).LoadStream(path))
			Assert.AreEqual("absolute", new StreamReader(stream).ReadToEnd());
	}

	#endregion

	#region Asset import

	private const string AssetFolder = "Assets/__UnicodeUriPathTests";

	private static readonly Vector3[] Triangle = { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 2, 0) };

	private static byte[] Positions(Vector3[] positions)
	{
		var floats = positions.SelectMany(p => new[] { p.x, p.y, p.z }).ToArray();
		var bytes = new byte[floats.Length * 4];
		System.Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
		return bytes;
	}

	private static string TexturedTriangleJson(string bufferUri, string imageUri) => new JObject
	{
		["asset"] = new JObject { ["version"] = "2.0" },
		["scene"] = 0,
		["scenes"] = new JArray(new JObject { ["nodes"] = new JArray(0) }),
		["nodes"] = new JArray(new JObject { ["mesh"] = 0, ["name"] = "Triangle" }),
		["meshes"] = new JArray(new JObject { ["primitives"] = new JArray(new JObject { ["attributes"] = new JObject { ["POSITION"] = 0 }, ["material"] = 0 }) }),
		["materials"] = new JArray(new JObject { ["name"] = "Textured", ["pbrMetallicRoughness"] = new JObject { ["baseColorTexture"] = new JObject { ["index"] = 0 } } }),
		["textures"] = new JArray(new JObject { ["source"] = 0 }),
		["images"] = new JArray(new JObject { ["uri"] = imageUri }),
		["accessors"] = new JArray(new JObject { ["bufferView"] = 0, ["componentType"] = 5126, ["count"] = 3, ["type"] = "VEC3", ["min"] = new JArray(0, 0, 0), ["max"] = new JArray(1, 2, 0) }),
		["bufferViews"] = new JArray(new JObject { ["buffer"] = 0, ["byteLength"] = 36 }),
		["buffers"] = new JArray(new JObject { ["uri"] = bufferUri, ["byteLength"] = 36 }),
	}.ToString();

	private static byte[] Png()
	{
		var texture = new Texture2D(2, 2);
		try { return texture.EncodeToPNG(); }
		finally { Object.DestroyImmediate(texture); }
	}

	private const string PlaceholderJson = "{\"asset\":{\"version\":\"2.0\"},\"scene\":0,\"scenes\":[{\"nodes\":[0]}],\"nodes\":[{\"name\":\"Placeholder\"}]}";

	/// <summary>
	/// Writes a .gltf that is imported with UnityGLTF. Another importer (e.g. glTFast) can be the default for .gltf files
	/// in the project, so the file starts as a placeholder that any importer accepts, is switched to the GLTFImporter,
	/// and then gets its content.
	/// </summary>
	private static string WriteGltf(string fileName, string json)
	{
		var path = WriteFile(AssetFolder, fileName, Encoding.UTF8.GetBytes(PlaceholderJson)).Replace("\\", "/");
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		if (AssetDatabase.GetImporterType(path) != typeof(GLTFImporter))
			AssetDatabase.SetImporterOverride<GLTFImporter>(path);
		Assert.AreEqual(typeof(GLTFImporter), AssetDatabase.GetImporterType(path), $"{path} is not imported with the {nameof(GLTFImporter)}");

		File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(json));
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		return path;
	}

	/// <summary>
	/// Writes a glTF with a buffer and an image in a child folder, both stored in NFC on disk and referenced in the given forms.
	/// </summary>
	private static (string gltf, string bin, string png) WriteTexturedTriangle(string bufferUri, string imageUri)
	{
		var name = Nfc(Names["Korean"]);
		var image = Nfc(Names["Japanese"]);
		if (AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.DeleteAsset(AssetFolder);
		var bin = WriteFile(AssetFolder, name + ".bin", Positions(Triangle)).Replace("\\", "/");
		var png = WriteFile(AssetFolder, "textures/" + image + ".png", Png()).Replace("\\", "/");
		var gltf = WriteGltf("Triangle.gltf", TexturedTriangleJson(bufferUri, imageUri));
		return (gltf, bin, png);
	}

	private static readonly object[] UriForms =
	{
		new object[] { "NFD", (Func<string, string>)Nfd },
		new object[] { "escaped NFD", (Func<string, string>)(s => Escape(Nfd(s))) },
		new object[] { "NFC", (Func<string, string>)Nfc },
		new object[] { "escaped NFC", (Func<string, string>)(s => Escape(Nfc(s))) },
	};

	/// <summary>
	/// The case from the bug report: an import from the AssetDatabase, the buffer and the image file are stored in NFC.
	/// Any error logged during the import fails the test.
	/// </summary>
	[TestCaseSource(nameof(UriForms))]
	public void Import_BufferAndImageInAnyForm_LoadsFilesOnDisk(string form, Func<string, string> toUri)
	{
		var files = WriteTexturedTriangle(toUri(Names["Korean"] + ".bin"), toUri("textures/" + Names["Japanese"] + ".png"));

		var root = AssetDatabase.LoadAssetAtPath<GameObject>(files.gltf);
		Assert.IsTrue(root, $"{files.gltf} did not import ({form})");
		var mesh = root.GetComponentsInChildren<MeshFilter>(true).Single().sharedMesh;
		Assert.AreEqual(3, mesh.vertexCount, "vertex count");
		Assert.That(Vector3.Distance(new Vector3(1, 2, 0), mesh.bounds.size), Is.LessThan(1e-5f), $"mesh bounds {mesh.bounds.size}");

		// The image is the texture asset in the project, not a copy: the AssetDatabase path was found
		var material = root.GetComponentInChildren<Renderer>(true).sharedMaterial;
		var texture = material.GetTexturePropertyNameIDs().Select(material.GetTexture).FirstOrDefault(t => t);
		Assert.IsTrue(texture, "no texture on the material");
		Assert.AreEqual(files.png, AssetDatabase.GetAssetPath(texture), "texture asset");
	}

	private static string[] GatherDependencies(string gltfPath)
	{
		var method = typeof(GLTFImporter).GetMethod("GatherDependenciesFromSourceFile", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
		Assert.IsNotNull(method, "GLTFImporter.GatherDependenciesFromSourceFile not found");
		return (string[])method.Invoke(null, new object[] { gltfPath });
	}

	[TestCaseSource(nameof(UriForms))]
	public void Dependencies_BufferAndImageInAnyForm_AreFilesOnDisk(string form, Func<string, string> toUri)
	{
		var files = WriteTexturedTriangle(toUri(Names["Korean"] + ".bin"), toUri("textures/" + Names["Japanese"] + ".png"));

		var dependencies = GatherDependencies(files.gltf);

		CollectionAssert.Contains(dependencies, files.bin, form);
		CollectionAssert.Contains(dependencies, files.png, form);
	}

	/// <summary>
	/// Escaped relative URIs were not found as dependencies before, independent of Unicode.
	/// </summary>
	[Test]
	public void Dependencies_EscapedAsciiUri_IsFileOnDisk()
	{
		if (AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.DeleteAsset(AssetFolder);
		var bin = WriteFile(AssetFolder, "my buffer.bin", Positions(Triangle)).Replace("\\", "/");
		var png = WriteFile(AssetFolder, "my texture.png", Png()).Replace("\\", "/");
		var gltf = WriteGltf("Triangle.gltf", TexturedTriangleJson("my%20buffer.bin", "my%20texture.png"));

		var dependencies = GatherDependencies(gltf);

		CollectionAssert.Contains(dependencies, bin);
		CollectionAssert.Contains(dependencies, png);
	}

	/// <summary>
	/// The dependency is registered with the AssetDatabase: changing the buffer reimports the glTF.
	/// </summary>
	[Test]
	public void ChangedBuffer_ReferencedInNfd_ReimportsGltf()
	{
		var files = WriteTexturedTriangle(Nfd(Names["Korean"] + ".bin"), Escape(Nfd("textures/" + Names["Japanese"] + ".png")));
		Mesh LoadMesh() => AssetDatabase.LoadAssetAtPath<GameObject>(files.gltf).GetComponentsInChildren<MeshFilter>(true).Single().sharedMesh;
		Assert.That(LoadMesh().vertices[0].magnitude, Is.LessThan(1e-5f), "first vertex before the change");

		// same bounds, so the accessor min/max stay valid
		File.WriteAllBytes(files.bin, Positions(new[] { Triangle[1], Triangle[0], Triangle[2] }));
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

		Assert.That(LoadMesh().vertices[0].magnitude, Is.EqualTo(1).Within(1e-5f), "first vertex after the change: the glTF was not reimported");
	}

	#endregion
}
