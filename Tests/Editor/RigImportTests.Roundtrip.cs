using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
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

	// Spike exactly at the second-to-last 30 fps frame (29/30s) of a 1s clip
	private const string AnimationEndOfClip = "Animation_EndOfClip.glb";

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
		yield return new TestCaseData(AnimationEndOfClip, AnimationMethod.Mecanim, "");
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

	/// <summary>
	/// Exports a hierarchy with an Animator playing the clip, imports the exported file and returns the imported clip.
	/// Fails if the importer reports keyframe times that are not increasing.
	/// </summary>
	private static AnimationClip ExportClipAndReimport(AnimationClip clip, string fileName, System.Action<GameObject> setupMover = null)
	{
		var root = new GameObject("Root");
		var mover = new GameObject("Mover");
		mover.transform.SetParent(root.transform, false);
		setupMover?.Invoke(mover);
		var controller = new AnimatorController { name = "Export" };
		controller.AddLayer("Base Layer");
		controller.layers[0].stateMachine.AddState(clip.name).motion = clip;
		root.AddComponent<Animator>().runtimeAnimatorController = controller;
		var settings = ScriptableObject.CreateInstance<GLTFSettings>();
		settings.UseMainCameraVisibility = false;

		var path = $"{TempFolder}/{fileName}.glb";
		try
		{
			var exporter = new GLTFSceneExporter(root.transform, new ExportContext(settings));
			File.WriteAllBytes(path, exporter.SaveGLBToByteArray(fileName));
		}
		finally
		{
			Object.DestroyImmediate(root);
			Object.DestroyImmediate(controller);
			Object.DestroyImmediate(settings);
		}

		var notIncreasing = new List<string>();
		void OnLog(string message, string stackTrace, LogType type)
		{
			if (message.Contains("not increasing")) notIncreasing.Add(message);
		}
		Application.logMessageReceived += OnLog;
		try
		{
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
			Reimport(path, AnimationMethod.Mecanim);
		}
		finally
		{
			Application.logMessageReceived -= OnLog;
		}
		Assert.IsEmpty(notIncreasing, "Exported keyframe times are not increasing");

		var imported = LoadClips(path).SingleOrDefault(c => c.name == clip.name);
		Assert.IsNotNull(imported, $"Clip \"{clip.name}\" missing after export and import");
		return imported;
	}

	private static void SetPositionCurves(AnimationClip clip, AnimationCurve x)
	{
		var end = x.keys.Last().time;
		AnimationCurve Zero() => end > 0 ? AnimationCurve.Constant(0, end, 0) : new AnimationCurve(new Keyframe(0, 0));
		clip.SetCurve("Mover", typeof(Transform), "m_LocalPosition.x", x);
		clip.SetCurve("Mover", typeof(Transform), "m_LocalPosition.y", Zero());
		clip.SetCurve("Mover", typeof(Transform), "m_LocalPosition.z", Zero());
	}

	[Test]
	public void Export_StepAtEndOfClip_KeepsValuesAndIncreasingTimes()
	{
		// Linear first, then steps right before the end (constant tangents): uses the exporter's step handling up to the last sample
		var x = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(0.99f, 5), new Keyframe(1, 5));
		for (var i = 0; i < x.length; i++)
			AnimationUtility.SetKeyRightTangentMode(x, i, AnimationUtility.TangentMode.Linear);
		AnimationUtility.SetKeyLeftTangentMode(x, 0, AnimationUtility.TangentMode.Linear);
		AnimationUtility.SetKeyLeftTangentMode(x, 1, AnimationUtility.TangentMode.Linear);
		AnimationUtility.SetKeyLeftTangentMode(x, 2, AnimationUtility.TangentMode.Constant);
		AnimationUtility.SetKeyLeftTangentMode(x, 3, AnimationUtility.TangentMode.Constant);

		var clip = new AnimationClip { name = "StepAtEnd" };
		try
		{
			SetPositionCurves(clip, x);
			var imported = ExportClipAndReimport(clip, "Export_StepAtEnd");
			var curve = AnimationUtility.GetEditorCurve(imported, EditorCurveBinding.FloatCurve("Mover", typeof(Transform), "m_LocalPosition.x"));
			Assert.AreEqual(1f, imported.length, CurveTolerance);
			Assert.AreEqual(0.5f, curve.Evaluate(0.25f), 0.01f, "Linear part");
			Assert.AreEqual(1f, curve.Evaluate(0.9f), 0.01f, "Value before the step");
			Assert.AreEqual(5f, curve.Evaluate(1f), 0.01f, "Value after the step, at the end of the clip");
		}
		finally
		{
			Object.DestroyImmediate(clip);
		}
	}

	[Test]
	public void Export_BlendShapeAnimation_KeepsTiming()
	{
		// Morph target weights of meshes with more than one morph target used to be exported one frame late
		var mesh = Object.Instantiate(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
		mesh.name = "Cube";
		mesh.AddBlendShapeFrame("Grow", 100, mesh.vertices.Select(v => v * 0.5f).ToArray(), null, null);
		mesh.AddBlendShapeFrame("Shrink", 100, mesh.vertices.Select(v => v * -0.5f).ToArray(), null, null);
		var clip = new AnimationClip { name = "Grow" };
		try
		{
			clip.SetCurve("Mover", typeof(SkinnedMeshRenderer), "blendShape.Grow", AnimationCurve.Linear(0, 0, 1, 100));
			var imported = ExportClipAndReimport(clip, "Export_BlendShape", mover =>
			{
				var renderer = mover.AddComponent<SkinnedMeshRenderer>();
				renderer.sharedMesh = mesh;
				renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
			});

			// glTF animates all weights of a mesh together, the animated one is the one that changes
			var curve = AnimationUtility.GetCurveBindings(imported)
				.Where(b => b.propertyName.StartsWith("blendShape."))
				.Select(b => AnimationUtility.GetEditorCurve(imported, b))
				.OrderByDescending(c => c.Evaluate(1))
				.FirstOrDefault();
			Assert.IsNotNull(curve, "Blend shape curve missing after export and import");
			// The importer may use a different weight range, so compare relative to the end value
			var end = curve.Evaluate(1);
			Assert.Greater(end, 0);
			for (var frame = 0; frame <= ExportFrameRate; frame++)
			{
				var time = frame / ExportFrameRate;
				Assert.AreEqual(time, curve.Evaluate(time) / end, 0.01f, $"Blend shape weight at {time:F3}s");
			}
		}
		finally
		{
			Object.DestroyImmediate(clip);
			Object.DestroyImmediate(mesh);
		}
	}

	[Test]
	public void Export_ZeroLengthClip_HasSingleKey()
	{
		var clip = new AnimationClip { name = "SinglePose" };
		try
		{
			SetPositionCurves(clip, new AnimationCurve(new Keyframe(0, 2)));
			var imported = ExportClipAndReimport(clip, "Export_SinglePose");
			var curve = AnimationUtility.GetEditorCurve(imported, EditorCurveBinding.FloatCurve("Mover", typeof(Transform), "m_LocalPosition.x"));
			Assert.AreEqual(2f, curve.Evaluate(0), CurveTolerance);
		}
		finally
		{
			Object.DestroyImmediate(clip);
		}
	}

	[Test]
	public void Export_MissingBone_KeepsJointOrder()
	{
		// https://github.com/KhronosGroup/UnityGLTF/issues/873: a deleted bone used to be dropped from the joints,
		// shifting all following joints and leaving more inverse bind matrices than joints
		var path = Import(HumanoidArmature, AnimationMethod.Mecanim);
		var model = LoadModel(path);
		var exportPath = $"{TempFolder}/Export_MissingBone.glb";

		var instance = Object.Instantiate(model);
		instance.name = model.name;
		var settings = ScriptableObject.CreateInstance<GLTFSettings>();
		string[] expectedBones;
		int missingIndex;
		try
		{
			var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>();
			Assert.IsNotNull(skin, "Test asset has no SkinnedMeshRenderer");
			var bones = skin.bones;

			// Delete a leaf bone that is not the last one, so the following joints would shift
			missingIndex = System.Array.FindIndex(bones, b => b.childCount == 0 && b != skin.rootBone);
			Assert.That(missingIndex, Is.InRange(0, bones.Length - 2), "Test asset has no leaf bone before the last bone");
			expectedBones = bones.Select(b => AnimationUtility.CalculateTransformPath(b, instance.transform)).ToArray();
			Object.DestroyImmediate(bones[missingIndex].gameObject);
			Assert.IsFalse(skin.bones[missingIndex], "Deleted bone should be a missing reference");
			var maxJointIndex = skin.sharedMesh.boneWeights.Max(w => Mathf.Max(w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3));

			settings.UseMainCameraVisibility = false;
			var exporter = new GLTFSceneExporter(instance.transform, new ExportContext(settings));
			LogAssert.Expect(LogType.Warning, new Regex($"null bone at index {missingIndex}"));
			File.WriteAllBytes(exportPath, exporter.SaveGLBToByteArray(Path.GetFileNameWithoutExtension(exportPath)));

			var gltfSkin = exporter.GetRoot().Skins.Single();
			Assert.AreEqual(bones.Length, gltfSkin.Joints.Count, "Joint count");
			CollectionAssert.AllItemsAreUnique(gltfSkin.Joints.Select(j => j.Id), "Joints of a skin have to be unique");
			Assert.AreEqual(gltfSkin.Joints.Count, (int)gltfSkin.InverseBindMatrices.Value.Count, "Inverse bind matrix count must match the joint count");
			Assert.Less(maxJointIndex, gltfSkin.Joints.Count, "JOINTS_0 references a joint that doesn't exist");
		}
		finally
		{
			Object.DestroyImmediate(instance);
			Object.DestroyImmediate(settings);
		}

		AssetDatabase.ImportAsset(exportPath, ImportAssetOptions.ForceSynchronousImport);
		Reimport(exportPath, AnimationMethod.Mecanim);
		var reimported = LoadModel(exportPath);
		var reimportedSkin = reimported.GetComponentInChildren<SkinnedMeshRenderer>();
		Assert.IsNotNull(reimportedSkin, "Skin missing after export and import");
		var actualBones = reimportedSkin.bones.Select(b => AnimationUtility.CalculateTransformPath(b, reimported.transform)).ToArray();
		Assert.AreEqual(expectedBones.Length, actualBones.Length, "Bone count after export and import");
		for (var i = 0; i < expectedBones.Length; i++)
		{
			// The missing bone's slot is filled with a stand-in, every other joint has to stay at its index
			if (i == missingIndex) continue;
			Assert.AreEqual(expectedBones[i], actualBones[i], $"Bone {i} after export and import");
		}
	}

	#region Roundtrip helpers

	/// <summary>
	/// Exports an instance of the imported model, then imports the exported file with the same settings.
	/// </summary>
	internal static string ExportAndReimport(string path, AnimationMethod method, string rootNodeName)
	{
		var model = LoadModel(path);
		var clips = LoadClips(path);
		var exportPath = $"{path.Substring(0, path.LastIndexOf('/'))}/{Path.GetFileNameWithoutExtension(path)}_{method}_{rootNodeName}_Roundtrip.glb";

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

	internal static Dictionary<string, Transform> TransformsByPath(GameObject model)
	{
		return model.GetComponentsInChildren<Transform>(true)
			.ToDictionary(t => AnimationUtility.CalculateTransformPath(t, model.transform), t => t);
	}

	internal static void AssertHierarchyEqual(string expectedPath, string actualPath)
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

	internal static void AssertMeshesEqual(string expectedPath, string actualPath)
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

	internal static void AssertAnimationsEqual(string expectedPath, string actualPath)
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

	internal static void AssertAvatarsEqual(string expectedPath, string actualPath)
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
