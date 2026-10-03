using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

/// <summary>
/// Compares two glTF files on the JSON level, so values that are undefined in a file can be told apart from defaults.
/// Used to compare an original file with the file exported after importing it.
/// </summary>
internal static class GltfFile
{
	private const float Tolerance = 2e-3f;

	private const int Nearest = 9728, Linear = 9729, NearestMipmapNearest = 9984, LinearMipmapNearest = 9985,
		NearestMipmapLinear = 9986, LinearMipmapLinear = 9987, Repeat = 10497;

	public static JObject Read(string path) => Parse(File.ReadAllBytes(path));

	public static JObject Parse(byte[] bytes)
	{
		if (bytes.Length >= 20 && Encoding.ASCII.GetString(bytes, 0, 4) == "glTF")
			return JObject.Parse(Encoding.UTF8.GetString(bytes, 20, BitConverter.ToInt32(bytes, 12)));
		return JObject.Parse(Encoding.UTF8.GetString(bytes));
	}

	/// <summary>
	/// Reads a float accessor from a GLB file, one array of components per element.
	/// </summary>
	public static float[][] ReadAccessor(byte[] glb, JObject json, int accessorIndex)
	{
		var accessor = json["accessors"][accessorIndex];
		if ((int)accessor["componentType"] != 5126) throw new NotSupportedException("Only float accessors are supported");
		var components = new Dictionary<string, int> { { "SCALAR", 1 }, { "VEC2", 2 }, { "VEC3", 3 }, { "VEC4", 4 } }[(string)accessor["type"]];
		var bufferView = json["bufferViews"][(int)accessor["bufferView"]];
		var stride = (int?)bufferView["byteStride"] ?? components * 4;

		// The binary chunk follows the JSON chunk: [length][type][data]
		var binaryStart = 20 + BitConverter.ToInt32(glb, 12) + 8;
		var start = binaryStart + ((int?)bufferView["byteOffset"] ?? 0) + ((int?)accessor["byteOffset"] ?? 0);
		var result = new float[(int)accessor["count"]][];
		for (var i = 0; i < result.Length; i++)
		{
			result[i] = new float[components];
			for (var c = 0; c < components; c++)
				result[i][c] = BitConverter.ToSingle(glb, start + i * stride + c * 4);
		}
		return result;
	}

	public static List<string> CompareMaterials(JObject original, JObject exported)
	{
		var differences = new List<string>();

		// Match by name first. The importer can merge a root node into the model root (named after the file),
		// so remaining nodes are matched in order.
		var originalNodes = MeshNodes(original);
		var exportedNodes = MeshNodes(exported);
		var pairs = new List<(string name, JObject node, JObject exportedNode)>();
		var unmatched = new List<(string name, JObject node)>();
		foreach (var (name, node) in originalNodes)
		{
			var index = exportedNodes.FindIndex(n => n.name == name);
			if (index < 0)
			{
				unmatched.Add((name, node));
				continue;
			}
			pairs.Add((name, node, exportedNodes[index].node));
			exportedNodes.RemoveAt(index);
		}
		for (var i = 0; i < unmatched.Count; i++)
		{
			if (i < exportedNodes.Count) pairs.Add((unmatched[i].name, unmatched[i].node, exportedNodes[i].node));
			else differences.Add($"\"{unmatched[i].name}\": mesh node missing in the export");
		}

		foreach (var (name, node, exportedNode) in pairs)
		{
			var originalPrimitives = (JArray)original["meshes"][(int)node["mesh"]]["primitives"];
			var exportedPrimitives = (JArray)exported["meshes"][(int)exportedNode["mesh"]]["primitives"];
			if (originalPrimitives.Count != exportedPrimitives.Count)
			{
				differences.Add($"\"{name}\": {originalPrimitives.Count} primitives vs {exportedPrimitives.Count}");
				continue;
			}
			for (var i = 0; i < originalPrimitives.Count; i++)
			{
				var where = $"\"{name}\"[{i}]";
				CompareMaterial(where, original, Material(original, originalPrimitives[i]), exported, Material(exported, exportedPrimitives[i]), differences);
			}
		}
		return differences;
	}

	private static JObject Material(JObject file, JToken primitive)
	{
		var index = primitive["material"];
		return index == null ? new JObject() : (JObject)file["materials"][(int)index];
	}

