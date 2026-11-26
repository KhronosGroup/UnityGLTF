using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

/// <summary>
/// Writes and reads GLB files independently of GLTFParser and GLBBuilder, following the binary format
/// version 2 (glTF 2.0) and version 3 (glTF 2.1 draft) layouts.
/// </summary>
public class GlbTestFile
{
	public const uint Magic = 0x46546C67, JsonType = 0x4E4F534A, BinType = 0x004E4942;

	public uint Version;
	public List<Chunk> Chunks = new List<Chunk>();

	public struct Chunk
	{
		public uint Type, Encoding;
		public byte[] Data;

		public Chunk(uint type, byte[] data, uint encoding = 0)
		{
			Type = type;
			Data = data;
			Encoding = encoding;
		}
	}

	public static Chunk Json(string json) => new Chunk(JsonType, Encoding.UTF8.GetBytes(json));
	public static Chunk Bin(byte[] data, uint encoding = 0) => new Chunk(BinType, data, encoding);

	public byte[] Bin() => Chunks.Single(c => c.Type == BinType).Data;
	public JObject ParseJson() => JObject.Parse(Encoding.UTF8.GetString(Chunks.First(c => c.Type == JsonType).Data));

	public static long Align(long value, long alignment) => (value + alignment - 1) / alignment * alignment;

	/// <summary>
	/// Version 2 pads each chunk's content to 4 bytes (JSON with spaces, others with zeros), as the end of each chunk has to be aligned.
	/// Version 3 writes the unpadded content length and pads with zeros after the chunk, as only the start of each chunk has to be aligned.
	/// </summary>
	public static byte[] Write(uint version, params Chunk[] chunks)
	{
		var body = new MemoryStream();
		var writer = new BinaryWriter(body);
		foreach (var chunk in chunks)
		{
			if (version == 2)
			{
				var padded = Align(chunk.Data.Length, 4);
				writer.Write((uint)padded);
				writer.Write(chunk.Type);
				writer.Write(chunk.Data);
				for (var i = chunk.Data.Length; i < padded; i++) writer.Write(chunk.Type == JsonType ? (byte)' ' : (byte)0);
			}
			else
			{
				writer.Write(chunk.Type);
				writer.Write(chunk.Encoding);
				writer.Write((ulong)chunk.Data.Length);
				writer.Write(chunk.Data);
				for (var i = chunk.Data.Length; i < Align(chunk.Data.Length, 8); i++) writer.Write((byte)0);
			}
		}

		var headerSize = version == 2 ? 12 : 16;
		var result = new MemoryStream();
		var resultWriter = new BinaryWriter(result);
		resultWriter.Write(Magic);
		resultWriter.Write(version);
		if (version == 2) resultWriter.Write((uint)(headerSize + body.Length));
		else resultWriter.Write((ulong)(headerSize + body.Length));
		resultWriter.Write(body.ToArray());
		return result.ToArray();
	}

	/// <summary>
	/// Reads a GLB file and asserts that its headers, chunk layout, alignment and padding follow the binary format of its version.
	/// </summary>
	public static GlbTestFile Read(byte[] bytes)
	{
		var reader = new BinaryReader(new MemoryStream(bytes));
		Assert.AreEqual(Magic, reader.ReadUInt32(), "magic");
		var file = new GlbTestFile { Version = reader.ReadUInt32() };
		Assert.That(file.Version, Is.EqualTo(2).Or.EqualTo(3), "version");
		var v2 = file.Version == 2;
		var length = v2 ? reader.ReadUInt32() : (long)reader.ReadUInt64();
		Assert.AreEqual(bytes.Length, length, "file length in header");
		var alignment = v2 ? 4 : 8;

		while (reader.BaseStream.Position < bytes.Length)
		{
			var chunk = new Chunk();
			long chunkLength;
			if (v2)
			{
				chunkLength = reader.ReadUInt32();
				chunk.Type = reader.ReadUInt32();
			}
			else
			{
				chunk.Type = reader.ReadUInt32();
				chunk.Encoding = reader.ReadUInt32();
				chunkLength = (long)reader.ReadUInt64();
			}
			// Version 2: start and end of each chunk are 4-byte aligned. Version 3: only the start of each chunk is 8-byte aligned.
			if (v2) Assert.AreEqual(0, chunkLength % alignment, $"length of chunk {file.Chunks.Count} must be {alignment}-byte aligned");
			Assert.LessOrEqual(reader.BaseStream.Position + chunkLength, bytes.Length, $"chunk {file.Chunks.Count} exceeds file length");
			chunk.Data = reader.ReadBytes((int)chunkLength);
			file.Chunks.Add(chunk);
			var padding = (int)(Align(reader.BaseStream.Position, alignment) - reader.BaseStream.Position);
			Assert.That(reader.ReadBytes(padding), Is.All.EqualTo(0), $"padding after chunk {file.Chunks.Count - 1}");
		}
		return file;
	}
}
