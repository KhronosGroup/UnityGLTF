#if UNITY_EDITOR
#define ANIMATION_EXPORT_SUPPORTED
#endif

#if ANIMATION_EXPORT_SUPPORTED && (UNITY_ANIMATION || !UNITY_2019_1_OR_NEWER)
#define ANIMATION_SUPPORTED
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityGLTF.Timeline;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
#if ANIMATION_SUPPORTED
		internal void CollectClipCurvesForHumanoid(GameObject root, AnimationClip clip, Dictionary<string, TargetCurveSet> targetCurves)
		{
			if (!clip.humanMotion) return;

			// collect which bones we want
			var animator = root.GetComponent<Animator>();
			Dictionary<Transform, Transform> animatedTransforms = new Dictionary<Transform, Transform>();
			foreach (HumanBodyBones val in Enum.GetValues(typeof(HumanBodyBones)))
			{
				// index must be between 0 and LastBone
				if((int) val <= 0 || (int) val >= (int) HumanBodyBones.LastBone) continue;

				var tr = animator.GetBoneTransform(val);
				if (tr)
					animatedTransforms.Add(tr, null);
			}

			// Debug.Log("Animated:\n" + string.Join("\n", animatedTransforms.Keys.Select(x => x.name)));

			var recorder = new GLTFRecorder(root.transform, false, false, false);

			// var playableGraph = PlayableGraph.Create();
			// var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);

			// Wrap the clip in a playable
			// var animationClipPlayable = AnimationClipPlayable.Create(playableGraph, clip);

			// Connect the Playable to an output
			// playableOutput.SetSourcePlayable(animationClipPlayable);

			// Plays the Graph.
			// playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

			var timeStep = 1.0f / 30.0f;
			var length = clip.length;
			var time = 0f;

#if UNITY_2020_1_OR_NEWER
			var driver = ScriptableObject.CreateInstance<AnimationModeDriver>();
			AnimationMode.StartAnimationMode(driver);
#else
			AnimationMode.StartAnimationMode();
#endif
			AnimationMode.BeginSampling();

			root.transform.position = Vector3.zero;
			root.transform.rotation = Quaternion.identity;

			// first frame
			AnimationMode.SampleAnimationClip(root, clip, time);
			recorder.StartRecording(time);

			while (time + timeStep < length)
			{
				time += timeStep;
				AnimationMode.SampleAnimationClip(root, clip, time);
				recorder.UpdateRecording(time);
			}

			// last frame
			time = length;
			AnimationMode.SampleAnimationClip(root, clip, time);
			recorder.UpdateRecording(time);

			AnimationMode.EndSampling();
#if UNITY_2020_1_OR_NEWER
			AnimationMode.StopAnimationMode(driver);
#else
			AnimationMode.StopAnimationMode();
#endif

			recorder.EndRecording(out var data);
			if (data == null || !data.Any()) return;

			string CalculatePath(Transform child, Transform parent)
			{
				if (child == parent) return "";
				if (child.parent == null) return "";
				var parentPath = CalculatePath(child.parent, parent);
				if (!string.IsNullOrEmpty(parentPath)) return parentPath + "/" + child.name;
				return child.name;
			}

			// convert AnimationData back to AnimationCurve (slow)
			// better would be to directly emit the animation here, but then we need to be careful with partial hierarchies
			// and other cases that can go wrong.
			foreach (var kvp in data)
			{
				var curveSet = new TargetCurveSet();
				curveSet.Init();
				var times = kvp.Value.keys.Keys.ToArray();
				var frameData = kvp.Value.keys.Values.ToArray();

				var posX = new AnimationCurve(times.Select((value, index) => new Keyframe((float)value, frameData[index].position.x)).ToArray());
				var posY = new AnimationCurve(times.Select((value, index) => new Keyframe((float)value, frameData[index].position.y)).ToArray());
				var posZ = new AnimationCurve(times.Select((value, index) => new Keyframe((float)value, frameData[index].position.z)).ToArray());

				var rotX = new AnimationCurve(times.Select((value, index) => new Keyframe((float)value, frameData[index].rotation.x)).ToArray());
				var rotY = new AnimationCurve(times.Select((value, index) => new Keyframe((float)value, frameData[index].rotation.y)).ToArray());
				var rotZ = new AnimationCurve(times.Select((value, index) => new Keyframe((float)value, frameData[index].rotation.z)).ToArray());
				var rotW = new AnimationCurve(times.Select((value, index) => new Keyframe((float)value, frameData[index].rotation.w)).ToArray());

				var sclX = new AnimationCurve(times.Select((value, index) => new Keyframe((float)value, frameData[index].scale.x)).ToArray());
				var sclY = new AnimationCurve(times.Select((value, index) => new Keyframe((float)value, frameData[index].scale.y)).ToArray());
				var sclZ = new AnimationCurve(times.Select((value, index) => new Keyframe((float)value, frameData[index].scale.z)).ToArray());

				curveSet.translationCurves = new [] { posX, posY, posZ };
				curveSet.rotationCurves = new [] { rotX, rotY, rotZ, rotW };
				curveSet.scaleCurves = new [] { sclX, sclY, sclZ };

				var calculatedPath = CalculatePath(kvp.Key, root.transform);
				targetCurves[calculatedPath] = curveSet;
			}

			// Debug.Log("Recorded Transforms:\n" + string.Join("\n", targetCurves.Keys));
		}
#endif
	}
}
