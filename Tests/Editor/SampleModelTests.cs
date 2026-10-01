using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityGLTF;
using Object = UnityEngine.Object;

/// <summary>
/// Import and roundtrip tests (import, export, import) with models from the official Khronos glTF-Sample-Models repository.
/// The models are downloaded on first use and cached in Library/UnityGLTFTests/SampleModels; without a connection the tests are ignored.
/// The visual comparison renders the original and the roundtrip import from the same camera and writes
/// [original | roundtrip | difference] images and the exported files to Logs/UnityGLTFTests/SampleModels.
/// </summary>
[Category("SampleModels")]
public class SampleModelTests
{
	private const string BaseUrl = "https://raw.githubusercontent.com/KhronosGroupArchives/glTF-Sample-Models/main/2.0";
	private const string CacheFolder = "Library/UnityGLTFTests/SampleModels";
	private const string OutputFolder = "Logs/UnityGLTFTests/SampleModels";
	private const string TempFolder = "Assets/UnityGLTFTests_SampleModels";

	private const int ImageSize = 256;
	// A pixel counts as different when one channel differs by more than this (0-255)
	private const int PixelThreshold = 24;
	// Maximum fraction of different pixels, leaves room for anti-aliasing differences at edges
	private const float MaxDifferentPixels = 0.002f;

	// Known failures, caused by export settings rather than bugs:
	// - Duck, 2CylinderEngine, MultiUVTest (Roundtrip_KeepsHierarchyMeshesAndAnimations): cameras are imported disabled by default
	//   (CameraImportOption.ImportAndCameraDisabled) and disabled cameras aren't exported, so the camera and its 180° flip are lost.
	// - NormalTangentTest (Roundtrip_LooksTheSame, with URP): textures without alpha are exported as JPEG by default
	//   (UseTextureFileTypeHeuristic), and the JPEG artifacts in the normal map change the highlights. As PNG, the export looks the same.
	// - Texture filters (Roundtrip_KeepsMaterials, Roundtrip_ExportMatchesOriginalFile): magFilter LINEAR isn't written to the file,
	//   and Bilinear textures are exported as LINEAR_MIPMAP_LINEAR because every anisoLevel above 0 counts as anisotropic filtering.
	public static readonly string[] Samples =
	{
		// Skins, animations and morph targets
		"RiggedSimple",
		"RiggedFigure",
		"CesiumMan",
		"Fox",
		"BrainStem",
		"RecursiveSkeletons",
		"BoxAnimated",
		"InterpolationTest",
		"AnimatedMorphCube",
		"MorphPrimitivesTest",
		"SuzanneMorphSparse",
		// Geometry and hierarchy
		"Box",
		"BoxInterleaved",
		"BoxVertexColors",
		"Duck",
		"OrientationTest",
		"NegativeScaleTest",
		"SimpleInstancing",
		"2CylinderEngine",
		"CesiumMilkTruck",
		// Materials and textures
		"BoxTextured",
		"DamagedHelmet",
		"MetalRoughSpheresNoTextures",
		"AlphaBlendModeTest",
		"TextureCoordinateTest",
		"TextureSettingsTest",
		"MultiUVTest",
		"VertexColorTest",
		"NormalTangentTest",
		"UnlitTest",
		"EmissiveStrengthTest",
		"TextureTransformMultiTest",
	};

