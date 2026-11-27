using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityGLTF;
using Object = UnityEngine.Object;

/// <summary>
/// Names of imported textures, and the file names they get when exported as .gltf with separate textures.
/// An image without a name is named after the file of its URI, but an embedded data URI is not a file name: its base64 data
/// contains '/', so the "file name" was a fragment of the data, and exporting as .gltf failed with an invalid path.
/// The test file is generated: one textured quad per case.
/// </summary>
public class TextureNameTests
{
	private const string TempFolder = "Assets/UnityGLTFTests_TextureNames";
	private const string FilePath = TempFolder + "/TextureNames.gltf";
	private const string ExportFolder = TempFolder + "/Export";
	private const string ExternalImage = "external image.png";

	private class NameCase
	{
		public string Material;
		public int Image;
		// null: the texture has no sampler
		public int? Sampler;
	}

	// Images: 0 and 1 unnamed with data URIs, 2 named with a data URI, 3 an external file
	private static readonly NameCase[] Cases =
	{
		new NameCase { Material = "UnnamedDataUri", Image = 0 },
		// The same unnamed image with two different samplers: the second texture is a copy of the image, named separately
		new NameCase { Material = "SharedSamplerA", Image = 1, Sampler = 0 },
		new NameCase { Material = "SharedSamplerB", Image = 1, Sampler = 1 },
		new NameCase { Material = "NamedDataUri", Image = 2 },
		new NameCase { Material = "FileUri", Image = 3 },
	};

