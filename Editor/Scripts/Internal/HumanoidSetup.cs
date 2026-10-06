using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	internal static class HumanoidSetup
	{
		internal static Avatar AddAvatarToGameObject(GameObject gameObject, bool flipForward)
		{
			var previousRotation = gameObject.transform.rotation;
			if (flipForward)
				gameObject.transform.rotation *= Quaternion.Euler(0, 180, 0);

			try
			{
				var description = AvatarUtils.CreateHumanDescription(gameObject);
				var namedBones = description.human;
				SetupHumanSkeleton(gameObject, namedBones, out var human, out var skeleton, out var hasTranslationDoF);
				description.human = human;
				description.skeleton = skeleton;
				description.hasTranslationDoF = hasTranslationDoF;

				var avatar = BuildHumanAvatar(gameObject, description);
				if (!avatar)
				{
					// Fall back to the bone names only and the model's own pose
					description.human = namedBones;
					description.skeleton = Array.Empty<SkeletonBone>();
					description.hasTranslationDoF = false;
					avatar = BuildHumanAvatar(gameObject, description);
				}
				if (!avatar) return null;

				var animator = gameObject.GetComponent<Animator>();
				if (animator) animator.avatar = avatar;
				return avatar;
			}
			finally
			{
				if (flipForward)
					gameObject.transform.rotation = previousRotation;
			}
		}

		private static readonly string[] RootCurveNames = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };
		private static readonly string[] FingerNames = { "Thumb", "Index", "Middle", "Ring", "Little" };
		private static readonly (string name, HumanBodyBones bone)[] IKGoals =
		{
			("LeftFoot", HumanBodyBones.LeftFoot), ("RightFoot", HumanBodyBones.RightFoot),
			("LeftHand", HumanBodyBones.LeftHand), ("RightHand", HumanBodyBones.RightHand),
		};
		private static readonly string[] IKGoalCurveSuffixes = { "T.x", "T.y", "T.z", "Q.x", "Q.y", "Q.z", "Q.w" };

		// Rotation from a bone to the frame Unity uses for its IK goal. Internal, so the IK goal curves are skipped if it ever goes away.
		private static readonly MethodInfo AvatarGetPostRotation = typeof(Avatar).GetMethod("GetPostRotation",
			BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);

		/// <summary>
		/// The clip property of a muscle. <see cref="HumanTrait.MuscleName"/> uses "Left Thumb 1 Stretched" for finger muscles,
		/// clips use "LeftHand.Thumb.1 Stretched"; all other muscles have the same name in both.
		/// </summary>
		private static string MuscleCurveName(string muscleName)
		{
			foreach (var side in new[] { "Left", "Right" })
			foreach (var finger in FingerNames)
			{
				var prefix = side + " " + finger + " ";
				if (muscleName.StartsWith(prefix, StringComparison.Ordinal))
					return side + "Hand." + finger + "." + muscleName.Substring(prefix.Length);
			}
			return muscleName;
		}

		private class IKGoal
		{
			public string name;
			public Transform transform;
			public Quaternion postRotation;
			// Foot goals are at the sole, below the ankle
			public float bottomHeight;
			public float[][] values;
		}

		/// <summary>
		/// Turns clips that animate the skeleton into humanoid clips, like the model importer does for humanoid rigs:
		/// the clip is sampled on the hierarchy and the transform curves of the human bones are replaced by body (RootT / RootQ),
		/// IK goal and muscle curves on the Animator. Without that the clips stay generic: they only play on this exact hierarchy,
		/// can't be retargeted to other humanoids, and the Animator doesn't extract root motion from them.
		/// Curves of transforms that aren't part of the human (props, extra bones) and of other components are kept.
		/// </summary>
		internal static void ConvertClipsToHumanoid(GameObject gameObject, Avatar avatar, AnimationClip[] clips)
		{
			if (clips == null || !avatar || !avatar.isHuman) return;

			var root = gameObject.transform;
			var transforms = root.GetComponentsInChildren<Transform>(true);
			var boneNames = avatar.humanDescription.human.ToDictionary(b => b.humanName, b => b.boneName);
			var humanBoneNames = new HashSet<string>(boneNames.Values);

			// Curves of the human bones and of their parents below the root are replaced by the body and muscle curves,
			// since the body pose already contains the motion of the parents
			var humanPaths = new HashSet<string>();
			foreach (var t in transforms)
			{
				if (t == root || !humanBoneNames.Contains(t.name)) continue;
				for (var p = t; p && p != root; p = p.parent)
					humanPaths.Add(AnimationUtility.CalculateTransformPath(p, root));
			}
			if (humanPaths.Count == 0) return;

			var muscleCurveNames = HumanTrait.MuscleName.Select(MuscleCurveName).ToArray();

			// Sampling the clips changes the pose, so remember it
			var pose = transforms.Select(t => (t.localPosition, t.localRotation, t.localScale)).ToArray();
			void RestorePose()
			{
				for (var i = 0; i < transforms.Length; i++)
				{
					transforms[i].localPosition = pose[i].localPosition;
					transforms[i].localRotation = pose[i].localRotation;
					transforms[i].localScale = pose[i].localScale;
				}
			}

			// The human scale and the feet heights are only available through an Animator that uses the avatar.
			// After reading them the avatar is removed again: with the human avatar assigned, SampleAnimation evaluates the
			// clips as humanoid clips, and as they don't have muscle curves yet the skeleton would end up in the zero muscle pose.
			var animator = gameObject.GetComponent<Animator>();
			var addedAnimator = !animator;
			if (addedAnimator) animator = gameObject.AddComponent<Animator>();
			var animatorAvatar = animator.avatar;
			animator.avatar = avatar;
			var humanScale = animator.humanScale;
			var feetBottomHeight = new[] { animator.leftFeetBottomHeight, animator.rightFeetBottomHeight };
			animator.avatar = null;

			var goals = new List<IKGoal>();
			if (AvatarGetPostRotation != null)
			{
				for (var i = 0; i < IKGoals.Length; i++)
				{
					var (name, bone) = IKGoals[i];
					var t = boneNames.TryGetValue(HumanTrait.BoneName[(int)bone], out var boneName) ? transforms.FirstOrDefault(x => x.name == boneName) : null;
					if (!t) continue;
					goals.Add(new IKGoal
					{
						name = name,
						transform = t,
						postRotation = (Quaternion)AvatarGetPostRotation.Invoke(avatar, new object[] { (int)bone }),
						bottomHeight = i < feetBottomHeight.Length ? feetBottomHeight[i] : 0,
					});
				}
			}

			var handler = new HumanPoseHandler(avatar, root);
			try
			{
				var humanPose = new HumanPose();
				foreach (var clip in clips)
				{
					if (!clip || clip.humanMotion) continue;

					var humanBindings = AnimationUtility.GetCurveBindings(clip)
						.Where(b => b.type == typeof(Transform) && humanPaths.Contains(b.path))
						.ToArray();
					// Clips that don't move the skeleton (e.g. only blend shapes or props) stay generic, so they can
					// play on another layer without locking the body
					if (humanBindings.Length == 0) continue;

					// A clip only sets the properties it animates, so start each clip from the original pose
					RestorePose();

					var keyTimes = GetSampleTimes(clip, humanBindings);
					var rootValues = RootCurveNames.Select(_ => new float[keyTimes.Length]).ToArray();
					var muscleValues = muscleCurveNames.Select(_ => new float[keyTimes.Length]).ToArray();
					foreach (var goal in goals)
						goal.values = IKGoalCurveSuffixes.Select(_ => new float[keyTimes.Length]).ToArray();

					for (var k = 0; k < keyTimes.Length; k++)
					{
						clip.SampleAnimation(gameObject, keyTimes[k]);
						handler.GetHumanPose(ref humanPose);

						var bodyRotation = humanPose.bodyRotation;
						SetPositionAndRotation(rootValues, k, humanPose.bodyPosition, bodyRotation);
						for (var m = 0; m < muscleValues.Length; m++)
							muscleValues[m][k] = humanPose.muscles[m];

						// Goals are relative to the body, in the same normalized space as the body position
						var inverseBodyRotation = Quaternion.Inverse(bodyRotation);
						foreach (var goal in goals)
						{
							var rotation = Quaternion.Inverse(root.rotation) * goal.transform.rotation * goal.postRotation;
							var position = root.InverseTransformPoint(goal.transform.position) + rotation * new Vector3(goal.bottomHeight, 0, 0);
							SetPositionAndRotation(goal.values, k,
								inverseBodyRotation * (position / humanScale - humanPose.bodyPosition),
								inverseBodyRotation * rotation);
						}
					}

					foreach (var binding in humanBindings)
						AnimationUtility.SetEditorCurve(clip, binding, null);

					var bindings = new List<EditorCurveBinding>();
					var curves = new List<AnimationCurve>();
					void AddCurve(string property, float[] values)
					{
						bindings.Add(EditorCurveBinding.FloatCurve("", typeof(Animator), property));
						curves.Add(LinearCurve(keyTimes, values));
					}
					for (var i = 0; i < RootCurveNames.Length; i++)
						AddCurve(RootCurveNames[i], rootValues[i]);
					foreach (var goal in goals)
						for (var i = 0; i < IKGoalCurveSuffixes.Length; i++)
							AddCurve(goal.name + IKGoalCurveSuffixes[i], goal.values[i]);
					for (var i = 0; i < muscleCurveNames.Length; i++)
						AddCurve(muscleCurveNames[i], muscleValues[i]);
					AnimationUtility.SetEditorCurves(clip, bindings.ToArray(), curves.ToArray());

					// Base the root transform on the original root (the glTF scene) instead of the center of mass and body
					// orientation, so the character plays exactly where the clip places it: with the defaults it's shifted
					// below the center of mass at the start of the clip
					var settings = AnimationUtility.GetAnimationClipSettings(clip);
					settings.keepOriginalOrientation = settings.keepOriginalPositionY = settings.keepOriginalPositionXZ = true;
					AnimationUtility.SetAnimationClipSettings(clip, settings);
				}
			}
			finally
			{
				handler.Dispose();
				RestorePose();
				if (addedAnimator) Object.DestroyImmediate(animator);
				else animator.avatar = animatorAvatar;
			}
		}

		/// <summary>
		/// The keys of the given curves, plus one sample per frame so curves with non-linear interpolation are followed.
		/// </summary>
		private static float[] GetSampleTimes(AnimationClip clip, EditorCurveBinding[] bindings)
		{
			var times = new SortedSet<float>();
			foreach (var binding in bindings)
			foreach (var key in AnimationUtility.GetEditorCurve(clip, binding).keys)
				times.Add(key.time);
			var frameRate = clip.frameRate > 0 ? clip.frameRate : 30;
			var frameCount = Mathf.FloorToInt(clip.length * frameRate);
			for (var i = 0; i <= frameCount; i++)
				times.Add(i / frameRate);
			times.Add(clip.length);

			// Keys are usually on frames as well; skip samples that only differ by rounding
			var result = new List<float>();
			foreach (var time in times)
				if (result.Count == 0 || time - result[result.Count - 1] > 0.0001f)
					result.Add(time);
			return result.ToArray();
		}

		/// <summary>
		/// Writes a position and rotation into the x, y, z and x, y, z, w curve values at the given key.
		/// The rotation is kept in the same hemisphere as the previous key, so interpolation takes the short path.
		/// The first key has a positive w, so the curves don't depend on which of q and -q the pose returned.
		/// </summary>
		private static void SetPositionAndRotation(float[][] values, int key, Vector3 position, Quaternion rotation)
		{
			var flip = key > 0
				? values[3][key - 1] * rotation.x + values[4][key - 1] * rotation.y + values[5][key - 1] * rotation.z + values[6][key - 1] * rotation.w < 0
				: rotation.w < 0;
			if (flip)
				rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
			values[0][key] = position.x;
			values[1][key] = position.y;
			values[2][key] = position.z;
			values[3][key] = rotation.x;
			values[4][key] = rotation.y;
			values[5][key] = rotation.z;
			values[6][key] = rotation.w;
		}

		private static AnimationCurve LinearCurve(float[] times, float[] values)
		{
			var keys = new Keyframe[times.Length];
			for (var i = 0; i < keys.Length; i++)
			{
				var inTangent = i > 0 ? (values[i] - values[i - 1]) / (times[i] - times[i - 1]) : 0;
				var outTangent = i < keys.Length - 1 ? (values[i + 1] - values[i]) / (times[i + 1] - times[i]) : 0;
				keys[i] = new Keyframe(times[i], values[i], inTangent, outTangent);
			}
			return new AnimationCurve(keys);
		}

		private static Avatar BuildHumanAvatar(GameObject gameObject, HumanDescription description)
		{
			var avatar = AvatarBuilder.BuildHumanAvatar(gameObject, description);
			avatar.name = gameObject.name + "Avatar";
			if (avatar.isValid && avatar.isHuman) return avatar;

			Object.DestroyImmediate(avatar);
			return null;
		}

		/// <summary>
		/// Does what AvatarSetupTool.SetupHumanSkeleton does for the model importer (its signature differs between Unity
		/// versions, so it's rebuilt from the AvatarSetupTool helpers): completes the bone mapping with Unity's auto
		/// mapping and creates the skeleton pose, corrected to a T-pose. Works on a copy, so the imported hierarchy keeps its pose.
		/// Bones mapped by name (see <see cref="AvatarUtils"/>) take priority over the auto mapping.
		/// </summary>
		private static void SetupHumanSkeleton(GameObject gameObject, HumanBone[] namedBones,
			out HumanBone[] human, out SkeletonBone[] skeleton, out bool hasTranslationDoF)
		{
			hasTranslationDoF = false;
			var copy = Object.Instantiate(gameObject);
			// Skeleton bones are matched by name, so the root must keep its name (no "(Clone)")
			copy.name = gameObject.name;
			try
			{
				var isBiped = AvatarBipedMapper.IsBiped(copy.transform, null);
				AvatarSetupTool.SampleBindPose(copy);

				// GetModelBones uses the bone weights of skinned meshes, which aren't readable when Read/Write is disabled.
				// Then all transforms are treated as bones.
				var meshesReadable = copy.GetComponentsInChildren<SkinnedMeshRenderer>(true)
					.All(r => !r.sharedMesh || r.sharedMesh.isReadable);
				var modelBones = AvatarSetupTool.GetModelBones(copy.transform, !meshesReadable, null);
				var autoMapping = isBiped
					? AvatarBipedMapper.MapBones(copy.transform)
					: AvatarAutoMapper.MapBones(copy.transform, modelBones);

				var mapping = new Dictionary<string, string>();
				foreach (var bone in namedBones)
				{
					if (!mapping.ContainsKey(bone.humanName))
						mapping[bone.humanName] = bone.boneName;
				}
				var mappedTransforms = new HashSet<string>(mapping.Values);
				foreach (var pair in autoMapping)
				{
					var humanName = HumanTrait.BoneName[pair.Key];
					if (!pair.Value || mapping.ContainsKey(humanName) || mappedTransforms.Contains(pair.Value.name)) continue;
					mapping[humanName] = pair.Value.name;
					mappedTransforms.Add(pair.Value.name);
				}
				human = mapping.Select(m => new HumanBone
				{
					humanName = m.Key,
					boneName = m.Value,
					limit = new HumanLimit { useDefaultValues = true },
				}).ToArray();

				var humanBones = AvatarSetupTool.GetHumanBones(mapping, modelBones);
				if (isBiped)
				{
					AvatarBipedMapper.BipedPose(copy, humanBones);
					hasTranslationDoF = true;
				}
				else
				{
					// Start from whichever is closer to a T-pose, the bind pose or the model's pose, then enforce a T-pose
					var bindPoseError = AvatarSetupTool.GetPoseError(humanBones);
					AvatarSetupTool.CopyPose(copy, gameObject);
					if (bindPoseError < AvatarSetupTool.GetPoseError(humanBones))
						AvatarSetupTool.SampleBindPose(copy);
					AvatarSetupTool.MakePoseValid(humanBones);
				}

				skeleton = AvatarSetupTool.GetSkeletonBones(copy.transform);
			}
			finally
			{
				Object.DestroyImmediate(copy);
			}
		}


	    // AvatarSetupTools
	    // AvatarBuilder.BuildHumanAvatar
	    // AvatarConfigurationStage.CreateStage
	    // AssetImporterTabbedEditor
	    // ModelImporterRigEditor

