using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	internal static class NonHumanoidSetup
	{
		/// <summary>
		/// Builds a generic avatar for the hierarchy and assigns it to the Animator on the root, if there is one.
		/// </summary>
		/// <param name="rootNodeName">Name of the root motion transform below the root. Null or empty for no root motion.</param>
		/// <param name="rootNodeFound">False if a root node name was given but no transform below the root has that name.
		/// The avatar is then built without a root motion node.</param>
		internal static Avatar AddAvatarToGameObject(GameObject gameObject, string rootNodeName, out bool rootNodeFound)
		{
			rootNodeName ??= "";

			// AvatarBuilder only searches below the root, and logs an error if the name isn't found
			rootNodeFound = rootNodeName.Length == 0 || gameObject.GetComponentsInChildren<Transform>(true)
				.Any(t => t != gameObject.transform && t.name == rootNodeName);
			if (!rootNodeFound)
				rootNodeName = "";

			var avatar = AvatarBuilder.BuildGenericAvatar(gameObject, rootNodeName);
			avatar.name = gameObject.name + "Avatar";

			if (!avatar.isValid)
			{
				Object.DestroyImmediate(avatar);
				return null;
			}

			var animator = gameObject.GetComponent<Animator>();
			if (animator) animator.avatar = avatar;
			return avatar;
		}

		private static readonly string[] MotionCurveNames = { "MotionT.x", "MotionT.y", "MotionT.z", "MotionQ.x", "MotionQ.y", "MotionQ.z", "MotionQ.w" };

		/// <summary>
		/// Adds generic root motion curves (MotionT / MotionQ on the Animator) to the clips, like the FBX importer does.
		/// Without them the Animator doesn't extract root motion from script-created clips, even if the avatar has a
		/// root motion node: the root node just moves on its own and the GameObject stays in place.
		/// The motion is the root node's pose relative to the root, so animated parents of the root node are included.
		/// </summary>
		internal static void AddRootMotionCurves(GameObject gameObject, string rootNodeName, AnimationClip[] clips)
		{
			if (clips == null || string.IsNullOrEmpty(rootNodeName)) return;

			var root = gameObject.transform;
			var rootNode = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t != root && t.name == rootNodeName);
			if (!rootNode) return;
			var rootNodePath = AnimationUtility.CalculateTransformPath(rootNode, root);

			// Sampling the clips changes the pose, so remember it
			var transforms = root.GetComponentsInChildren<Transform>(true);
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

			foreach (var clip in clips)
			{
				if (!clip) continue;
				// A clip only sets the properties it animates, so start each clip from the original pose
				RestorePose();

				// Keys of everything that moves the root node: its own transform curves and those of its parents
				var times = new SortedSet<float>();
				foreach (var binding in AnimationUtility.GetCurveBindings(clip))
				{
					if (binding.type != typeof(Transform)) continue;
					if (binding.path.Length > 0 && binding.path != rootNodePath && !rootNodePath.StartsWith(binding.path + "/")) continue;
					foreach (var key in AnimationUtility.GetEditorCurve(clip, binding).keys)
						times.Add(key.time);
				}
				// Root node isn't animated in this clip
				if (times.Count == 0) continue;

				// Also sample in between keys, so curves with non-linear interpolation are followed
				var frameRate = clip.frameRate > 0 ? clip.frameRate : 30;
				for (var t = 0f; t < clip.length; t += 1f / frameRate)
					times.Add(t);
				times.Add(clip.length);

				var curves = MotionCurveNames.Select(_ => new AnimationCurve()).ToArray();
				var previousRotation = Quaternion.identity;
				foreach (var time in times)
				{
					clip.SampleAnimation(gameObject, time);
					var position = root.InverseTransformPoint(rootNode.position);
					var rotation = Quaternion.Inverse(root.rotation) * rootNode.rotation;
					// Keep the quaternion in the same hemisphere as the previous key, so interpolation takes the short path
					if (Quaternion.Dot(previousRotation, rotation) < 0)
						rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
					previousRotation = rotation;

					curves[0].AddKey(time, position.x);
					curves[1].AddKey(time, position.y);
					curves[2].AddKey(time, position.z);
					curves[3].AddKey(time, rotation.x);
					curves[4].AddKey(time, rotation.y);
					curves[5].AddKey(time, rotation.z);
					curves[6].AddKey(time, rotation.w);
				}

				for (var i = 0; i < curves.Length; i++)
				{
					for (var k = 0; k < curves[i].length; k++)
					{
						AnimationUtility.SetKeyLeftTangentMode(curves[i], k, AnimationUtility.TangentMode.Linear);
						AnimationUtility.SetKeyRightTangentMode(curves[i], k, AnimationUtility.TangentMode.Linear);
					}
					AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), MotionCurveNames[i]), curves[i]);
				}
			}

			RestorePose();
		}
	}
}