	/// <summary>
	/// Nodes with a mesh in scene order, with the name the importer gives them.
	/// </summary>
	private static List<(string name, JObject node)> MeshNodes(JObject file)
	{
		var result = new List<(string, JObject)>();
		var nodes = (JArray)file["nodes"] ?? new JArray();
		void Visit(int index)
		{
			var node = (JObject)nodes[index];
			if (node["mesh"] != null) result.Add(((string)node["name"] ?? "GLTFNode" + index, node));
			foreach (var child in (JArray)node["children"] ?? new JArray()) Visit((int)child);
		}
		var scene = file["scenes"]?[(int?)file["scene"] ?? 0];
		foreach (var root in (JArray)scene?["nodes"] ?? new JArray()) Visit((int)root);
		return result;
	}

	private static void CompareMaterial(string where, JObject originalFile, JObject original, JObject exportedFile, JObject exported, List<string> differences)
	{
		var originalName = (string)original["name"];
		if (originalName != null && originalName != (string)exported["name"])
			differences.Add($"{where}: material name \"{originalName}\" vs \"{exported["name"]}\"");
		where += $" ({originalName ?? "unnamed material"})";

		void CompareValue(string property, JToken a, JToken b, float defaultValue)
		{
			var va = a != null ? (float)a : defaultValue;
			var vb = b != null ? (float)b : defaultValue;
			if (Math.Abs(va - vb) > Tolerance) differences.Add($"{where} {property}: {Format(va)} vs {Format(vb)}");
		}
		void CompareArray(string property, float[] a, float[] b)
		{
			if (a.Zip(b, (x, y) => Math.Abs(x - y)).Any(d => d > Tolerance))
				differences.Add($"{where} {property}: [{string.Join(", ", a.Select(Format))}] vs [{string.Join(", ", b.Select(Format))}]");
		}
		float[] Array(JToken token, params float[] defaultValue) => token != null ? token.Select(x => (float)x).ToArray() : defaultValue;

		var alphaMode = (string)original["alphaMode"] ?? "OPAQUE";
		if (alphaMode != ((string)exported["alphaMode"] ?? "OPAQUE"))
			differences.Add($"{where} alphaMode: {alphaMode} vs {(string)exported["alphaMode"] ?? "OPAQUE"}");
		else if (alphaMode == "MASK")
			CompareValue("alphaCutoff", original["alphaCutoff"], exported["alphaCutoff"], 0.5f);
		if (((bool?)original["doubleSided"] ?? false) != ((bool?)exported["doubleSided"] ?? false))
			differences.Add($"{where} doubleSided: {(bool?)original["doubleSided"] ?? false} vs {(bool?)exported["doubleSided"] ?? false}");

		var originalPbr = original["pbrMetallicRoughness"];
		var exportedPbr = exported["pbrMetallicRoughness"];
		CompareArray("baseColorFactor", Array(originalPbr?["baseColorFactor"], 1, 1, 1, 1), Array(exportedPbr?["baseColorFactor"], 1, 1, 1, 1));
		CompareValue("metallicFactor", originalPbr?["metallicFactor"], exportedPbr?["metallicFactor"], 1);
		CompareValue("roughnessFactor", originalPbr?["roughnessFactor"], exportedPbr?["roughnessFactor"], 1);

		float[] Emission(JObject material)
		{
			var strength = (float?)material["extensions"]?["KHR_materials_emissive_strength"]?["emissiveStrength"] ?? 1;
			return Array(material["emissiveFactor"], 0, 0, 0).Select(x => x * strength).ToArray();
		}
		CompareArray("emission (emissiveFactor * emissiveStrength)", Emission(original), Emission(exported));

		var originalUnlit = original["extensions"]?["KHR_materials_unlit"] != null;
		if (originalUnlit != (exported["extensions"]?["KHR_materials_unlit"] != null))
			differences.Add($"{where} KHR_materials_unlit: {originalUnlit} vs {!originalUnlit}");

		CompareTexture(where + " baseColorTexture", originalFile, originalPbr?["baseColorTexture"], exportedFile, exportedPbr?["baseColorTexture"], differences);
		CompareTexture(where + " metallicRoughnessTexture", originalFile, originalPbr?["metallicRoughnessTexture"], exportedFile, exportedPbr?["metallicRoughnessTexture"], differences);
		CompareTexture(where + " normalTexture", originalFile, original["normalTexture"], exportedFile, exported["normalTexture"], differences);
		CompareTexture(where + " occlusionTexture", originalFile, original["occlusionTexture"], exportedFile, exported["occlusionTexture"], differences);
		CompareTexture(where + " emissiveTexture", originalFile, original["emissiveTexture"], exportedFile, exported["emissiveTexture"], differences);
		if (original["normalTexture"] != null && exported["normalTexture"] != null)
			CompareValue("normalTexture.scale", original["normalTexture"]["scale"], exported["normalTexture"]["scale"], 1);
		if (original["occlusionTexture"] != null && exported["occlusionTexture"] != null)
			CompareValue("occlusionTexture.strength", original["occlusionTexture"]["strength"], exported["occlusionTexture"]["strength"], 1);
	}

