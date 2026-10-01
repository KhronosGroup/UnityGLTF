using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Import and export of texture sampler settings (filter and wrap modes). The test file is generated: one textured quad per case,
/// each with its own image (so textures aren't deduplicated), texture and sampler.
/// </summary>
public class TextureSamplerTests
{
	private const string TempFolder = "Assets/UnityGLTFTests_TextureSamplers";
	private const string FilePath = TempFolder + "/Samplers.gltf";

	public class SamplerCase
	{
		public string Name;
		// null: the texture has no sampler
		public string SamplerJson;
		public FilterMode Filter;
		public TextureWrapMode WrapU, WrapV;
		public override string ToString() => Name;
	}

	public static readonly SamplerCase[] Cases =
	{
		// No minFilter means NEAREST_MIPMAP_LINEAR, no magFilter LINEAR: smooth, with blending between mipmaps
		new SamplerCase { Name = "EmptySampler", SamplerJson = "{}", Filter = FilterMode.Trilinear, WrapU = TextureWrapMode.Repeat, WrapV = TextureWrapMode.Repeat },
		new SamplerCase { Name = "NoSampler", SamplerJson = null, Filter = FilterMode.Bilinear, WrapU = TextureWrapMode.Repeat, WrapV = TextureWrapMode.Repeat },
		new SamplerCase { Name = "Nearest", SamplerJson = "{\"magFilter\":9728,\"minFilter\":9984}", Filter = FilterMode.Point, WrapU = TextureWrapMode.Repeat, WrapV = TextureWrapMode.Repeat },
		new SamplerCase { Name = "NearestMagOnly", SamplerJson = "{\"magFilter\":9728}", Filter = FilterMode.Point, WrapU = TextureWrapMode.Repeat, WrapV = TextureWrapMode.Repeat },
		new SamplerCase { Name = "LinearNearestMipmapLinear", SamplerJson = "{\"magFilter\":9729,\"minFilter\":9986}", Filter = FilterMode.Trilinear, WrapU = TextureWrapMode.Repeat, WrapV = TextureWrapMode.Repeat },
		new SamplerCase { Name = "LinearMipmapNearest", SamplerJson = "{\"magFilter\":9729,\"minFilter\":9985}", Filter = FilterMode.Bilinear, WrapU = TextureWrapMode.Repeat, WrapV = TextureWrapMode.Repeat },
		new SamplerCase { Name = "LinearMipmapLinear", SamplerJson = "{\"magFilter\":9729,\"minFilter\":9987}", Filter = FilterMode.Trilinear, WrapU = TextureWrapMode.Repeat, WrapV = TextureWrapMode.Repeat },
		new SamplerCase { Name = "ClampRepeat", SamplerJson = "{\"wrapS\":33071,\"wrapT\":10497}", Filter = FilterMode.Trilinear, WrapU = TextureWrapMode.Clamp, WrapV = TextureWrapMode.Repeat },
		new SamplerCase { Name = "RepeatMirror", SamplerJson = "{\"wrapS\":10497,\"wrapT\":33648}", Filter = FilterMode.Trilinear, WrapU = TextureWrapMode.Repeat, WrapV = TextureWrapMode.Mirror },
		new SamplerCase { Name = "MirrorClamp", SamplerJson = "{\"wrapS\":33648,\"wrapT\":33071}", Filter = FilterMode.Trilinear, WrapU = TextureWrapMode.Mirror, WrapV = TextureWrapMode.Clamp },
	};

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		if (!AssetDatabase.IsValidFolder(TempFolder))
			AssetDatabase.CreateFolder("Assets", Path.GetFileName(TempFolder));
		File.WriteAllText(FilePath, CreateGltf());
		AssetDatabase.ImportAsset(FilePath, ImportAssetOptions.ForceSynchronousImport);
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		AssetDatabase.DeleteAsset(TempFolder);
	}

	[TestCaseSource(nameof(Cases))]
	public void Import_SetsFilterAndWrapModes(SamplerCase c)
	{
		var texture = LoadTexture(FilePath, c.Name);
		Assert.AreEqual(c.Filter, texture.filterMode, "Filter mode");
		Assert.AreEqual(c.WrapU, texture.wrapModeU, "Wrap mode U");
		Assert.AreEqual(c.WrapV, texture.wrapModeV, "Wrap mode V");
	}

	#region Helpers

	private static Texture LoadTexture(string path, string materialName)
	{
		var material = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().SingleOrDefault(m => m.name == materialName);
		Assert.IsNotNull(material, $"Material \"{materialName}\" missing in {path}");
		var texture = material.GetTexturePropertyNames().Select(material.GetTexture).FirstOrDefault(t => t);
		Assert.IsNotNull(texture, $"Material \"{materialName}\" has no texture");
		return texture;
	}

	/// <summary>
	/// A glTF with one quad per case. Mesh data and images are embedded as data URIs.
	/// </summary>
	private static string CreateGltf()
	{
		var buffer = new List<byte>();
		foreach (var f in new float[] { 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0 }) buffer.AddRange(BitConverter.GetBytes(f));
		foreach (var f in new float[] { 0, 1, 1, 1, 1, 0, 0, 0 }) buffer.AddRange(BitConverter.GetBytes(f));
		foreach (var i in new ushort[] { 0, 1, 2, 0, 2, 3 }) buffer.AddRange(BitConverter.GetBytes(i));

		var samplers = new List<string>();
		var nodes = new List<string>();
		var meshes = new List<string>();
		var materials = new List<string>();
		var textures = new List<string>();
		var images = new List<string>();
		for (var i = 0; i < Cases.Length; i++)
		{
			var c = Cases[i];
			nodes.Add($"{{\"name\":\"{c.Name}\",\"mesh\":{i},\"translation\":[{(i * 1.5f).ToString(System.Globalization.CultureInfo.InvariantCulture)},0,0]}}");
			meshes.Add($"{{\"primitives\":[{{\"attributes\":{{\"POSITION\":0,\"TEXCOORD_0\":1}},\"indices\":2,\"material\":{i}}}]}}");
			materials.Add($"{{\"name\":\"{c.Name}\",\"pbrMetallicRoughness\":{{\"baseColorTexture\":{{\"index\":{i}}}}}}}");
			if (c.SamplerJson != null)
			{
				textures.Add($"{{\"source\":{i},\"sampler\":{samplers.Count}}}");
				samplers.Add(c.SamplerJson);
			}
			else
			{
				textures.Add($"{{\"source\":{i}}}");
			}

			// A different color per image, so the importer doesn't merge them
			var image = new Texture2D(4, 4);
			image.SetPixels(Enumerable.Repeat(Color.HSVToRGB(i / (float)Cases.Length, 0.8f, 1), 16).ToArray());
			image.Apply();
			images.Add($"{{\"name\":\"{c.Name}\",\"uri\":\"data:image/png;base64,{Convert.ToBase64String(image.EncodeToPNG())}\"}}");
			Object.DestroyImmediate(image);
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
