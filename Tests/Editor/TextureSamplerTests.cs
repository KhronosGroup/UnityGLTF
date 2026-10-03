using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GLTF.Schema;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityGLTF;
using Object = UnityEngine.Object;
using WrapMode = GLTF.Schema.WrapMode;

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

	[TestCase(TextureWrapMode.Clamp, TextureWrapMode.Repeat, WrapMode.ClampToEdge, WrapMode.Repeat)]
	[TestCase(TextureWrapMode.Repeat, TextureWrapMode.Mirror, WrapMode.Repeat, WrapMode.MirroredRepeat)]
	[TestCase(TextureWrapMode.Mirror, TextureWrapMode.Clamp, WrapMode.MirroredRepeat, WrapMode.ClampToEdge)]
	[TestCase(TextureWrapMode.Clamp, TextureWrapMode.Clamp, WrapMode.ClampToEdge, WrapMode.ClampToEdge)]
	public void Export_WritesWrapModesPerAxis(TextureWrapMode u, TextureWrapMode v, WrapMode expectedS, WrapMode expectedT)
	{
		var root = new GameObject("Root");
		var objects = new List<Object> { root };
		var settings = ScriptableObject.CreateInstance<GLTFSettings>();
		settings.UseMainCameraVisibility = false;
		objects.Add(settings);
		try
		{
			// A second texture with the same U but a different V, so samplers can't be shared by mistake
			var other = v == TextureWrapMode.Repeat ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
			AddTexturedQuad(root, "Tested", u, v, objects);
			AddTexturedQuad(root, "Other", u, other, objects);

			var exporter = new GLTFSceneExporter(root.transform, new ExportContext(settings));
			exporter.SaveGLBToByteArray("Samplers");

			Sampler SamplerOf(string materialName)
			{
				var material = exporter.GetRoot().Materials.Single(m => m.Name == materialName);
				return material.PbrMetallicRoughness.BaseColorTexture.Index.Value.Sampler.Value;
			}
			var sampler = SamplerOf("Tested");
			Assert.AreEqual(expectedS, sampler.WrapS, "wrapS");
			Assert.AreEqual(expectedT, sampler.WrapT, "wrapT");
			Assert.AreNotEqual(sampler.WrapT, SamplerOf("Other").WrapT, "Textures with different wrap modes share a sampler");
		}
		finally
		{
			foreach (var o in objects) Object.DestroyImmediate(o);
		}
	}

	#region Helpers

	private static void AddTexturedQuad(GameObject root, string name, TextureWrapMode u, TextureWrapMode v, List<Object> objects)
	{
		var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		quad.name = name;
		quad.transform.SetParent(root.transform, false);
		var texture = new Texture2D(4, 4) { name = name, wrapModeU = u, wrapModeV = v };
		texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
		texture.Apply();
		var renderer = quad.GetComponent<Renderer>();
		// Copy of the render pipeline's default material, so the test works with every pipeline
		var material = new Material(renderer.sharedMaterial) { name = name, mainTexture = texture };
		renderer.sharedMaterial = material;
		objects.Add(texture);
		objects.Add(material);
	}

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
