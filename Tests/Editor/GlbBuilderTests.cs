using System;
using System.Collections.Generic;
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
	private const uint JsonType = 0x4E4F534A, BinType = 0x004E4942;

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
		CollectionAssert.AreEqual(data, file.Bin.Skip((int)offset).Take(data.Length).ToArray(), "binary data");
		Assert.AreEqual(file.Bin.Length, (long)file.ParseJson()["buffers"][0]["byteLength"], "buffer byteLength");
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
		CollectionAssert.AreEqual(data, file.Bin.Take(data.Length).ToArray(), "binary data");
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
			Assert.AreEqual(expected, Encoding.ASCII.GetString(file.Bin, offset, expected.Length), "data of bufferView with offset " + offset);
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
		CollectionAssert.AreEqual(data, file.Bin.Take(data.Length).ToArray(), "binary data");
		Assert.That(file.Bin.Skip(data.Length), Is.All.EqualTo(0), "binary padding");
		Assert.AreEqual(file.Bin.Length, (long)file.ParseJson()["buffers"][0]["byteLength"], "buffer byteLength");
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
		Assert.AreEqual("AAAAAAAA", Encoding.ASCII.GetString(file.Bin, 0, 8));
		Assert.AreEqual(1, file.ParseJson()["bufferViews"].Count(), "bufferView count");
	}

	private static GLTFRoot NewRoot() => new GLTFRoot { Asset = new Asset { Version = "2.0" } };

	/// <summary>
	/// Loads a GLB with an optional BIN chunk, which is referenced by one buffer and one bufferView.
	/// </summary>
	private static GLBObject Load(uint version, byte[] bin)
	{
		var json = bin == null
			? "{\"asset\":{\"version\":\"2.0\"}}"
			: "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":" + Align(bin.Length, 4) + "}],\"bufferViews\":[{\"buffer\":0,\"byteLength\":" + bin.Length + "}]}";
		var bytes = WriteGlb(version, json, bin);
		var stream = new MemoryStream();
		stream.Write(bytes, 0, bytes.Length);
		return GLBBuilder.ConstructFromStream(stream);
	}

	private static long Align(long value, long alignment) => (value + alignment - 1) / alignment * alignment;

	private static byte[] WriteGlb(uint version, string json, byte[] bin)
	{
		var alignment = version == 2 ? 4 : 8;
		var chunks = new List<(uint type, byte[] data, byte pad)> { (JsonType, Encoding.UTF8.GetBytes(json), (byte)' ') };
		if (bin != null) chunks.Add((BinType, bin, 0));

		var body = new MemoryStream();
		var writer = new BinaryWriter(body);
		foreach (var (type, data, pad) in chunks)
		{
			var padded = Align(data.Length, alignment);
			if (version == 2)
			{
				writer.Write((uint)padded);
				writer.Write(type);
			}
			else
			{
				writer.Write(type);
				writer.Write(0u);
				writer.Write((ulong)padded);
			}
			writer.Write(data);
			for (var i = data.Length; i < padded; i++) writer.Write(pad);
		}

		var headerSize = version == 2 ? 12 : 16;
		var result = new MemoryStream();
		var resultWriter = new BinaryWriter(result);
		resultWriter.Write(0x46546C67u);
		resultWriter.Write(version);
		if (version == 2) resultWriter.Write((uint)(headerSize + body.Length));
		else resultWriter.Write((ulong)(headerSize + body.Length));
		resultWriter.Write(body.ToArray());
		return result.ToArray();
	}

	private class GlbFile
	{
		public uint Version;
		public List<(uint type, uint encoding, byte[] data)> Chunks = new List<(uint, uint, byte[])>();
		public byte[] Bin => Chunks.Single(c => c.type == BinType).data;
		public JObject ParseJson() => JObject.Parse(Encoding.UTF8.GetString(Chunks[0].data));
	}

	/// <summary>
	/// Reads a GLB file and asserts that its headers, chunk layout and alignment follow the binary format of its version.
	/// </summary>
	private static GlbFile ReadGlb(byte[] bytes)
	{
		var reader = new BinaryReader(new MemoryStream(bytes));
		Assert.AreEqual(0x46546C67u, reader.ReadUInt32(), "magic");
		var file = new GlbFile { Version = reader.ReadUInt32() };
		Assert.That(file.Version, Is.EqualTo(2).Or.EqualTo(3), "version");
		var v2 = file.Version == 2;
		var length = v2 ? reader.ReadUInt32() : (long)reader.ReadUInt64();
		Assert.AreEqual(bytes.Length, length, "file length in header");
		var alignment = v2 ? 4 : 8;

		while (reader.BaseStream.Position < bytes.Length)
		{
			uint type, encoding = 0;
			long chunkLength;
			if (v2)
			{
				chunkLength = reader.ReadUInt32();
				type = reader.ReadUInt32();
			}
			else
			{
				type = reader.ReadUInt32();
				encoding = reader.ReadUInt32();
				chunkLength = (long)reader.ReadUInt64();
			}
			Assert.That(type, Is.EqualTo(JsonType).Or.EqualTo(BinType), $"type of chunk {file.Chunks.Count}");
			Assert.AreEqual(file.Chunks.Count == 0, type == JsonType, "the JSON chunk must be the first chunk");
			// Version 2: start and end of each chunk are 4-byte aligned. Version 3: only the start of each chunk is 8-byte aligned.
			if (v2) Assert.AreEqual(0, chunkLength % alignment, $"length of chunk {file.Chunks.Count} must be {alignment}-byte aligned");
			Assert.LessOrEqual(reader.BaseStream.Position + chunkLength, bytes.Length, $"chunk {file.Chunks.Count} exceeds file length");
			file.Chunks.Add((type, encoding, reader.ReadBytes((int)chunkLength)));
			var padding = (int)(Align(reader.BaseStream.Position, alignment) - reader.BaseStream.Position);
			Assert.That(reader.ReadBytes(padding), Is.All.EqualTo(type == JsonType ? (byte)' ' : (byte)0), $"padding after chunk {file.Chunks.Count - 1}");
		}
		Assert.That(file.Chunks.Count, Is.InRange(1, 2), "chunk count");
		return file;
	}
}