	private static readonly Regex PlaceholderName = new Regex(@"^Texture_\d+$");

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		if (!AssetDatabase.IsValidFolder(TempFolder))
			AssetDatabase.CreateFolder("Assets", Path.GetFileName(TempFolder));
		File.WriteAllBytes($"{TempFolder}/{ExternalImage}", CreatePng(Color.blue));
		File.WriteAllText(FilePath, CreateGltf());
		AssetDatabase.ImportAsset($"{TempFolder}/{ExternalImage}", ImportAssetOptions.ForceSynchronousImport);
		AssetDatabase.ImportAsset(FilePath, ImportAssetOptions.ForceSynchronousImport);
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		AssetDatabase.DeleteAsset(TempFolder);
	}

	[TestCase("UnnamedDataUri")]
	[TestCase("SharedSamplerA")]
	[TestCase("SharedSamplerB")]
	public void Import_UnnamedDataUriImage_GetsPlaceholderName(string material)
	{
		var name = VisibleName(LoadTexture(material).name);
		Assert.IsTrue(PlaceholderName.IsMatch(name), $"Texture of \"{material}\" is named \"{Shorten(name)}\", expected Texture_N");
	}

	[TestCase("NamedDataUri", "Named Image")]
	[TestCase("FileUri", "external image")]
	public void Import_NamedImageOrFileUri_KeepsName(string material, string expected)
	{
		Assert.AreEqual(expected, VisibleName(LoadTexture(material).name));
	}

	[Test]
	public void ExportGltf_AfterImportingDataUris_WritesValidTextureFiles()
	{
		var model = AssetDatabase.LoadAssetAtPath<GameObject>(FilePath);
		Assert.IsNotNull(model, $"{FilePath} wasn't imported");
		var instance = Object.Instantiate(model);
		var settings = ScriptableObject.CreateInstance<GLTFSettings>();
		settings.UseMainCameraVisibility = false;
		var exportFolder = Path.GetFullPath(ExportFolder);
		Directory.CreateDirectory(exportFolder);
		try
		{
			var exporter = new GLTFSceneExporter(instance.transform, new ExportContext(settings));
			exporter.SaveGLTFandBin(exportFolder, "TextureNames");
		}
		finally
		{
			Object.DestroyImmediate(instance);
			Object.DestroyImmediate(settings);
		}

		var json = GltfFile.Read(Path.Combine(exportFolder, "TextureNames.gltf"));
		var uris = json["images"]?.Select(i => (string)i["uri"]).ToList() ?? new List<string>();
		// At least one per image; the image used with two samplers is imported as two textures, which may be exported separately
		Assert.GreaterOrEqual(uris.Count, 4, "Exported images");
		var invalid = Path.GetInvalidFileNameChars();
		foreach (var uri in uris)
		{
			Assert.IsNotNull(uri, "Exported image without uri");
			var file = Uri.UnescapeDataString(uri);
			Assert.IsFalse(file.StartsWith("data:"), $"Image exported as data URI instead of a file: {Shorten(file)}");
			Assert.IsFalse(file.IndexOfAny(invalid) >= 0, $"Invalid texture file name \"{Shorten(file)}\"");
			Assert.IsTrue(File.Exists(Path.Combine(exportFolder, file)), $"Texture file \"{Shorten(file)}\" wasn't written");
			var name = VisibleName(Path.GetFileNameWithoutExtension(file));
			Assert.IsTrue(PlaceholderName.IsMatch(name) || name == "Named Image" || name == "external image",
				$"Unexpected texture file name \"{Shorten(file)}\"");
		}
	}

	#region Helpers

	/// <summary>The name without the suffix the importer adds to placeholder names (GLTFSceneImporter.EMPTY_TEXTURE_NAME_SUFFIX).</summary>
	private static string VisibleName(string name) => name.TrimEnd(' ', '\0');

	private static string Shorten(string s) => s.Length > 80 ? s.Substring(0, 80) + "…" : s;

	private static Texture LoadTexture(string materialName)
	{
		var material = AssetDatabase.LoadAllAssetsAtPath(FilePath).OfType<Material>().SingleOrDefault(m => m.name == materialName);
		Assert.IsNotNull(material, $"Material \"{materialName}\" missing in {FilePath}");
		var texture = material.GetTexturePropertyNames().Select(material.GetTexture).FirstOrDefault(t => t);
		Assert.IsNotNull(texture, $"Material \"{materialName}\" has no texture");
		return texture;
	}

	private static byte[] CreatePng(Color color)
	{
		var image = new Texture2D(4, 4);
		image.SetPixels(Enumerable.Repeat(color, 16).ToArray());
		image.Apply();
		var png = image.EncodeToPNG();
		Object.DestroyImmediate(image);
		return png;
	}

	private static string DataUri(Color color) => $"data:image/png;base64,{Convert.ToBase64String(CreatePng(color))}";

	/// <summary>A glTF with one quad per case. Mesh data and all images but the external one are embedded as data URIs.</summary>
	private static string CreateGltf()
	{
		var buffer = new List<byte>();
		foreach (var f in new float[] { 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0 }) buffer.AddRange(BitConverter.GetBytes(f));
		foreach (var f in new float[] { 0, 1, 1, 1, 1, 0, 0, 0 }) buffer.AddRange(BitConverter.GetBytes(f));
		foreach (var i in new ushort[] { 0, 1, 2, 0, 2, 3 }) buffer.AddRange(BitConverter.GetBytes(i));

		// Different colors, so the importer doesn't merge the images
		var images = new[]
		{
			$"{{\"uri\":\"{DataUri(Color.red)}\"}}",
			$"{{\"uri\":\"{DataUri(Color.green)}\"}}",
			$"{{\"name\":\"Named Image\",\"uri\":\"{DataUri(Color.yellow)}\"}}",
			$"{{\"uri\":\"{Uri.EscapeDataString(ExternalImage)}\"}}",
		};
		var samplers = new[]
		{
			"{\"magFilter\":9729,\"minFilter\":9987,\"wrapS\":10497,\"wrapT\":10497}",
			"{\"magFilter\":9728,\"minFilter\":9984,\"wrapS\":33071,\"wrapT\":33071}",
		};

		var nodes = new List<string>();
		var meshes = new List<string>();
		var materials = new List<string>();
		var textures = new List<string>();
		for (var i = 0; i < Cases.Length; i++)
		{
			var c = Cases[i];
			nodes.Add($"{{\"name\":\"{c.Material}\",\"mesh\":{i},\"translation\":[{(i * 1.5f).ToString(System.Globalization.CultureInfo.InvariantCulture)},0,0]}}");
			meshes.Add($"{{\"primitives\":[{{\"attributes\":{{\"POSITION\":0,\"TEXCOORD_0\":1}},\"indices\":2,\"material\":{i}}}]}}");
			materials.Add($"{{\"name\":\"{c.Material}\",\"pbrMetallicRoughness\":{{\"baseColorTexture\":{{\"index\":{i}}}}}}}");
			textures.Add(c.Sampler == null ? $"{{\"source\":{c.Image}}}" : $"{{\"source\":{c.Image},\"sampler\":{c.Sampler}}}");
		}

		var json = new StringBuilder();
		json.Append("{\"asset\":{\"version\":\"2.0\"},\"scene\":0,");
		json.Append($"\"scenes\":[{{\"nodes\":[{string.Join(",", Enumerable.Range(0, Cases.Length))}]}}],");
		json.Append($"\"nodes\":[{string.Join(",", nodes)}],");
		json.Append($"\"meshes\":[{string.Join(",", meshes)}],");
		json.Append($"\"materials\":[{string.Join(",", materials)}],");
		json.Append($"\"textures\":[{string.Join(",", textures)}],");
		json.Append($"\"samplers\":[{string.Join(",", samplers)}],");
		json.Append($"\"images\":[{string.Join(",", images)}],");
		json.Append("\"accessors\":[");
		json.Append("{\"bufferView\":0,\"componentType\":5126,\"count\":4,\"type\":\"VEC3\",\"min\":[0,0,0],\"max\":[1,1,0]},");
		json.Append("{\"bufferView\":1,\"componentType\":5126,\"count\":4,\"type\":\"VEC2\"},");
		json.Append("{\"bufferView\":2,\"componentType\":5123,\"count\":6,\"type\":\"SCALAR\"}],");
		json.Append("\"bufferViews\":[{\"buffer\":0,\"byteOffset\":0,\"byteLength\":48},{\"buffer\":0,\"byteOffset\":48,\"byteLength\":32},{\"buffer\":0,\"byteOffset\":80,\"byteLength\":12}],");
		json.Append($"\"buffers\":[{{\"byteLength\":{buffer.Count},\"uri\":\"data:application/octet-stream;base64,{Convert.ToBase64String(buffer.ToArray())}\"}}]}}");
		return json.ToString();
	}

	#endregion
}