	private static void CompareTexture(string where, JObject originalFile, JToken original, JObject exportedFile, JToken exported, List<string> differences)
	{
		if (original == null || exported == null)
		{
			if ((original == null) != (exported == null))
				differences.Add($"{where}: {(original == null ? "none" : "texture")} vs {(exported == null ? "none" : "texture")}");
			return;
		}

		// KHR_texture_transform can override the texCoord
		var originalTransform = original["extensions"]?["KHR_texture_transform"];
		var exportedTransform = exported["extensions"]?["KHR_texture_transform"];
		var originalTexCoord = (int?)originalTransform?["texCoord"] ?? (int?)original["texCoord"] ?? 0;
		var exportedTexCoord = (int?)exportedTransform?["texCoord"] ?? (int?)exported["texCoord"] ?? 0;
		if (originalTexCoord != exportedTexCoord) differences.Add($"{where} texCoord: {originalTexCoord} vs {exportedTexCoord}");

		string Transform(JToken t) => t == null ? "none" :
			$"offset [{string.Join(", ", (t["offset"] ?? new JArray(0, 0)).Select(x => Format((float)x)))}] " +
			$"rotation {Format((float?)t["rotation"] ?? 0)} scale [{string.Join(", ", (t["scale"] ?? new JArray(1, 1)).Select(x => Format((float)x)))}]";
		var identity = Transform(new JObject());
		var a = Transform(originalTransform);
		var b = Transform(exportedTransform);
		if ((a == "none" ? identity : a) != (b == "none" ? identity : b)) differences.Add($"{where} KHR_texture_transform: {a} vs {b}");

		JToken Sampler(JObject file, JToken textureInfo)
		{
			var samplerIndex = file["textures"][(int)textureInfo["index"]]["sampler"];
			return samplerIndex == null ? null : file["samplers"][(int)samplerIndex];
		}
		var originalSampler = Sampler(originalFile, original);
		var exportedSampler = Sampler(exportedFile, exported);

		foreach (var wrap in new[] { "wrapS", "wrapT" })
		{
			var wa = (int?)originalSampler?[wrap] ?? Repeat;
			var wb = (int?)exportedSampler?[wrap] ?? Repeat;
			if (wa != wb) differences.Add($"{where} {wrap}: {wa} vs {wb}");
		}

		// Filters have no default in the spec: undefined means "auto filtering", so any exported value is fine
		var originalMag = (int?)originalSampler?["magFilter"];
		var exportedMag = (int?)exportedSampler?["magFilter"];
		if (originalMag != null && originalMag != exportedMag)
			differences.Add($"{where} magFilter: {FilterName(originalMag)} vs {FilterName(exportedMag)}");

		var originalMin = (int?)originalSampler?["minFilter"];
		var exportedMin = (int?)exportedSampler?["minFilter"];
		// Unity limitations:
		// - it can't use the nearest texel within a mipmap while blending between mipmaps, Trilinear is the closest
		// - it always generates mipmaps, so a minFilter without mipmaps comes back as its mipmap variant
		var unityLimitation = originalMin == NearestMipmapLinear && exportedMin == LinearMipmapLinear ||
		                      originalMin == Linear && exportedMin == LinearMipmapNearest ||
		                      originalMin == Nearest && exportedMin == NearestMipmapNearest;
		if (originalMin != null && originalMin != exportedMin && !unityLimitation)
			differences.Add($"{where} minFilter: {FilterName(originalMin)} vs {FilterName(exportedMin)}");
	}

	private static string Format(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

	private static string FilterName(int? filter)
	{
		switch (filter)
		{
			case null: return "undefined";
			case Nearest: return "NEAREST";
			case Linear: return "LINEAR";
			case NearestMipmapNearest: return "NEAREST_MIPMAP_NEAREST";
			case LinearMipmapNearest: return "LINEAR_MIPMAP_NEAREST";
			case NearestMipmapLinear: return "NEAREST_MIPMAP_LINEAR";
			case LinearMipmapLinear: return "LINEAR_MIPMAP_LINEAR";
			default: return filter.ToString();
		}
	}
}