	private static readonly Dictionary<string, (string path, string roundtripPath)> Imported = new Dictionary<string, (string, string)>();

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		if (!AssetDatabase.IsValidFolder(TempFolder))
			AssetDatabase.CreateFolder("Assets", Path.GetFileName(TempFolder));
		Directory.CreateDirectory(OutputFolder);
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		Imported.Clear();
		AssetDatabase.DeleteAsset(TempFolder);
	}

	[TestCaseSource(nameof(Samples))]
	public void Import_CreatesModel(string sample)
	{
		var path = Import(sample);
		var model = RigImportTests.LoadModel(path);
		Assert.IsNotEmpty(model.GetComponentsInChildren<Renderer>(true), "No renderers imported");
	}

	[TestCaseSource(nameof(Samples))]
	public void Roundtrip_KeepsHierarchyMeshesAndAnimations(string sample)
	{
		var (path, roundtripPath) = ImportAndRoundtrip(sample);
		RigImportTests.AssertHierarchyEqual(path, roundtripPath);
		RigImportTests.AssertMeshesEqual(path, roundtripPath);
		RigImportTests.AssertAnimationsEqual(path, roundtripPath);
	}

	[TestCaseSource(nameof(Samples))]
	public void Roundtrip_KeepsMaterials(string sample)
	{
		var (path, roundtripPath) = ImportAndRoundtrip(sample);
		var expected = MaterialsBySlot(path);
		var actual = MaterialsBySlot(roundtripPath);
		CollectionAssert.AreEquivalent(expected.Keys, actual.Keys, "Material slots differ after the roundtrip");

		var differences = new List<string>();
		foreach (var pair in expected)
		{
			var e = pair.Value;
			var a = actual[pair.Key];
			if (!e || !a)
			{
				if ((bool)e != (bool)a) differences.Add($"{pair.Key}: material missing");
				continue;
			}
			if (e.shader != a.shader)
			{
				differences.Add($"{pair.Key}: shader {e.shader.name} vs {a.shader.name}");
				continue;
			}

			var shader = e.shader;
			for (var i = 0; i < shader.GetPropertyCount(); i++)
			{
				var name = shader.GetPropertyName(i);
				var where = $"{pair.Key} ({e.name}) {name}";
				switch (shader.GetPropertyType(i))
				{
					case UnityEngine.Rendering.ShaderPropertyType.Color:
					case UnityEngine.Rendering.ShaderPropertyType.Vector:
						var ev = e.GetVector(name);
						var av = a.GetVector(name);
						if ((ev - av).magnitude > 0.01f) differences.Add($"{where}: {ev} vs {av}");
						break;
					case UnityEngine.Rendering.ShaderPropertyType.Float:
					case UnityEngine.Rendering.ShaderPropertyType.Range:
						var ef = e.GetFloat(name);
						var af = a.GetFloat(name);
						if (Mathf.Abs(ef - af) > 0.01f) differences.Add($"{where}: {ef} vs {af}");
						break;
					case UnityEngine.Rendering.ShaderPropertyType.Texture:
						var et = e.GetTexture(name);
						var at = a.GetTexture(name);
						if ((bool)et != (bool)at)
						{
							differences.Add($"{where}: texture {(et ? et.name : "none")} vs {(at ? at.name : "none")}");
							break;
						}
						if (!et) break;
						if (et.filterMode != at.filterMode) differences.Add($"{where}: filter mode {et.filterMode} vs {at.filterMode}");
						if (et.wrapModeU != at.wrapModeU || et.wrapModeV != at.wrapModeV)
							differences.Add($"{where}: wrap mode {et.wrapModeU}x{et.wrapModeV} vs {at.wrapModeU}x{at.wrapModeV}");
						if (e.GetTextureScale(name) != a.GetTextureScale(name) || e.GetTextureOffset(name) != a.GetTextureOffset(name))
							differences.Add($"{where}: tiling {e.GetTextureScale(name)}/{e.GetTextureOffset(name)} vs {a.GetTextureScale(name)}/{a.GetTextureOffset(name)}");
						break;
				}
			}
		}
		Assert.IsEmpty(differences, $"{sample}: material differences after the roundtrip:\n" + string.Join("\n", differences));
	}

	/// <summary>
	/// Compares the original file with the exported file (not the Unity import results), following the glTF spec:
	/// undefined values are compared as their spec defaults, and undefined filters ("auto filtering") accept any value.
	/// Material slots are matched through the mesh nodes, by name (unnamed nodes are imported as "GLTFNode{index}").
	/// </summary>
	[TestCaseSource(nameof(Samples))]
	public void Roundtrip_ExportMatchesOriginalFile(string sample)
	{
		var (_, roundtripPath) = ImportAndRoundtrip(sample);
		var original = GltfFile.Read(Download(sample));
		var exported = GltfFile.Read(roundtripPath);
		var differences = GltfFile.CompareMaterials(original, exported);
		Assert.IsEmpty(differences, $"{sample}: differences between the original and the exported file:\n" + string.Join("\n", differences));
	}

	[TestCaseSource(nameof(Samples))]
	public void Roundtrip_LooksTheSame(string sample)
	{
		var (path, roundtripPath) = ImportAndRoundtrip(sample);

		// Animated models are compared in a pose from the middle of the first clip, so skins and animations are part of it
		var clip = RigImportTests.LoadClips(path).OrderBy(c => c.name).FirstOrDefault();
		var roundtripClip = clip ? RigImportTests.LoadClips(roundtripPath).FirstOrDefault(c => c.name == clip.name) : null;
		// On a time the exporter samples at (length / ceil(length * 30), see BakePropertyAnimation),
		// so the comparison doesn't depend on the interpolation between baked frames
		const float exportFrameRate = 30;
		var time = 0f;
		if (clip && clip.length > 0)
		{
			var intervals = Mathf.CeilToInt(clip.length * exportFrameRate);
			time = Mathf.Round(intervals * 0.37f) * clip.length / intervals;
		}

		var expected = Render(RigImportTests.LoadModel(path), clip, time, null, out var bounds);
		var actual = Render(RigImportTests.LoadModel(roundtripPath), roundtripClip, time, bounds, out _);
		try
		{
			var different = Compare(expected, actual, $"{OutputFolder}/{sample}.png", out var empty);
			var message = $"{sample}: {different:P2} of the pixels differ after the roundtrip, see {Path.GetFullPath($"{OutputFolder}/{sample}.png")}";
			Debug.Log(message);
			Assert.Less(empty, 0.99f, $"{sample}: the original renders empty, nothing to compare");
			Assert.LessOrEqual(different, MaxDifferentPixels, message);
		}
		finally
		{
			Object.DestroyImmediate(expected);
			Object.DestroyImmediate(actual);
		}
	}

	#region Helpers

	private static Dictionary<string, Material> MaterialsBySlot(string path)
	{
		var model = RigImportTests.LoadModel(path);
		var result = new Dictionary<string, Material>();
		foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
		{
			var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, model.transform);
			var materials = renderer.sharedMaterials;
			for (var i = 0; i < materials.Length; i++)
				result[$"\"{rendererPath}\"[{i}]"] = materials[i];
		}
		return result;
	}

	private static string Download(string sample)
	{
		var cachePath = Path.GetFullPath($"{CacheFolder}/{sample}.glb");
		if (File.Exists(cachePath)) return cachePath;

		var url = $"{BaseUrl}/{sample}/glTF-Binary/{sample}.glb";
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
			using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) })
			{
				var bytes = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
				File.WriteAllBytes(cachePath + ".tmp", bytes);
				File.Move(cachePath + ".tmp", cachePath);
			}
		}
		catch (Exception e)
		{
			Assert.Ignore($"Could not download {url}: {e.Message}");
		}
		return cachePath;
	}

	private static string Import(string sample)
	{
		if (Imported.TryGetValue(sample, out var imported)) return imported.path;

		var path = $"{TempFolder}/{sample}.glb";
		File.Copy(Download(sample), path, true);
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
		RigImportTests.Reimport(path, AnimationMethod.Mecanim);
		Imported[sample] = (path, null);
		return path;
	}

	private static (string path, string roundtripPath) ImportAndRoundtrip(string sample)
	{
		var path = Import(sample);
		var roundtripPath = Imported[sample].roundtripPath;
		if (roundtripPath == null)
		{
			roundtripPath = RigImportTests.ExportAndReimport(path, AnimationMethod.Mecanim, "");
			Imported[sample] = (path, roundtripPath);
			// Keep the exported file, e.g. to run the glTF Validator on it
			File.Copy(roundtripPath, $"{OutputFolder}/{sample}_Roundtrip.glb", true);
		}
		return (path, roundtripPath);
	}

	/// <summary>
	/// Renders the model in a preview scene. Without bounds the camera frames the model and returns the bounds it used.
	/// </summary>
	private static Texture2D Render(GameObject model, AnimationClip clip, float time, Bounds? framing, out Bounds bounds)
	{
		var preview = new PreviewRenderUtility();
		var materialCopies = new List<Material>();
		try
		{
			var instance = preview.InstantiatePrefabInScene(model);
			if (clip) clip.SampleAnimation(instance, time);

			// Materials with GPU instancing (e.g. from EXT_mesh_gpu_instancing) don't render correctly in preview scenes
			foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
			{
				var materials = renderer.sharedMaterials;
				if (!materials.Any(m => m && m.enableInstancing)) continue;
				for (var i = 0; i < materials.Length; i++)
				{
					if (!materials[i] || !materials[i].enableInstancing) continue;
					materials[i] = new Material(materials[i]) { enableInstancing = false };
					materialCopies.Add(materials[i]);
				}
				renderer.sharedMaterials = materials;
			}

			if (framing.HasValue)
			{
				bounds = framing.Value;
			}
			else
			{
				var renderers = instance.GetComponentsInChildren<Renderer>();
				bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(instance.transform.position, Vector3.one);
				foreach (var r in renderers) bounds.Encapsulate(r.bounds);
			}

			var camera = preview.camera;
			camera.fieldOfView = 30;
			camera.cullingMask = ~0;
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1);
			var radius = Mathf.Max(bounds.extents.magnitude, 1e-3f);
			var distance = radius / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
			// From the front (+Z, like the default glTF camera), a bit from above and the side
			var rotation = Quaternion.Euler(20, 150, 0);
			camera.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * distance, rotation);
			camera.nearClipPlane = Mathf.Max(distance - radius * 1.1f, distance * 0.01f);
			camera.farClipPlane = distance + radius * 1.1f;

			preview.lights[0].intensity = 1.2f;
			preview.lights[0].transform.rotation = Quaternion.Euler(40, 40, 0);
			preview.lights[1].intensity = 0.6f;
			preview.ambientColor = new Color(0.3f, 0.3f, 0.3f, 1);

			preview.BeginStaticPreview(new Rect(0, 0, ImageSize, ImageSize));
			// With URP the first frame can still use stale material properties, so only the second one is kept
			preview.Render(true, false);
			preview.Render(true, false);
			return preview.EndStaticPreview();
		}
		finally
		{
			preview.Cleanup();
			foreach (var material in materialCopies) Object.DestroyImmediate(material);
		}
	}

	/// <summary>
	/// Returns the fraction of different pixels and writes an [expected | actual | difference] image.
	/// </summary>
	private static float Compare(Texture2D expected, Texture2D actual, string imagePath, out float emptyFraction)
	{
		Assert.AreEqual(expected.width, actual.width);
		Assert.AreEqual(expected.height, actual.height);
		int w = expected.width, h = expected.height;
		var e = expected.GetPixels32();
		var a = actual.GetPixels32();
		var background = e[0];

		var image = new Texture2D(w * 3, h, TextureFormat.RGBA32, false);
		var pixels = new Color32[w * 3 * h];
		int different = 0, empty = 0;
		for (var y = 0; y < h; y++)
		for (var x = 0; x < w; x++)
		{
			var i = y * w + x;
			var diff = Mathf.Max(Mathf.Abs(e[i].r - a[i].r), Mathf.Abs(e[i].g - a[i].g), Mathf.Abs(e[i].b - a[i].b));
			if (diff > PixelThreshold) different++;
			if (e[i].r == background.r && e[i].g == background.g && e[i].b == background.b) empty++;

			var gray = (byte)((e[i].r + e[i].g + e[i].b) / 9);
			pixels[y * w * 3 + x] = e[i];
			pixels[y * w * 3 + w + x] = a[i];
			pixels[y * w * 3 + 2 * w + x] = diff > PixelThreshold
				? new Color32(255, (byte)(255 - Mathf.Min(255, diff * 2)), 0, 255)
				: new Color32(gray, gray, gray, 255);
		}
		image.SetPixels32(pixels);
		image.Apply();
		File.WriteAllBytes(imagePath, image.EncodeToPNG());
		Object.DestroyImmediate(image);

		emptyFraction = (float)empty / e.Length;
		return (float)different / e.Length;
	}

	#endregion
}
