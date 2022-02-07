#if HAVE_TIMELINE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Timeline
{
	public class GLTFRecorder
	{
		public GLTFRecorder(Transform root)
		{
			this.root = root;
		}

		private Transform root;
		private Dictionary<Transform, AnimationData> data = new Dictionary<Transform, AnimationData>();
		private double startTime;
		private double lastRecordedTime;
		private bool isRecording;

		internal class AnimationData
		{
			public int Size => 4 + 4 + keys.Count * (4 + 4 + 40);
			private Transform tr;
			public TransformData lastData;
			public Dictionary<double, TransformData> keys = new Dictionary<double, TransformData>();

			private bool skippedLastFrame = false;
			private double skippedTime;

			public AnimationData(Transform tr, double time, bool zeroScale = false)
			{
				this.tr = tr;
				keys.Add(time, new TransformData(tr, zeroScale));
			}

			public void Update(double time)
			{
				var newTr = new TransformData(tr, !tr.gameObject.activeSelf);
				if (newTr.Equals(lastData))
				{
					skippedLastFrame = true;
					skippedTime = time;
					return;
				}

				if (skippedLastFrame)
				{
					keys[skippedTime] = lastData;
				}

				skippedLastFrame = false;

				lastData = newTr;

				// first keyframe could be different to initialization, so we want to override this
				if (keys.ContainsKey(time))
				{
					keys[time] = newTr;
				}
				else
				{
					keys.Add(time, newTr);
				}
			}
		}
		public void StartRecording(double time)
		{
			startTime = time;
			lastRecordedTime = time;
			var trs = root.GetComponentsInChildren<Transform>(true);
			data.Clear();
			foreach (var tr in trs)
			{
				data.Add(tr, new AnimationData(tr, 0, !tr.gameObject.activeSelf));
			}
			isRecording = true;
		}

		public void UpdateRecording(double time)
		{
			if (!isRecording)
			{
				throw new InvalidOperationException($"{nameof(GLTFRecorder)} isn't recording, but {nameof(UpdateRecording)} was called. This is invalid.");
			}
			if (time <= lastRecordedTime)
			{
				Debug.LogWarning("Can't record backwards in time, please avoid this.");
				return;
			}

			var currentTime = time - startTime;
			var trs = root.GetComponentsInChildren<Transform>(true);
			foreach (var tr in trs)
			{
				if (!data.ContainsKey(tr))
				{
					// insert "empty" frame with scale=0,0,0 because this object might have just appeared in this frame
					var emptyData = new AnimationData(tr, lastRecordedTime, true);
					data.Add(tr, emptyData);
					// insert actual keyframe
					data[tr].Update(currentTime);
				}
				else
					data[tr].Update(currentTime);
			}

			lastRecordedTime = time;
		}

		public void EndRecording(string filename)
		{
			if (!isRecording) return;
			isRecording = false;

			// log
			Debug.Log("Gltf Recording saved. Tracks: " + data.Count + ", Keys: " + data.First().Value.keys.Count + ",\nTotal Keys: " + data.Sum(x => x.Value.keys.Count));

			GLTFSceneExporter.ExportDisabledGameObjects = true;

			var exporter = new GLTFSceneExporter(new Transform[] { root }, new ExportOptions()
			{
				ExportInactivePrimitives = true,
				AfterSceneExport = PostExport,
			});
			var path = Path.GetDirectoryName(filename);
			var file = Path.GetFileName(filename);
			exporter.SaveGLB(path, file);
		}

		private void PostExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
		{
			exporter.ExportAnimationFromNode(ref root);

			GLTFAnimation anim = new GLTFAnimation();
			anim.Name = gltfRoot.GetDefaultScene()?.Name ?? "Recording";

			CollectAnimation(exporter, ref root, ref anim, 1f);

			if (anim.Channels.Count > 0 && anim.Samplers.Count > 0)
				gltfRoot.Animations.Add(anim);
		}

		private void CollectAnimation(GLTFSceneExporter gltfSceneExporter, ref Transform root, ref GLTFAnimation anim, float speed)
		{
			foreach (var kvp in data)
			{
				if(kvp.Value.keys.Count < 1) continue;

				var times = kvp.Value.keys.Keys.Select(x => (float) x).ToArray();
				var values = kvp.Value.keys.Values.ToArray();
				var positions = values.Select(x => x.position).ToArray();
				var rotations = values.Select(x => x.rotation).Select(x => new Vector4(x.x, x.y, x.z, x.w)).ToArray();
				var scales = values.Select(x => x.scale).ToArray();
				float[] weights = null;
				int weightCount = 0;

				gltfSceneExporter.RemoveUnneededKeyframes(ref times, ref positions, ref rotations, ref scales, ref weights, ref weightCount);
				gltfSceneExporter.AddAnimationData(kvp.Key, ref anim, times, positions, rotations, scales);
			}
		}

		internal readonly struct TransformData
		{
			public readonly Vector3 position;
			public readonly Quaternion rotation;
			public readonly Vector3 scale;

			public TransformData(Transform tr, bool zeroScale = false)
			{
				position = tr.localPosition;
				rotation = tr.localRotation;
				scale = zeroScale ? Vector3.zero : tr.localScale;
			}

			public bool Equals(TransformData other)
			{
				return position.Equals(other.position) && rotation.Equals(other.rotation) && scale.Equals(other.scale);
			}

			public override bool Equals(object obj)
			{
				return obj is TransformData other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = position.GetHashCode();
					hashCode = (hashCode * 397) ^ rotation.GetHashCode();
					hashCode = (hashCode * 397) ^ scale.GetHashCode();
					return hashCode;
				}
			}

			public static bool operator ==(TransformData left, TransformData right)
			{
				return left.Equals(right);
			}

			public static bool operator !=(TransformData left, TransformData right)
			{
				return !left.Equals(right);
			}
		}
	}
}
#endif
