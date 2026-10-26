using System;
using System.IO;
using System.Linq;
using System.Text;
using GLTF;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

/// <summary>
/// GLBBuilder write paths for GLB version 2 and 3.
/// The output is checked with an independent reader instead of GLTFParser, which shares code with GLBBuilder.
/// </summary>
public class GlbBuilderTests
{
	[Test]
	public void EmptyStream_UpdateStream_WritesValidGlb()
	{
		var stream = new MemoryStream();
		var glb = GLBBuilder.ConstructFromStream(stream);
		GLBBuilder.SetRoot(glb, NewRoot());
		GLBBuilder.UpdateStream(glb);

		var file = ReadGlb(stream.ToArray());
		Assert.AreEqual(2, file.Version);
		Assert.AreEqual(1, file.Chunks.Count, "chunk count");
		Assert.AreEqual("2.0", (string)file.ParseJson()["asset"]["version"]);
	}

	[TestCase(2u)]
	[TestCase(3u)]
	public void AddBinaryData_WritesBinChunk(uint version)
	{
		var glb = Load(version, null);
		var data = new byte[] { 1, 2, 3, 4, 5 };
		var id = GLBBuilder.AddBinaryData(glb, new MemoryStream(data));
		GLBBuilder.UpdateStream(glb);

		var file = ReadGlb(((MemoryStream)glb.Stream).ToArray());
		Assert.AreEqual(version, file.Version);
		var view = file.ParseJson()["bufferViews"][id.Id];
		var offset = (long?)view["byteOffset"] ?? 0;
		CollectionAssert.AreEqual(data, file.Bin().Skip((int)offset).Take(data.Length).ToArray(), "binary data");
		Assert.AreEqual(file.Bin().Length, (long)file.ParseJson()["buffers"][0]["byteLength"], "buffer byteLength");
	}

	[TestCase(2u)]
	[TestCase(3u)]
	public void UpdateStream_GrowingJson_KeepsBinChunk(uint version)
	{
		var data = Encoding.ASCII.GetBytes("BINDATA!");
		var glb = Load(version, data);
		// Make the JSON larger than the existing JSON chunk, so the BIN chunk has to move
		glb.Root.Nodes = Enumerable.Range(0, 200).Select(i => new Node { Name = "Node" + i }).ToList();
		GLBBuilder.UpdateStream(glb);

		var file = ReadGlb(((MemoryStream)glb.Stream).ToArray());
		Assert.AreEqual(version, file.Version);
		Assert.AreEqual(200, file.ParseJson()["nodes"].Count(), "node count");
		CollectionAssert.AreEqual(data, file.Bin().Take(data.Length).ToArray(), "binary data");
	}

	[TestCase(2u, 2u)]
	[TestCase(2u, 3u)]
	[TestCase(3u, 2u)]
	[TestCase(3u, 3u)]
	public void MergeGLBs_AppendsBinaryData(uint toVersion, uint fromVersion)
	{
		// Different sizes, so offsets shifted by the wrong size are detected
		var to = Load(toVersion, Encoding.ASCII.GetBytes("AAAAAAAA"));
		var from = Load(fromVersion, Encoding.ASCII.GetBytes("BBBBBBBBBBBBBBBB"));
		GLBBuilder.MergeGLBs(to, from);
		GLBBuilder.UpdateStream(to);

		var file = ReadGlb(((MemoryStream)to.Stream).ToArray());
		Assert.AreEqual(toVersion, file.Version);
		var json = file.ParseJson();
		var views = json["bufferViews"];
		Assert.AreEqual(2, views.Count(), "bufferView count");
		foreach (var (view, expected) in new[] { (views[0], "AAAAAAAA"), (views[1], "BBBBBBBBBBBBBBBB") })
		{
			var offset = (int)((long?)view["byteOffset"] ?? 0);
			Assert.AreEqual(expected, Encoding.ASCII.GetString(file.Bin(), offset, expected.Length), "data of bufferView with offset " + offset);
		}
	}

