using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityGLTF;

/// <summary>
/// Roundtrip tests: import a test asset, export the result with the GLTFSceneExporter, import the exported file with the
/// same settings and compare both import results (hierarchy, meshes, skins, animation curves, avatar, animator).
/// The files themselves can't be compared byte by byte, since exporter and generator write different but equivalent glTF.
/// </summary>
public partial class RigImportTests
{
	private const float PositionTolerance = 1e-4f;
	private const float AngleTolerance = 0.1f;
	private const float CurveTolerance = 1e-3f;
	private const float CurveAngleTolerance = 0.5f;
	// Matches AnimationBakingFramerate in ExporterAnimation.cs
	private const float ExportFrameRate = 30;

	public static IEnumerable<TestCaseData> RoundtripCases()
	{
		yield return new TestCaseData(RootMotionEmptyRoot, AnimationMethod.Mecanim, "Root");
		yield return new TestCaseData(RootMotionEmptyRoot, AnimationMethod.Mecanim, "");
		yield return new TestCaseData(RootMotionAnimatedRoot, AnimationMethod.Mecanim, "Root");
		yield return new TestCaseData(StaticNoAnimation, AnimationMethod.Mecanim, "");
		yield return new TestCaseData(HumanoidArmature, AnimationMethod.MecanimHumanoid, "");
		yield return new TestCaseData(HumanoidMixamo, AnimationMethod.MecanimHumanoid, "");
		yield return new TestCaseData(HumanoidAPose, AnimationMethod.MecanimHumanoid, "");
		yield return new TestCaseData(HumanoidUnrealNames, AnimationMethod.MecanimHumanoid, "");
		yield return new TestCaseData(HumanoidArmature, AnimationMethod.Mecanim, "Hips");
	}

	[TestCaseSource(nameof(RoundtripCases))]
	public void Roundtrip_ImportExportImport_KeepsRigAndAnimations(string file, AnimationMethod method, string rootNodeName)
	{
		var path = Import(file, method, rootNodeName);
		var roundtripPath = ExportAndReimport(path, method, rootNodeName);

		AssertHierarchyEqual(path, roundtripPath);
		AssertMeshesEqual(path, roundtripPath);
		AssertAnimationsEqual(path, roundtripPath);
		AssertAvatarsEqual(path, roundtripPath);
	}

	#region Roundtrip helpers

	/// <summary>
	/// Exports an instance of the imported model, then imports the exported file with the same settings.
	/// </summary>
	private static string ExportAndReimport(string path, AnimationMethod method, string rootNodeName)
	{
		var model = LoadModel(path);
		var clips = LoadClips(path);
		var exportPath = $"{TempFolder}/{Path.GetFileNameWithoutExtension(path)}_{method}_{rootNodeName}_Roundtrip.glb";

		var instance = Object.Instantiate(model);
		instance.name = model.name;
		AnimatorController controller = null;
		var settings = ScriptableObject.CreateInstance<GLTFSettings>();
		try
		{
			// The exporter takes the clips from the AnimatorController, which isn't stored with the imported model
			var animator = instance.GetComponent<Animator>();
			if (animator && clips.Length > 0)
			{
				controller = new AnimatorController { name = "Roundtrip" };
				controller.AddLayer("Base Layer");
				foreach (var clip in clips)
					controller.layers[0].stateMachine.AddState(clip.name).motion = clip;
				animator.runtimeAnimatorController = controller;
			}

			// Own settings instead of the project's, so the test doesn't depend on (or create) them
			settings.UseMainCameraVisibility = false;
			settings.ExportAnimations = true;
			var exporter = new GLTFSceneExporter(instance.transform, new ExportContext(settings));
			File.WriteAllBytes(exportPath, exporter.SaveGLBToByteArray(Path.GetFileNameWithoutExtension(exportPath)));
		}
		finally
		{
			Object.DestroyImmediate(instance);
			if (controller) Object.DestroyImmediate(controller);
			Object.DestroyImmediate(settings);
		}

		AssetDatabase.ImportAsset(exportPath, ImportAssetOptions.ForceSynchronousImport);
		Reimport(exportPath, method, rootNodeName);
		return exportPath;
	}

	private static Dictionary<string, Transform> TransformsByPath(GameObject model)
	{
		return model.GetComponentsInChildren<Transform>(true)
			.ToDictionary(t => AnimationUtility.CalculateTransformPath(t, model.transform), t => t);
	}