#if TESTING
	    [MenuItem("Tools/Copy Hierarchy Array")]
	    static void _Copy(MenuCommand command)
	    {
		    var gameObject = Selection.activeGameObject;
		    var sb = new System.Text.StringBuilder();

		    void Traverse(Transform tr)
		    {
			    sb.AppendLine(tr.name);
			    foreach (Transform child in tr)
			    {
				    Traverse(child);
			    }
		    }

		    Traverse(gameObject.transform);
		    EditorGUIUtility.systemCopyBuffer = sb.ToString();
	    }

	    [MenuItem("Tools/Setup Humanoid")]
	    static void _Do(MenuCommand command)
	    {
		    var gameObject = Selection.activeGameObject;
		    // SetupHumanSkeleton(go, ref humanBoneMappingArray, out var skeletonBones, out var hasTranslationDoF);
			AddAvatarToGameObject(gameObject);
	    }

	    [MenuItem("Tools/Open Avatar Editor")]
	    static void _OpenEditor(MenuCommand command)
	    {
		    var gameObject = Selection.activeGameObject;
		    var avatar = gameObject.GetComponent<Animator>().avatar;
		    var e = (AvatarEditor) Editor.CreateEditor(avatar, typeof(AvatarEditor));
		    e.m_CameFromImportSettings = true;
		    Selection.activeObject = e;
		    e.SwitchToEditMode();
	    }
#endif
	}
}