	[TestCase(2u, 3u)]
	[TestCase(3u, 2u)]
	public void ConvertVersion_KeepsJsonAndBinaryData(uint fromVersion, uint toVersion)
	{
		// 12 bytes: needs padding for the 8-byte alignment of version 3
		var data = Encoding.ASCII.GetBytes("BINDATA!1234");
		var glb = Load(fromVersion, data);
		GLBBuilder.ConvertVersion(glb, toVersion);

		var file = ReadGlb(((MemoryStream)glb.Stream).ToArray());
		Assert.AreEqual(toVersion, file.Version);
		Assert.AreEqual("2.0", (string)file.ParseJson()["asset"]["version"]);
		CollectionAssert.AreEqual(data, file.Bin().Take(data.Length).ToArray(), "binary data");
		Assert.That(file.Bin().Skip(data.Length), Is.All.EqualTo(0), "binary padding");
		Assert.AreEqual(file.Bin().Length, (long)file.ParseJson()["buffers"][0]["byteLength"], "buffer byteLength");
	}

	[TestCase(2u)]
	[TestCase(3u)]
	public void RemoveBinaryData_LastView_WritesValidGlb(uint version)
	{
		var glb = Load(version, Encoding.ASCII.GetBytes("AAAAAAAA"));
		var id = GLBBuilder.AddBinaryData(glb, new MemoryStream(Encoding.ASCII.GetBytes("BBBBBBBB")));
		GLBBuilder.RemoveBinaryData(glb, id);
		GLBBuilder.UpdateStream(glb);

		var file = ReadGlb(((MemoryStream)glb.Stream).ToArray());
		Assert.AreEqual(version, file.Version);
		Assert.AreEqual("AAAAAAAA", Encoding.ASCII.GetString(file.Bin(), 0, 8));
		Assert.AreEqual(1, file.ParseJson()["bufferViews"].Count(), "bufferView count");
	}