	private static void AssertHierarchyEqual(string expectedPath, string actualPath)
	{
		var expected = TransformsByPath(LoadModel(expectedPath));
		var actual = TransformsByPath(LoadModel(actualPath));
		CollectionAssert.AreEquivalent(expected.Keys, actual.Keys, "Hierarchy differs after the roundtrip");

		foreach (var pair in expected)
		{
			// The root's own transform is not part of the comparison, only the hierarchy below it
			if (pair.Key.Length == 0) continue;
			var e = pair.Value;
			var a = actual[pair.Key];
			Assert.Less(Vector3.Distance(e.localPosition, a.localPosition), PositionTolerance, $"Position of \"{pair.Key}\": {e.localPosition:F4} vs {a.localPosition:F4}");
			Assert.Less(Quaternion.Angle(e.localRotation, a.localRotation), AngleTolerance, $"Rotation of \"{pair.Key}\": {e.localRotation.eulerAngles:F2} vs {a.localRotation.eulerAngles:F2}");
			Assert.Less(Vector3.Distance(e.localScale, a.localScale), PositionTolerance, $"Scale of \"{pair.Key}\": {e.localScale:F4} vs {a.localScale:F4}");
		}
	}

	private static void AssertMeshesEqual(string expectedPath, string actualPath)
	{
		var expected = TransformsByPath(LoadModel(expectedPath));
		var actual = TransformsByPath(LoadModel(actualPath));

		foreach (var pair in expected)
		{
			var e = pair.Value;
			var a = actual[pair.Key];

			var expectedMesh = e.GetComponent<MeshFilter>() ? e.GetComponent<MeshFilter>().sharedMesh : null;
			var expectedSkin = e.GetComponent<SkinnedMeshRenderer>();
			if (expectedSkin) expectedMesh = expectedSkin.sharedMesh;
			var actualSkin = a.GetComponent<SkinnedMeshRenderer>();
			var actualMesh = actualSkin ? actualSkin.sharedMesh : a.GetComponent<MeshFilter>() ? a.GetComponent<MeshFilter>().sharedMesh : null;

			Assert.AreEqual((bool)expectedSkin, (bool)actualSkin, $"\"{pair.Key}\" skinned mesh after the roundtrip");
			Assert.AreEqual((bool)expectedMesh, (bool)actualMesh, $"\"{pair.Key}\" mesh after the roundtrip");
			if (!expectedMesh) continue;

			Assert.AreEqual(expectedMesh.vertexCount, actualMesh.vertexCount, $"Vertex count of \"{pair.Key}\"");
			Assert.AreEqual(expectedMesh.subMeshCount, actualMesh.subMeshCount, $"Submesh count of \"{pair.Key}\"");
			for (var i = 0; i < expectedMesh.subMeshCount; i++)
				Assert.AreEqual(expectedMesh.GetIndexCount(i), actualMesh.GetIndexCount(i), $"Index count of \"{pair.Key}\" submesh {i}");
			Assert.Less(Vector3.Distance(expectedMesh.bounds.center, actualMesh.bounds.center), 1e-3f, $"Bounds center of \"{pair.Key}\"");
			Assert.Less(Vector3.Distance(expectedMesh.bounds.size, actualMesh.bounds.size), 1e-3f, $"Bounds size of \"{pair.Key}\"");

			if (!expectedSkin) continue;
			var expectedModel = LoadModel(expectedPath).transform;
			var actualModel = LoadModel(actualPath).transform;
			CollectionAssert.AreEqual(
				expectedSkin.bones.Select(b => AnimationUtility.CalculateTransformPath(b, expectedModel)),
				actualSkin.bones.Select(b => AnimationUtility.CalculateTransformPath(b, actualModel)),
				$"Skin bones of \"{pair.Key}\"");

			var expectedBindposes = expectedMesh.bindposes;
			var actualBindposes = actualMesh.bindposes;
			Assert.AreEqual(expectedBindposes.Length, actualBindposes.Length, $"Bindpose count of \"{pair.Key}\"");
			for (var i = 0; i < expectedBindposes.Length; i++)
			for (var j = 0; j < 16; j++)
				Assert.AreEqual(expectedBindposes[i][j], actualBindposes[i][j], 1e-3f, $"Bindpose {i} of \"{pair.Key}\"");
		}
	}

