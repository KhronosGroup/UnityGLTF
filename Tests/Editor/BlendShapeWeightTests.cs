using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityGLTF;
using Object = UnityEngine.Object;

/// <summary>
/// Blend shape weights exported as glTF morph target weights. glTF has a single frame per morph target, so the exporter
/// writes the last frame of a blend shape and the weights (static and animated) have to be relative to that frame's weight.
/// https://github.com/KhronosGroup/UnityGLTF/issues/899
/// Roundtrips use Tests/Assets/MorphTargets/MorphTargets_Weights.gltf: a quad with the morph targets "Up" and "Wide",
/// mesh weights [0.25, 0.75] and an animation of the weights from [0, 0] to [1, 0.5] in 1s.
/// </summary>
public class BlendShapeWeightTests
{
	private const string SourceFolder = "Packages/org.khronos.unitygltf/Tests/Assets/MorphTargets";
	private const string TempFolder = "Assets/UnityGLTFTests_MorphTargets";
	private const string MorphTargetsFile = "MorphTargets_Weights.gltf";
	private const float Tolerance = 1e-4f;
	private const float VertexTolerance = 1e-4f;

	private static readonly float[] FileWeights = { 0.25f, 0.75f };

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		if (!AssetDatabase.IsValidFolder(TempFolder))
			AssetDatabase.CreateFolder("Assets", Path.GetFileName(TempFolder));
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		AssetDatabase.DeleteAsset(TempFolder);
	}

	#region Blend shapes created in Unity

	// The frames are scaled versions of the last one, so Unity's interpolation between frames is linear
	// and a single glTF morph target looks exactly the same at every weight.
	[TestCase(new[] { 100f }, 55.5555f, 0.555555f, TestName = "SingleFrame")]
	[TestCase(new[] { 16.6666f, 100f }, 55.5555f, 0.555555f, TestName = "TwoFrames_Issue899")]
	[TestCase(new[] { 25f, 50f, 100f }, 75f, 0.75f, TestName = "ThreeFrames")]
	[TestCase(new[] { 25f, 50f }, 40f, 0.8f, TestName = "LastFrameBelow100")]
	[TestCase(new[] { 1f }, 0.25f, 0.25f, TestName = "SingleFrameWeight1")]
	public void Export_BlendShapeFrames_WeightIsRelativeToLastFrame(float[] frameWeights, float weight, float expected)
	{
		var objects = new List<Object>();
		try
		{
			var root = CreateBlendShapeObject(frameWeights, weight, objects, out _);
			var json = GltfFile.Parse(Export(root, objects, "BlendShapeFrames"));

			var weights = json["meshes"].Single(m => m["weights"] != null)["weights"].Select(w => (float)w).ToArray();
			Assert.AreEqual(2, weights.Length, "Morph target weight count");
			Assert.AreEqual(expected, weights[0], Tolerance, "Weight of the multi-frame blend shape");
			// The second blend shape has a single frame at 100, so it must not be affected by the other shape's frames
			Assert.AreEqual(0.3f, weights[1], Tolerance, "Weight of the single-frame blend shape");
		}
		finally
		{
			foreach (var o in objects) Object.DestroyImmediate(o);
		}
	}

	[TestCase(new[] { 100f }, 55.5555f, TestName = "SingleFrame")]
	[TestCase(new[] { 16.6666f, 100f }, 55.5555f, TestName = "TwoFrames_Issue899")]
	[TestCase(new[] { 25f, 50f, 100f }, 75f, TestName = "ThreeFrames")]
	[TestCase(new[] { 25f, 50f }, 40f, TestName = "LastFrameBelow100")]
	public void Roundtrip_BlendShapeFrames_LooksTheSame(float[] frameWeights, float weight)
	{
		var objects = new List<Object>();
		Vector3[] expectedVertices;
		var path = $"{TempFolder}/BlendShapeFrames_{string.Join("_", frameWeights.Select(w => Mathf.RoundToInt(w)))}.glb";
		try
		{
			var root = CreateBlendShapeObject(frameWeights, weight, objects, out var renderer);
			expectedVertices = Bake(renderer);
			File.WriteAllBytes(path, Export(root, objects, Path.GetFileNameWithoutExtension(path)));
		}
		finally
		{
			foreach (var o in objects) Object.DestroyImmediate(o);
		}

		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
		RigImportTests.Reimport(path, AnimationMethod.None);
		var instance = Object.Instantiate(RigImportTests.LoadModel(path));
		try
		{
			var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
			Assert.IsNotNull(renderer, "SkinnedMeshRenderer missing after export and import");
			var mesh = renderer.sharedMesh;
			var index = mesh.GetBlendShapeIndex("Grow");
			Assert.GreaterOrEqual(index, 0, "Blend shape \"Grow\" missing after export and import");
			var lastFrameWeight = mesh.GetBlendShapeFrameWeight(index, mesh.GetBlendShapeFrameCount(index) - 1);
			Assert.AreEqual(weight / frameWeights.Last(), renderer.GetBlendShapeWeight(index) / lastFrameWeight, Tolerance,
				"Blend shape weight relative to the last frame");
			AssertVerticesEqual(expectedVertices, Bake(renderer), "Blend shape result after export and import");
		}
		finally
		{
			Object.DestroyImmediate(instance);
		}
	}

	// "Other" has a single frame at 100, so with a different last frame for "Grow" the blend shapes have different weight ranges
	[TestCase(new[] { 100f }, TestName = "SingleFrame")]
	[TestCase(new[] { 16.6666f, 100f }, TestName = "TwoFrames")]
	[TestCase(new[] { 25f, 50f }, TestName = "LastFrameBelow100")]
	[TestCase(new[] { 50f, 200f }, TestName = "LastFrameAbove100")]
	public void Export_AnimatedBlendShapeFrames_WeightIsRelativeToLastFrame(float[] frameWeights)
	{
		var objects = new List<Object>();
		try
		{
			var root = CreateAnimatedBlendShapeObject(frameWeights, objects, out _);
			var bytes = Export(root, objects, "AnimatedBlendShapeFrames");
			var (times, weights) = ReadWeightAnimation(GltfFile.Parse(bytes), bytes);
			Assert.Greater(times.Length, 1, "Weight animation has no keyframes");
			for (var i = 0; i < times.Length; i++)
			{
				Assert.AreEqual(times[i], weights[i][0], 1e-3f, $"Weight of \"Grow\" at {times[i]:F3}s");
				Assert.AreEqual(0.3f + 0.7f * times[i], weights[i][1], 1e-3f, $"Weight of \"Other\" at {times[i]:F3}s");
			}
		}
		finally
		{
			foreach (var o in objects) Object.DestroyImmediate(o);
		}
	}

	[TestCase(new[] { 100f }, TestName = "SingleFrame")]
	[TestCase(new[] { 16.6666f, 100f }, TestName = "TwoFrames")]
	[TestCase(new[] { 25f, 50f }, TestName = "LastFrameBelow100")]
	[TestCase(new[] { 50f, 200f }, TestName = "LastFrameAbove100")]
	public void Roundtrip_AnimatedBlendShapeFrames_LooksTheSame(float[] frameWeights)
	{
		var sampleTimes = new[] { 0f, 0.25f, 0.5f, 1f };
		var objects = new List<Object>();
		var expectedVertices = new List<Vector3[]>();
		var path = $"{TempFolder}/AnimatedBlendShapeFrames_{string.Join("_", frameWeights.Select(w => Mathf.RoundToInt(w)))}.glb";
		try
		{
			var root = CreateAnimatedBlendShapeObject(frameWeights, objects, out var renderer);
			var clip = (AnimationClip)objects.Single(o => o is AnimationClip);
			foreach (var time in sampleTimes)
			{
				clip.SampleAnimation(root, time);
				expectedVertices.Add(Bake(renderer));
			}
			File.WriteAllBytes(path, Export(root, objects, Path.GetFileNameWithoutExtension(path)));
		}
		finally
		{
			foreach (var o in objects) Object.DestroyImmediate(o);
		}

		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
		RigImportTests.Reimport(path, AnimationMethod.Mecanim);
		var importedClip = RigImportTests.LoadClips(path).SingleOrDefault(c => c.name == "Grow");
		Assert.IsNotNull(importedClip, "Clip \"Grow\" missing after export and import");
		var instance = Object.Instantiate(RigImportTests.LoadModel(path));
		try
		{
			var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
			Assert.IsNotNull(renderer, "SkinnedMeshRenderer missing after export and import");
			for (var i = 0; i < sampleTimes.Length; i++)
			{
				importedClip.SampleAnimation(instance, sampleTimes[i]);
				AssertVerticesEqual(expectedVertices[i], Bake(renderer), $"Blend shape result at {sampleTimes[i]:F2}s after export and import");
			}
		}
		finally
		{
			Object.DestroyImmediate(instance);
		}
	}

	#endregion

	#region Roundtrip of a glTF file

	[TestCase(BlendShapeFrameWeightSetting.MultiplierOption.Multiplier1, 1f)]
	[TestCase(BlendShapeFrameWeightSetting.MultiplierOption.Multiplier100, 100f)]
	public void Import_MorphTargetWeights_UsesFrameWeightMultiplier(BlendShapeFrameWeightSetting.MultiplierOption option, float multiplier)
	{
		var path = ImportMorphTargets(option);
		var renderer = RigImportTests.LoadModel(path).GetComponentInChildren<SkinnedMeshRenderer>();
		Assert.IsNotNull(renderer, "Mesh with morph targets should get a SkinnedMeshRenderer");
		var mesh = renderer.sharedMesh;
		CollectionAssert.AreEqual(new[] { "Up", "Wide" }, Enumerable.Range(0, mesh.blendShapeCount).Select(mesh.GetBlendShapeName), "Blend shape names");
		for (var i = 0; i < FileWeights.Length; i++)
		{
			Assert.AreEqual(1, mesh.GetBlendShapeFrameCount(i), $"Frame count of \"{mesh.GetBlendShapeName(i)}\"");
			Assert.AreEqual(multiplier, mesh.GetBlendShapeFrameWeight(i, 0), Tolerance, $"Frame weight of \"{mesh.GetBlendShapeName(i)}\"");
			Assert.AreEqual(FileWeights[i] * multiplier, renderer.GetBlendShapeWeight(i), Tolerance * multiplier, $"Weight of \"{mesh.GetBlendShapeName(i)}\"");
		}
	}

	[TestCase(BlendShapeFrameWeightSetting.MultiplierOption.Multiplier1)]
	[TestCase(BlendShapeFrameWeightSetting.MultiplierOption.Multiplier100)]
	public void Roundtrip_MorphTargetWeights_KeepsWeightsAndAnimation(BlendShapeFrameWeightSetting.MultiplierOption option)
	{
		var path = ImportMorphTargets(option);
		var roundtripPath = RigImportTests.ExportAndReimport(path, AnimationMethod.Mecanim, "");
		// ExportAndReimport imports with the default multiplier, the comparison needs the same one
		SetFrameWeightMultiplier(roundtripPath, option);
		RigImportTests.Reimport(roundtripPath, AnimationMethod.Mecanim);

		// The exported file has the same weights as the original file, whatever multiplier was used for the import
		var bytes = File.ReadAllBytes(roundtripPath);
		var json = GltfFile.Parse(bytes);
		var weights = json["meshes"].Single(m => m["weights"] != null)["weights"].Select(w => (float)w).ToArray();
		Assert.AreEqual(FileWeights.Length, weights.Length, "Exported morph target weight count");
		for (var i = 0; i < FileWeights.Length; i++)
			Assert.AreEqual(FileWeights[i], weights[i], Tolerance, $"Exported mesh weight {i}");

		var (times, animatedWeights) = ReadWeightAnimation(json, bytes);
		Assert.AreEqual(1f, times.Last(), 1e-3f, "Exported animation length");
		for (var i = 0; i < times.Length; i++)
		{
			Assert.AreEqual(times[i], animatedWeights[i][0], 1e-3f, $"Exported weight of \"Up\" at {times[i]:F3}s");
			Assert.AreEqual(times[i] * 0.5f, animatedWeights[i][1], 1e-3f, $"Exported weight of \"Wide\" at {times[i]:F3}s");
		}

		RigImportTests.AssertHierarchyEqual(path, roundtripPath);
		RigImportTests.AssertMeshesEqual(path, roundtripPath);
		RigImportTests.AssertAnimationsEqual(path, roundtripPath);

		var original = Object.Instantiate(RigImportTests.LoadModel(path));
		var roundtrip = Object.Instantiate(RigImportTests.LoadModel(roundtripPath));
		try
		{
			var originalRenderer = original.GetComponentInChildren<SkinnedMeshRenderer>();
			var roundtripRenderer = roundtrip.GetComponentInChildren<SkinnedMeshRenderer>();
			for (var i = 0; i < FileWeights.Length; i++)
				Assert.AreEqual(originalRenderer.GetBlendShapeWeight(i), roundtripRenderer.GetBlendShapeWeight(i), Tolerance * 100,
					$"Weight of \"{originalRenderer.sharedMesh.GetBlendShapeName(i)}\" after the roundtrip");
			AssertVerticesEqual(Bake(originalRenderer), Bake(roundtripRenderer), "Blend shape result after the roundtrip");
		}
		finally
		{
			Object.DestroyImmediate(original);
			Object.DestroyImmediate(roundtrip);
		}
	}

	#endregion

	#region Helpers

	/// <summary>
	/// A cube with the blend shape "Grow" (one frame per weight, scaled versions of the last frame) and
	/// the single-frame blend shape "Other" with a weight of 30.
	/// </summary>
	private static GameObject CreateBlendShapeObject(float[] frameWeights, float weight, List<Object> objects, out SkinnedMeshRenderer renderer)
	{
		var mesh = Object.Instantiate(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
		mesh.name = "Cube";
		objects.Add(mesh);
		var last = frameWeights.Last();
		foreach (var frameWeight in frameWeights)
			mesh.AddBlendShapeFrame("Grow", frameWeight, mesh.vertices.Select(v => v * (0.5f * frameWeight / last)).ToArray(), null, null);
		mesh.AddBlendShapeFrame("Other", 100, mesh.vertices.Select(v => new Vector3(v.x * 0.25f, 0, 0)).ToArray(), null, null);

		var root = new GameObject("Root");
		objects.Add(root);
		var child = new GameObject("BlendShapes");
		child.transform.SetParent(root.transform, false);
		renderer = child.AddComponent<SkinnedMeshRenderer>();
		renderer.sharedMesh = mesh;
		renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
		renderer.SetBlendShapeWeight(0, weight);
		renderer.SetBlendShapeWeight(1, 30);
		return root;
	}

	/// <summary>
	/// <see cref="CreateBlendShapeObject"/> with an Animator playing the clip "Grow": in 1s, "Grow" goes from 0 to its last frame's weight
	/// and "Other" from 30 to 100. The clip is added to <paramref name="objects"/>.
	/// </summary>
	private static GameObject CreateAnimatedBlendShapeObject(float[] frameWeights, List<Object> objects, out SkinnedMeshRenderer renderer)
	{
		var root = CreateBlendShapeObject(frameWeights, 0, objects, out renderer);
		var clip = new AnimationClip { name = "Grow" };
		objects.Add(clip);
		clip.SetCurve(renderer.name, typeof(SkinnedMeshRenderer), "blendShape.Grow", AnimationCurve.Linear(0, 0, 1, frameWeights.Last()));
		clip.SetCurve(renderer.name, typeof(SkinnedMeshRenderer), "blendShape.Other", AnimationCurve.Linear(0, 30, 1, 100));
		var controller = new AnimatorController { name = "Grow" };
		objects.Add(controller);
		controller.AddLayer("Base Layer");
		controller.layers[0].stateMachine.AddState(clip.name).motion = clip;
		root.AddComponent<Animator>().runtimeAnimatorController = controller;
		return root;
	}

	private static byte[] Export(GameObject root, List<Object> objects, string name)
	{
		var settings = ScriptableObject.CreateInstance<GLTFSettings>();
		objects.Add(settings);
		settings.UseMainCameraVisibility = false;
		settings.ExportAnimations = true;
		var exporter = new GLTFSceneExporter(root.transform, new ExportContext(settings));
		return exporter.SaveGLBToByteArray(name);
	}

	/// <summary>
	/// Copies the test file into the temp folder (one copy per multiplier) and imports it with Mecanim animations.
	/// </summary>
	private static string ImportMorphTargets(BlendShapeFrameWeightSetting.MultiplierOption option)
	{
		var path = $"{TempFolder}/{Path.GetFileNameWithoutExtension(MorphTargetsFile)}_{option}.gltf";
		if (!File.Exists(path))
		{
			var source = FileUtil.GetPhysicalPath($"{SourceFolder}/{MorphTargetsFile}");
			Assert.IsTrue(File.Exists(source), $"Test asset not found: {source}");
			File.Copy(source, path, true);
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
		}
		SetFrameWeightMultiplier(path, option);
		RigImportTests.Reimport(path, AnimationMethod.Mecanim);
		return path;
	}

	/// <summary>
	/// Changes the importer's blend shape frame weight setting without reimporting.
	/// </summary>
	private static void SetFrameWeightMultiplier(string path, BlendShapeFrameWeightSetting.MultiplierOption option)
	{
		var importer = AssetImporter.GetAtPath(path) as GLTFImporter;
		Assert.IsNotNull(importer, $"{path} is not imported with the {nameof(GLTFImporter)}");
		var so = new SerializedObject(importer);
		so.FindProperty("_blendShapeFrameWeight._option").enumValueIndex = (int)option;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	/// <summary>
	/// Reads the (only) morph target weight animation: keyframe times and the weights per keyframe.
	/// </summary>
	private static (float[] times, float[][] weights) ReadWeightAnimation(JObject json, byte[] glb)
	{
		var channels = (json["animations"] ?? new JArray())
			.SelectMany(a => a["channels"].Select(c => (animation: a, channel: c)))
			.Where(x => (string)x.channel["target"]?["path"] == "weights" ||
			            ((string)x.channel["target"]?["extensions"]?["KHR_animation_pointer"]?["pointer"])?.EndsWith("/weights") == true)
			.ToList();
		Assert.AreEqual(1, channels.Count, "Morph target weight animation channels");

		var (animation, channel) = channels[0];
		var sampler = animation["samplers"][(int)channel["sampler"]];
		var times = GltfFile.ReadAccessor(glb, json, (int)sampler["input"]).Select(t => t[0]).ToArray();
		var values = GltfFile.ReadAccessor(glb, json, (int)sampler["output"]).Select(v => v[0]).ToArray();
		// CUBICSPLINE stores in-tangent, value and out-tangent per keyframe
		var cubic = (string)sampler["interpolation"] == "CUBICSPLINE";
		var perKey = values.Length / times.Length;
		var count = cubic ? perKey / 3 : perKey;
		var offset = cubic ? count : 0;
		var weights = Enumerable.Range(0, times.Length)
			.Select(i => values.Skip(i * perKey + offset).Take(count).ToArray())
			.ToArray();
		return (times, weights);
	}

	private static Vector3[] Bake(SkinnedMeshRenderer renderer)
	{
		var baked = new Mesh();
		try
		{
			renderer.BakeMesh(baked);
			return baked.vertices;
		}
		finally
		{
			Object.DestroyImmediate(baked);
		}
	}

	private static void AssertVerticesEqual(Vector3[] expected, Vector3[] actual, string message)
	{
		Assert.AreEqual(expected.Length, actual.Length, $"{message}: vertex count");
		for (var i = 0; i < expected.Length; i++)
			Assert.Less(Vector3.Distance(expected[i], actual[i]), VertexTolerance, $"{message}: vertex {i} {expected[i]:F4} vs {actual[i]:F4}");
	}

	#endregion
}
