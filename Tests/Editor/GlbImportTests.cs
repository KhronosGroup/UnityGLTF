using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GLTF;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityGLTF;
using Object = UnityEngine.Object;

/// <summary>
/// Import of GLB binary format version 2 and 3 (glTF 2.1 draft) chunk layouts.
/// Each file contains one triangle, so reading the wrong chunk shows up as wrong mesh bounds instead of passing silently.
/// </summary>
public class GlbImportTests
{
	private const uint ExtraType = 0x41525458; // "XTRA", an unknown chunk type that has to be ignored
	private const uint ZstdEncoding = 0x4454535A; // "ZSTD", an encoding that is not supported
	private static readonly Vector3 TriangleSize = new Vector3(1, 2, 0);

	[UnityTest]
	public IEnumerator V2_ImplicitBinChunk_Imports() => ImportTriangle(GlbTestFile.Write(2,
		GlbTestFile.Json(TriangleJson(Buffer())),
		GlbTestFile.Bin(Triangle())));

	[UnityTest]
	public IEnumerator V3_ImplicitBinChunk_Imports() => ImportTriangle(GlbTestFile.Write(3,
		GlbTestFile.Json(TriangleJson(Buffer())),
		GlbTestFile.Bin(Triangle())));

	[UnityTest]
	public IEnumerator V3_ChunkBeforeJsonChunk_Imports() => ImportTriangle(GlbTestFile.Write(3,
		Extra(),
		GlbTestFile.Json(TriangleJson(Buffer(chunk: 2))),
		GlbTestFile.Bin(Triangle())));

	[UnityTest]
	public IEnumerator V3_SecondBufferReferencesChunkByIndex_Imports() => ImportTriangle(GlbTestFile.Write(3,
		GlbTestFile.Json(TriangleJson(Buffer(byteLength: 8), Buffer(chunk: 3))),
		GlbTestFile.Bin(Encoding.ASCII.GetBytes("NOTDATA!")),
		Extra(),
		GlbTestFile.Bin(Triangle())), bufferIndex: 1);

	[UnityTest]
	public IEnumerator V3_UnusedChunkWithUnknownEncoding_Imports() => ImportTriangle(GlbTestFile.Write(3,
		GlbTestFile.Json(TriangleJson(Buffer())),
		GlbTestFile.Bin(Triangle()),
		new GlbTestFile.Chunk(ExtraType, new byte[16], ZstdEncoding)));

	[UnityTest]
	public IEnumerator V3_BinChunkWithUnknownEncoding_IsRefused()
	{
		LogAssert.Expect(LogType.Error, new Regex("Unsupported encoding 'ZSTD'"));
		return ImportTriangle(GlbTestFile.Write(3,
			GlbTestFile.Json(TriangleJson(Buffer())),
			GlbTestFile.Bin(Triangle(), ZstdEncoding)), expectFailure: true);
	}

	[Test]
	public void V3_JsonChunkWithUnknownEncoding_IsRefused()
	{
		var bytes = GlbTestFile.Write(3,
			new GlbTestFile.Chunk(GlbTestFile.JsonType, Encoding.UTF8.GetBytes(TriangleJson(Buffer())), ZstdEncoding),
			GlbTestFile.Bin(Triangle()));
		Assert.Throws<GLTFHeaderInvalidException>(() => GLTFParser.ParseJson(new MemoryStream(bytes), out _));
	}

	[Test]
	public void V3_FileLengthWithMostSignificantBit_IsRefused()
	{
		var bytes = GlbTestFile.Write(3, GlbTestFile.Json(TriangleJson(Buffer())), GlbTestFile.Bin(Triangle()));
		bytes[15] |= 0x80; // most significant bit of the 64-bit file length
		Assert.Throws<GLTFHeaderInvalidException>(() => GLTFParser.ParseJson(new MemoryStream(bytes), out _));
	}

	[Test]
	public void V3_ChunkLengthWithMostSignificantBit_IsRefused()
	{
		var bytes = GlbTestFile.Write(3, GlbTestFile.Json(TriangleJson(Buffer())), GlbTestFile.Bin(Triangle()));
		bytes[16 + 15] |= 0x80; // most significant bit of the JSON chunk's 64-bit length
		Assert.Throws<GLTFHeaderInvalidException>(() => GLTFParser.ParseJson(new MemoryStream(bytes), out _));
	}

	private static GlbTestFile.Chunk Extra() => new GlbTestFile.Chunk(ExtraType, Encoding.ASCII.GetBytes("unknown chunk!"));

	private static string Buffer(long byteLength = 36, int? chunk = null) =>
		"{\"byteLength\":" + byteLength + (chunk.HasValue ? ",\"chunk\":" + chunk : "") + "}";

	private static string TriangleJson(params string[] buffers) => TriangleJson(buffers.Length - 1, buffers);

	private static string TriangleJson(int bufferIndex, string[] buffers) =>
		"{\"asset\":{\"version\":\"2.0\"},\"scene\":0,\"scenes\":[{\"nodes\":[0]}],\"nodes\":[{\"mesh\":0}]," +
		"\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0}}]}]," +
		"\"accessors\":[{\"bufferView\":0,\"componentType\":5126,\"count\":3,\"type\":\"VEC3\",\"min\":[0,0,0],\"max\":[1,2,0]}]," +
		"\"bufferViews\":[{\"buffer\":" + bufferIndex + ",\"byteLength\":36}]," +
		"\"buffers\":[" + string.Join(",", buffers) + "]}";

	private static byte[] Triangle()
	{
		var floats = new float[] { 0, 0, 0, 1, 0, 0, 0, 2, 0 };
		var bytes = new byte[floats.Length * 4];
		System.Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
		return bytes;
	}

	private static IEnumerator ImportTriangle(byte[] glb, int bufferIndex = 0, bool expectFailure = false)
	{
		GLTFSceneImporter importer = null;
		Exception exception = null;
		try
		{
			importer = new GLTFSceneImporter(new MemoryStream(glb), new ImportOptions());
		}
		catch (Exception e)
		{
			exception = e;
		}

		if (importer != null)
		{
			var task = importer.LoadSceneAsync();
			while (!task.IsCompleted) yield return null;
			exception = task.Exception;
		}

		var created = importer?.CreatedObject;
		try
		{
			if (expectFailure)
			{
				Assert.IsNotNull(exception, "import must fail");
				yield break;
			}
			Assert.IsNull(exception, exception?.ToString());
			Assert.IsTrue(created, "no object created");
			var mesh = created.GetComponentsInChildren<MeshFilter>().Single().sharedMesh;
			Assert.AreEqual(3, mesh.vertexCount, "vertex count");
			Assert.That(Vector3.Distance(TriangleSize, mesh.bounds.size), Is.LessThan(1e-5f), $"mesh bounds {mesh.bounds.size}, buffer {bufferIndex} was not read from its chunk");
		}
		finally
		{
			if (created) Object.DestroyImmediate(created);
			importer?.Dispose();
		}
	}
}