	private const string BufferJson = "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":8}]}";
	private static GlbTestFile.Chunk Extra() => new GlbTestFile.Chunk(0x41525458, new byte[8]); // "XTRA"

	private static readonly object[] UnsupportedLayouts =
	{
		new object[] { "chunk before JSON", new[] { Extra(), GlbTestFile.Json(BufferJson), GlbTestFile.Bin(new byte[8]) } },
		new object[] { "chunk after BIN", new[] { GlbTestFile.Json(BufferJson), GlbTestFile.Bin(new byte[8]), Extra() } },
		new object[] { "two BIN chunks", new[] { GlbTestFile.Json(BufferJson), GlbTestFile.Bin(new byte[8]), GlbTestFile.Bin(new byte[8]) } },
		new object[] { "encoded BIN chunk", new[] { GlbTestFile.Json(BufferJson), GlbTestFile.Bin(new byte[8], 0x4454535A) } },
	};

	/// <summary>
	/// GLBObject models one JSON chunk followed by an optional BIN chunk, so other version 3 layouts can't be edited without losing data.
	/// </summary>
	[TestCaseSource(nameof(UnsupportedLayouts))]
	public void ConstructFromStream_UnsupportedV3Layout_Throws(string layout, GlbTestFile.Chunk[] chunks)
	{
		var bytes = GlbTestFile.Write(3, chunks);
		Assert.Throws<NotSupportedException>(() => GLBBuilder.ConstructFromStream(new MemoryStream(bytes)), layout);
	}

	[TestCase(2u)]
	[TestCase(3u)]
	public void StreamStartPosition_AddBinaryDataAndUpdateStream_KeepsPrefixAndWritesValidGlb(uint version)
	{
		var prefix = Encoding.ASCII.GetBytes("PREFIX__PREFIX__");
		var data = Encoding.ASCII.GetBytes("BINDATA!");
		var json = "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":8}],\"bufferViews\":[{\"buffer\":0,\"byteLength\":8}]}";
		var glbBytes = GlbTestFile.Write(version, GlbTestFile.Json(json), GlbTestFile.Bin(data));
		var stream = new MemoryStream();
		stream.Write(prefix, 0, prefix.Length);
		stream.Write(glbBytes, 0, glbBytes.Length);

		var glb = GLBBuilder.ConstructFromStream(stream, streamStartPosition: prefix.Length);
		var added = Encoding.ASCII.GetBytes("ADDED!!!");
		var id = GLBBuilder.AddBinaryData(glb, new MemoryStream(added));
		glb.Root.Nodes = Enumerable.Range(0, 50).Select(i => new Node { Name = "Node" + i }).ToList();
		GLBBuilder.UpdateStream(glb);

		var bytes = stream.ToArray();
		Assert.AreEqual(prefix, bytes.Take(prefix.Length).ToArray(), "data before the GLB");
		var file = ReadGlb(bytes.Skip(prefix.Length).ToArray());
		Assert.AreEqual(version, file.Version);
		Assert.AreEqual(50, file.ParseJson()["nodes"].Count(), "node count");
		Assert.AreEqual("BINDATA!", Encoding.ASCII.GetString(file.Bin(), 0, 8));
		var offset = (int)(long)file.ParseJson()["bufferViews"][id.Id]["byteOffset"];
		Assert.AreEqual("ADDED!!!", Encoding.ASCII.GetString(file.Bin(), offset, 8));
	}

	[TestCase(2u)]
	[TestCase(3u)]
	public void ConstructFromGLB_WithOutStream_EditsTheCopy(uint version)
	{
		var prefix = Encoding.ASCII.GetBytes("PREFIX__");
		var data = Encoding.ASCII.GetBytes("BINDATA!");
		var json = "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":8}],\"bufferViews\":[{\"buffer\":0,\"byteLength\":8}]}";
		var glbBytes = GlbTestFile.Write(version, GlbTestFile.Json(json), GlbTestFile.Bin(data));
		var input = new MemoryStream();
		input.Write(prefix, 0, prefix.Length);
		input.Write(glbBytes, 0, glbBytes.Length);
		var inputBytes = input.ToArray();

		var output = new MemoryStream();
		var glb = GLBBuilder.ConstructFromStream(input, output, streamStartPosition: prefix.Length);
		GLBBuilder.AddBinaryData(glb, new MemoryStream(Encoding.ASCII.GetBytes("ADDED!!!")));
		GLBBuilder.UpdateStream(glb);

		Assert.AreEqual(inputBytes, input.ToArray(), "the input stream must not change");
		var file = ReadGlb(output.ToArray());
		Assert.AreEqual(version, file.Version);
		Assert.AreEqual("BINDATA!ADDED!!!", Encoding.ASCII.GetString(file.Bin(), 0, 16));
	}

	private static GLTFRoot NewRoot() => new GLTFRoot { Asset = new Asset { Version = "2.0" } };

	/// <summary>
	/// Loads a GLB with an optional BIN chunk, which is referenced by one buffer and one bufferView.
	/// </summary>
	private static GLBObject Load(uint version, byte[] bin)
	{
		var json = bin == null
			? "{\"asset\":{\"version\":\"2.0\"}}"
			: "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":" + GlbTestFile.Align(bin.Length, 4) + "}],\"bufferViews\":[{\"buffer\":0,\"byteLength\":" + bin.Length + "}]}";
		var bytes = bin == null
			? GlbTestFile.Write(version, GlbTestFile.Json(json))
			: GlbTestFile.Write(version, GlbTestFile.Json(json), GlbTestFile.Bin(bin));
		var stream = new MemoryStream();
		stream.Write(bytes, 0, bytes.Length);
		return GLBBuilder.ConstructFromStream(stream);
	}

	/// <summary>
	/// Reads the GLB and asserts the layout GLBBuilder writes: one JSON chunk, followed by an optional BIN chunk.
	/// </summary>
	private static GlbTestFile ReadGlb(byte[] bytes)
	{
		var file = GlbTestFile.Read(bytes);
		Assert.That(file.Chunks.Count, Is.InRange(1, 2), "chunk count");
		Assert.AreEqual(GlbTestFile.JsonType, file.Chunks[0].Type, "the JSON chunk must be the first chunk");
		if (file.Chunks.Count > 1) Assert.AreEqual(GlbTestFile.BinType, file.Chunks[1].Type, "the BIN chunk must be the second chunk");
		return file;
	}
}
