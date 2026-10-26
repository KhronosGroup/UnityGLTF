using System;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityGLTF;
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
}
