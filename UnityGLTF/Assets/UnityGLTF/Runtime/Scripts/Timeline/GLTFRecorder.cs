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
		public GLTFRecorder(Transform root, bool recordBlendShapes = true)
		{
			this.root = root;
			this.recordBlendShapes = recordBlendShapes;
		}

		private Transform root;
		private Dictionary<Transform, AnimationData> data = new Dictionary<Transform, AnimationData>();
		private double startTime;
		private double lastRecordedTime;
		private bool isRecording;
		private readonly bool recordBlendShapes;

		public bool IsRecording => isRecording;
		public double LastRecordedTime => lastRecordedTime;

		internal class AnimationData
		{
			public int Size => 4 + 4 + keys.Count * (4 + 4 + 40);
			private Transform tr;
			public FrameData lastData;
			public Dictionary<double, FrameData> keys = new Dictionary<double, FrameData>();

			private bool skippedLastFrame = false;
			private double skippedTime;
			private bool recordBlendShapes;

			public AnimationData(Transform tr, double time, bool zeroScale = false, bool recordBlendShapes = true)
			{
				this.tr = tr;
				this.recordBlendShapes = recordBlendShapes;
				keys.Add(time, new FrameData(tr, zeroScale, this.recordBlendShapes));
			}

			public void Update(double time)
			{
				var newTr = new FrameData(tr, !tr.gameObject.activeSelf, recordBlendShapes);
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
					var emptyData = new AnimationData(tr, lastRecordedTime, true, recordBlendShapes);
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

			var previousExportDisabledState = GLTFSceneExporter.ExportDisabledGameObjects;
			var previousExportAnimationState = GLTFSceneExporter.ExportAnimations;
			GLTFSceneExporter.ExportDisabledGameObjects = true;
			GLTFSceneExporter.ExportAnimations = false;

			var exporter = new GLTFSceneExporter(new Transform[] { root }, new ExportOptions()
			{
				ExportInactivePrimitives = true,
				AfterSceneExport = PostExport,
			});
			var path = Path.GetDirectoryName(filename);
			var file = Path.GetFileName(filename);
			exporter.SaveGLB(path, file);

			GLTFSceneExporter.ExportDisabledGameObjects = previousExportDisabledState;
			GLTFSceneExporter.ExportAnimations = previousExportAnimationState;
		}

		private void PostExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
		{
			exporter.ExportAnimationFromNode(ref root);

			GLTFAnimation anim = new GLTFAnimation();
			anim.Name = gltfRoot.GetDefaultScene()?.Name ?? "Recording";

			CollectAndProcessAnimation(exporter, anim);

			if (anim.Channels.Count > 0 && anim.Samplers.Count > 0)
				gltfRoot.Animations.Add(anim);
		}

		private void CollectAndProcessAnimation(GLTFSceneExporter gltfSceneExporter, GLTFAnimation anim)
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
				if(values.All(x => x.weights != null))
				{
					weights = values.SelectMany(x => x.weights).ToArray();
					weightCount = values.First().weights.Length;
				}

				gltfSceneExporter.RemoveUnneededKeyframes(ref times, ref positions, ref rotations, ref scales, ref weights, ref weightCount);
				gltfSceneExporter.AddAnimationData(kvp.Key, anim, times, positions, rotations, scales, weights);
			}
		}

		internal readonly struct FrameData
		{
			public readonly Vector3 position;
			public readonly Quaternion rotation;
			public readonly Vector3 scale;
			public readonly float[] weights;

			public FrameData(Transform tr, bool zeroScale, bool recordBlendShapes)
			{
				position = tr.localPosition;
				rotation = tr.localRotation;
				scale = zeroScale ? Vector3.zero : tr.localScale;

				if (recordBlendShapes)
				{
					var skinnedMesh = tr.GetComponent<SkinnedMeshRenderer>();
					if (skinnedMesh)
					{
						var mesh = skinnedMesh.sharedMesh;
						var blendShapeCount = mesh.blendShapeCount;
						weights = new float[blendShapeCount];
						for (var i = 0; i < blendShapeCount; i++)
							weights[i] = skinnedMesh.GetBlendShapeWeight(i);
					}
					else
					{
						weights = null;
					}
				}
				else
				{
					weights = null;
				}
			}

			public bool Equals(FrameData other)
			{
				return position.Equals(other.position) && rotation.Equals(other.rotation) && scale.Equals(other.scale) && ((weights == null && other.weights == null) || (weights != null && other.weights != null && weights.SequenceEqual(other.weights)));
			}

			public override bool Equals(object obj)
			{
				return obj is FrameData other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = position.GetHashCode();
					hashCode = (hashCode * 397) ^ rotation.GetHashCode();
					hashCode = (hashCode * 397) ^ scale.GetHashCode();
					if(weights != null)
						hashCode = (hashCode * 397) ^ weights.GetHashCode();
					return hashCode;
				}
			}

			public static bool operator ==(FrameData left, FrameData right)
			{
				return left.Equals(right);
			}

			public static bool operator !=(FrameData left, FrameData right)
			{
				return !left.Equals(right);
			}
		}
	}
}
