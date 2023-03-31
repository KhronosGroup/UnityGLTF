#if UNITY_EDITOR
#define ANIMATION_EXPORT_SUPPORTED
#endif

#if ANIMATION_EXPORT_SUPPORTED && (UNITY_ANIMATION || !UNITY_2019_1_OR_NEWER)
#define ANIMATION_SUPPORTED
#endif

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		internal void CollectClipCurvesBySampling(GameObject root, AnimationClip clip, Dictionary<string, TargetCurveSet> targetCurves)
		{
			var recorder = new GLTFRecorder(root.transform, false, false, false);

			var playableGraph = PlayableGraph.Create();
			var animationClipPlayable = (Playable) AnimationClipPlayable.Create(playableGraph, clip);

#if UNITY_2020_2_OR_NEWER
			var rigs = root.GetComponents<IAnimationWindowPreview>();
			if (rigs != null)
			{
				foreach (var rig in rigs)
				{
					rig.StopPreview(); // seems to be needed in some cases because the Rig isn't properly marked as non-initialized by Unity
					rig.StartPreview();
					animationClipPlayable = rig.BuildPreviewGraph(playableGraph, animationClipPlayable); // modifies the playable to include Animation Rigging data
				}
			}
			else
			{
				rigs = System.Array.Empty<IAnimationWindowPreview>();
			}
#endif

			var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", root.GetComponent<Animator>());
			playableOutput.SetSourcePlayable(animationClipPlayable);
			playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

			var timeStep = 1.0f / 30.0f;
			var length = clip.length;
			var time = 0f;

#if UNITY_2020_1_OR_NEWER
			// This seems to not properly cleanup when exporting animation from inside a prefab asset
			// e.g. when exporting animator with humanoid animation the animator is left disabled after this
			// the animator is disabled after the first call to `SampleAnimationClip`
			var driver = ScriptableObject.CreateInstance<AnimationModeDriver>();
			AnimationMode.StartAnimationMode(driver);
#else
			AnimationMode.StartAnimationMode();
#endif
			AnimationMode.BeginSampling();

			// if this is a Prefab, we need to collect property modifications here to work around
			// limitations of AnimationMode - otherwise prefab modifications will persist...
			var isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(root);
			var prefabModifications = isPrefabAsset ? PrefabUtility.GetPropertyModifications(root) : default;
			Undo.RegisterFullObjectHierarchyUndo(root, "Animation Sampling");

			// add the root since we need to shift it around -
			// it will be reset when exiting AnimationMode again and will not be dirty.
			AnimationMode_AddTransformTRS(root);

			// TODO not entirely sure if only checking for humanMotion here is correct
			if (clip.isHumanMotion)
			{
				root.transform.localPosition = Vector3.zero;
				root.transform.localRotation = Quaternion.identity;
				// root.transform.localScale = Vector3.one;
			}

			// first frame
			foreach (var rig in rigs) rig.UpdatePreviewGraph(playableGraph);
			AnimationMode.SamplePlayableGraph(playableGraph, 0, time);

			// for Animation Rigging its often desired to have one complete loop first, so we need to prewarm to have a nice exportable animation
			var prewarm = rigs.Length > 0 && clip.isLooping;
			if (prewarm)
			{
				while (time + timeStep < length)
				{
					time += timeStep;
					foreach (var rig in rigs) rig.UpdatePreviewGraph(playableGraph);
					AnimationMode.SamplePlayableGraph(playableGraph, 0, time);
				}

				// reset time to the start
				time = 0;
			}

			recorder.StartRecording(time);

			while (time + timeStep < length)
			{
				time += timeStep;
				foreach (var rig in rigs) rig.UpdatePreviewGraph(playableGraph);
				AnimationMode.SamplePlayableGraph(playableGraph, 0, time);
				recorder.UpdateRecording(time);
			}

			// last frame
			time = length;
#if UNITY_2020_2_OR_NEWER
			foreach (var rig in rigs) rig.UpdatePreviewGraph(playableGraph);
#endif
			AnimationMode.SamplePlayableGraph(playableGraph, 0, time);
			recorder.UpdateRecording(time);

#if UNITY_2020_2_OR_NEWER
			foreach (var rig in rigs) rig.StopPreview();
#endif

			AnimationMode.EndSampling();
#if UNITY_2020_1_OR_NEWER
			AnimationMode.StopAnimationMode(driver);
#else
			AnimationMode.StopAnimationMode();
#endif

			// reset prefab modifications if this was a prefab asset
			if (isPrefabAsset) {
				PrefabUtility.SetPropertyModifications(root, prefabModifications);
			}

			// seems to be necessary because the animation sampling API doesn't fully work;
			// sometimes samples still "leak" into property modifications
			Undo.FlushUndoRecordObjects();
			Undo.PerformUndo();

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

				var positionTrack = kvp.Value.tracks.FirstOrDefault(x => x.propertyName == "translation");
				if (positionTrack != null)
				{
					var t0 = positionTrack.times;
					var frameData = positionTrack.values;
					var posX = new AnimationCurve(t0.Select((value, index) => new Keyframe((float)value, ((Vector3)frameData[index]).x)).ToArray());
					var posY = new AnimationCurve(t0.Select((value, index) => new Keyframe((float)value, ((Vector3)frameData[index]).y)).ToArray());
					var posZ = new AnimationCurve(t0.Select((value, index) => new Keyframe((float)value, ((Vector3)frameData[index]).z)).ToArray());
					curveSet.translationCurves = new [] { posX, posY, posZ };
				}

				var rotationTrack = kvp.Value.tracks.FirstOrDefault(x => x.propertyName == "rotation");
				if (rotationTrack != null)
				{
					var t1 = rotationTrack.times;
					var frameData = rotationTrack.values;
					var rotX = new AnimationCurve(t1.Select((value, index) => new Keyframe((float)value, ((Quaternion)frameData[index]).x)).ToArray());
					var rotY = new AnimationCurve(t1.Select((value, index) => new Keyframe((float)value, ((Quaternion)frameData[index]).y)).ToArray());
					var rotZ = new AnimationCurve(t1.Select((value, index) => new Keyframe((float)value, ((Quaternion)frameData[index]).z)).ToArray());
					var rotW = new AnimationCurve(t1.Select((value, index) => new Keyframe((float)value, ((Quaternion)frameData[index]).w)).ToArray());
					curveSet.rotationCurves = new [] { rotX, rotY, rotZ, rotW };
				}

				var scaleTrack = kvp.Value.tracks.FirstOrDefault(x => x.propertyName == "scale");
				if (scaleTrack != null)
				{
					var t2 = scaleTrack.times;
					var frameData = scaleTrack.values;
					var sclX = new AnimationCurve(t2.Select((value, index) => new Keyframe((float)value, ((Vector3)frameData[index]).x)).ToArray());
					var sclY = new AnimationCurve(t2.Select((value, index) => new Keyframe((float)value, ((Vector3)frameData[index]).y)).ToArray());
					var sclZ = new AnimationCurve(t2.Select((value, index) => new Keyframe((float)value, ((Vector3)frameData[index]).z)).ToArray());
					curveSet.scaleCurves = new [] { sclX, sclY, sclZ };
				}

				var calculatedPath = CalculatePath(kvp.Key, root.transform);
				targetCurves[calculatedPath] = curveSet;
			}
		}

		private static MethodInfo _AddTransformTRS;
		private static void AnimationMode_AddTransformTRS(GameObject gameObject)
		{
			if (!gameObject) return;
			if (_AddTransformTRS == null) _AddTransformTRS = typeof(AnimationMode).GetMethod("AddTransformTRS", (BindingFlags)(-1));
			_AddTransformTRS?.Invoke(null, new object[] { gameObject, "" });
		}
#endif
	}
}