	private static void AssertAnimationsEqual(string expectedPath, string actualPath)
	{
		var expectedClips = LoadClips(expectedPath).ToDictionary(c => c.name);
		var actualClips = LoadClips(actualPath).ToDictionary(c => c.name);
		CollectionAssert.AreEquivalent(expectedClips.Keys, actualClips.Keys, "Animation clips differ after the roundtrip");

		foreach (var expected in expectedClips.Values)
		{
			var actual = actualClips[expected.name];
			Assert.AreEqual(expected.length, actual.length, CurveTolerance, $"Length of clip \"{expected.name}\"");
			Assert.AreEqual(expected.hasMotionCurves, actual.hasMotionCurves, $"Root motion curves of clip \"{expected.name}\"");

			var expectedBindings = AnimationUtility.GetCurveBindings(expected);
			var actualBindings = AnimationUtility.GetCurveBindings(actual);
			string Key(EditorCurveBinding b) => $"{b.path}|{b.type.Name}|{b.propertyName}";
			CollectionAssert.AreEquivalent(expectedBindings.Select(Key), actualBindings.Select(Key), $"Animated properties of clip \"{expected.name}\"");

			// The exporter resamples animations at a fixed frame rate (keys in between are lost), so compare at those times.
			// Rotations are compared as quaternions, since q and -q are the same rotation.
			var samples = Mathf.Max(1, Mathf.CeilToInt(expected.length * ExportFrameRate)) + 1;
			var actualByKey = actualBindings.ToDictionary(Key);
			foreach (var binding in expectedBindings)
			{
				if (binding.propertyName.StartsWith("m_LocalRotation.") || binding.propertyName.StartsWith("MotionQ.")) continue;
				var expectedCurve = AnimationUtility.GetEditorCurve(expected, binding);
				var actualCurve = AnimationUtility.GetEditorCurve(actual, actualByKey[Key(binding)]);
				for (var i = 0; i < samples; i++)
				{
					var time = expected.length * i / (samples - 1);
					Assert.AreEqual(expectedCurve.Evaluate(time), actualCurve.Evaluate(time), CurveTolerance,
						$"Clip \"{expected.name}\", \"{binding.path}\" {binding.propertyName} at {time:F3}s");
				}
			}

			foreach (var group in expectedBindings
				         .Where(b => b.propertyName.StartsWith("m_LocalRotation.") || b.propertyName.StartsWith("MotionQ."))
				         .GroupBy(b => (b.path, b.type, prefix: b.propertyName.Substring(0, b.propertyName.IndexOf('.')))))
			{
				var (bindingPath, type, prefix) = group.Key;
				AnimationCurve Curve(AnimationClip clip, string component) =>
					AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(bindingPath, type, $"{prefix}.{component}"));
				// Components are interpolated separately, so the sampled quaternion has to be normalized
				Quaternion Sample(AnimationClip clip, float time) => Quaternion.Normalize(new Quaternion(
					Curve(clip, "x").Evaluate(time), Curve(clip, "y").Evaluate(time), Curve(clip, "z").Evaluate(time), Curve(clip, "w").Evaluate(time)));

				for (var i = 0; i < samples; i++)
				{
					var time = expected.length * i / (samples - 1);
					var e = Sample(expected, time);
					var a = Sample(actual, time);
					Assert.Less(Quaternion.Angle(e, a), CurveAngleTolerance,
						$"Clip \"{expected.name}\", \"{bindingPath}\" {prefix} at {time:F3}s: {e.eulerAngles:F2} vs {a.eulerAngles:F2}");
				}
			}
		}
	}

	private static void AssertAvatarsEqual(string expectedPath, string actualPath)
	{
		var expected = LoadAvatar(expectedPath);
		var actual = LoadAvatar(actualPath);
		Assert.AreEqual((bool)expected, (bool)actual, "Avatar after the roundtrip");

		var expectedAnimator = LoadModel(expectedPath).GetComponent<Animator>();
		var actualAnimator = LoadModel(actualPath).GetComponent<Animator>();
		Assert.AreEqual((bool)expectedAnimator, (bool)actualAnimator, "Animator after the roundtrip");
		if (expectedAnimator)
			Assert.AreEqual(expectedAnimator.applyRootMotion, actualAnimator.applyRootMotion, "Apply Root Motion after the roundtrip");

		if (!expected) return;
		Assert.AreEqual(expected.isValid, actual.isValid, "Avatar validity");
		Assert.AreEqual(expected.isHuman, actual.isHuman, "Avatar type");
		if (!expected.isHuman) return;

		var expectedMapping = expected.humanDescription.human.ToDictionary(b => b.humanName, b => b.boneName);
		var actualMapping = actual.humanDescription.human.ToDictionary(b => b.humanName, b => b.boneName);
		CollectionAssert.AreEquivalent(expectedMapping, actualMapping, "Human bone mapping after the roundtrip");
	}

	#endregion
}
