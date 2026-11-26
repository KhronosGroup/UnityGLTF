using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityGLTF;

/// <summary>
/// Export of texture images: file type and the export cache, which keeps encoded images between exports.
/// </summary>
public class TextureExportTests
{
	[Test]
	public void Export_WithCache_UsesRequestedFileType()
	{
		var root = new GameObject("Root");
		var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		quad.transform.SetParent(root.transform, false);
		// Opaque and different on every run, so the cache has no entry for it yet
		var texture = new Texture2D(8, 8, TextureFormat.RGB24, false) { name = "Opaque" };
		texture.SetPixels(Enumerable.Range(0, 64).Select(_ => new Color(Random.value, Random.value, Random.value, 1)).ToArray());
		texture.Apply();
		var renderer = quad.GetComponent<Renderer>();
		var material = new Material(renderer.sharedMaterial) { mainTexture = texture };
		renderer.sharedMaterial = material;
		try
		{
			var jpeg = Export(root, useJpeg: true, quality: 90);
			Assert.AreEqual(("image/jpeg", "JPEG"), jpeg.image, "First export");

			// Same texture again, now as PNG: the cached JPEG must not be reused
			var png = Export(root, useJpeg: false, quality: 90);
			Assert.AreEqual(("image/png", "PNG"), png.image, "Second export with the JPEG heuristic turned off");

			// Another JPEG quality must not reuse the cached JPEG either
			var lowQuality = Export(root, useJpeg: true, quality: 20);
			Assert.AreEqual(("image/jpeg", "JPEG"), lowQuality.image, "Export with another JPEG quality");
			CollectionAssert.AreNotEqual(jpeg.bytes, lowQuality.bytes, "Image bytes for JPEG quality 20 and 90 are the same");
		}
		finally
		{
			Object.DestroyImmediate(root);
			Object.DestroyImmediate(material);
			Object.DestroyImmediate(texture);
		}
	}

	/// <summary>
	/// Exports with the cache enabled and returns the mime type and actual file type of the only image, and its bytes.
	/// </summary>
	private static ((string mimeType, string fileType) image, byte[] bytes) Export(GameObject root, bool useJpeg, int quality)
	{
		var settings = ScriptableObject.CreateInstance<GLTFSettings>();
		try
		{
			settings.UseMainCameraVisibility = false;
			settings.UseCaching = true;
			settings.UseTextureFileTypeHeuristic = useJpeg;
			settings.DefaultJpegQuality = quality;
			var glb = new GLTFSceneExporter(root.transform, new ExportContext(settings)).SaveGLBToByteArray("Cache");

			var json = GltfFile.Parse(glb);
			var image = (JObject)json["images"].Single();
			var bufferView = json["bufferViews"][(int)image["bufferView"]];
			var start = 20 + System.BitConverter.ToInt32(glb, 12) + 8 + ((int?)bufferView["byteOffset"] ?? 0);
			var bytes = glb.Skip(start).Take((int)bufferView["byteLength"]).ToArray();
			var fileType = bytes[0] == 0x89 && bytes[1] == 0x50 ? "PNG" : bytes[0] == 0xFF && bytes[1] == 0xD8 ? "JPEG" : "unknown";
			return (((string)image["mimeType"], fileType), bytes);
		}
		finally
		{
			Object.DestroyImmediate(settings);
		}
	}
}
