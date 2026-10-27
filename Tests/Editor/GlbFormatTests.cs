using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GLTF;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityGLTF;
using UnityGLTF.Plugins;
using Object = UnityEngine.Object;

/// <summary>
/// Binary layout of exported GLB files.
/// </summary>
public class GlbFormatTests
{
	// Names of different lengths, so the JSON chunk needs 0 to 3 bytes of padding across the cases
	[TestCase("A")]
	[TestCase("AB")]
	[TestCase("ABC")]
	[TestCase("ABCD")]
	public void Export_JsonChunk_IsPaddedWithSpaces(string name)
	{
		var settings = ScriptableObject.CreateInstance<GLTFSettings>();
		var root = new GameObject(name);
		try
		{
			var exporter = new GLTFSceneExporter(root.transform, new ExportContext(settings));
			var bytes = exporter.SaveGLBToByteArray(name);

			var jsonLength = BitConverter.ToInt32(bytes, 12);
			Assert.AreEqual(0, jsonLength % 4, "JSON chunk length must be 4-byte aligned");
			var json = Encoding.UTF8.GetString(bytes, 20, jsonLength);
			var end = json.LastIndexOf('}') + 1;
			for (var i = end; i < json.Length; i++)
				Assert.AreEqual(' ', json[i], $"JSON chunk padding byte {i - end} must be a space (0x20), JSON chunk ends with '{json.Substring(end)}'");
		}
		finally
		{
			Object.DestroyImmediate(root);
			Object.DestroyImmediate(settings);
		}
	}

	[Test]
	public void Export_Default_IsVersion2() => Assert.AreEqual(2, ExportCube(null).Version);

	[Test]
	public void Export_Version3_HasV3LayoutAndParses()
	{
		var glb = ExportCube(context => context.GlbVersion = GlbVersion.V3_64Bit);
		Assert.AreEqual(3, glb.Version);
		CollectionAssert.AreEqual(new[] { GlbTestFile.JsonType, GlbTestFile.BinType }, glb.Chunks.Select(c => c.Type).ToArray(), "chunk types");
		Assert.That(glb.Chunks.Select(c => c.Encoding), Is.All.EqualTo(0), "chunk encodings");
		Assert.AreEqual(glb.Bin().Length, (long)glb.ParseJson()["buffers"][0]["byteLength"], "buffer byteLength");
	}

	[Test]
	public void Export_GlbV3Plugin_EnablesVersion3AndWarns()
	{
		LogAssert.Expect(LogType.Warning, new Regex("GLB version 3"));
		var plugin = ScriptableObject.CreateInstance<GlbV3Export>();
		try
		{
			Assert.AreEqual(3, ExportCube(null, plugin).Version);
		}
		finally
		{
			Object.DestroyImmediate(plugin);
		}
	}

	/// <summary>
	/// Exports a cube and reads the result with <see cref="GlbTestFile.Read"/>, which asserts the binary layout of its version
	/// and that <see cref="GLTFParser"/> can parse it.
	/// </summary>
	private static GlbTestFile ExportCube(Action<ExportContext> configure, GLTFExportPlugin plugin = null)
	{
		var settings = ScriptableObject.CreateInstance<GLTFSettings>();
		if (plugin) settings.ExportPlugins.Add(plugin);
		var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
		try
		{
			var context = new ExportContext(settings);
			configure?.Invoke(context);
			var bytes = new GLTFSceneExporter(root.transform, context).SaveGLBToByteArray("cube");
			var glb = GlbTestFile.Read(bytes);
			GLTFParser.ParseJson(new MemoryStream(bytes), out var gltfRoot);
			Assert.AreEqual(1, gltfRoot.Meshes.Count, "meshes");
			return glb;
		}
		finally
		{
			Object.DestroyImmediate(root);
			Object.DestroyImmediate(settings);
		}
	}
}
