#define USE_ANIMATION_POINTER
// #define USE_REGULAR_ANIMATION

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GLTF.Schema;
using Unity.Profiling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF.Timeline
{
	public class GLTFRecorder
	{
		public GLTFRecorder(Transform root, bool recordBlendShapes = true, bool recordRootInWorldSpace = false, bool recordAnimationPointer = false)
		{
			if (!root)
				throw new ArgumentNullException(nameof(root), "Please provide a root transform to record.");

			this.root = root;
			this.recordBlendShapes = recordBlendShapes;
			this.recordRootInWorldSpace = recordRootInWorldSpace;
			this.recordAnimationPointer = recordAnimationPointer;
		}

		/// <summary>
		/// Optionally assign a list of transforms to be recorded, other transforms will be ignored
		/// </summary>
		internal ICollection<Transform> recordingList = null;
		private bool AllowRecordingTransform(Transform tr) => recordingList == null || recordingList.Contains(tr);

		private Transform root;
		private Dictionary<Transform, AnimationData> data = new Dictionary<Transform, AnimationData>(64);
		private double startTime;
		private double lastRecordedTime;
		private bool isRecording;
		private readonly bool recordBlendShapes;
		private readonly bool recordRootInWorldSpace;
		private readonly bool recordAnimationPointer;

		public bool IsRecording => isRecording;
		public double LastRecordedTime => lastRecordedTime;

		internal class AnimationData
		{
			private Transform tr;
			private SkinnedMeshRenderer smr;
			private bool recordBlendShapes;
			private bool inWorldSpace = false;
			private bool recordAnimationPointer;

#if USE_REGULAR_ANIMATION
			public FrameData lastData;
			public Dictionary<double, FrameData> keys = new Dictionary<double, FrameData>(1024);
			private bool skippedLastFrame = false;
			private double skippedTime;
#endif

#if USE_ANIMATION_POINTER
			private static List<ExportPlan> exportPlans;
			private static MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			internal List<Track> tracks = new List<Track>();

			internal class ExportPlan
			{
				public string propertyName;
				public Type dataType;
				public Func<Transform, Object> GetTarget;
				public Func<Transform, Object, object> GetData;

				public ExportPlan(string propertyName, Type dataType, Func<Transform, Object> GetTarget, Func<Transform, Object, object> GetData)
				{
					this.propertyName = propertyName;
					this.dataType = dataType;
					this.GetTarget = GetTarget;
					this.GetData = GetData;
				}

				public object Sample(Transform tr)
				{
					var target = GetTarget(tr);
					return GetData(tr, target);
				}
			}
#endif

			public AnimationData(Transform tr, double time, bool zeroScale = false, bool recordBlendShapes = true, bool inWorldSpace = false, bool recordAnimationPointer = false)
			{
				this.tr = tr;
				this.smr = tr.GetComponent<SkinnedMeshRenderer>();
				this.recordBlendShapes = recordBlendShapes;
				this.inWorldSpace = inWorldSpace;
				this.recordAnimationPointer = recordAnimationPointer;
#if USE_REGULAR_ANIMATION
				keys.Add(time, new FrameData(tr, smr, zeroScale, this.recordBlendShapes, this.inWorldSpace));
#endif

#if USE_ANIMATION_POINTER
				if (exportPlans == null)
				{
					exportPlans = new List<ExportPlan>();
					exportPlans.Add(new ExportPlan("translation", typeof(Vector3), x => x, (tr0, _) => tr0.localPosition));
					exportPlans.Add(new ExportPlan("rotation", typeof(Quaternion), x => x, (tr0, _) =>
					{
						var q = tr0.localRotation;
						return new Quaternion(q.x, q.y, q.z, q.w);
					}));
					exportPlans.Add(new ExportPlan("scale", typeof(Vector3), x => x, (tr0, _) => tr0.localScale));

					exportPlans.Add(new ExportPlan("weights", typeof(float[]), x => x.GetComponent<SkinnedMeshRenderer>(), (tr0, x) =>
					{
						if (x is SkinnedMeshRenderer skinnedMesh && skinnedMesh.sharedMesh)
						{
							var mesh = skinnedMesh.sharedMesh;
							var blendShapeCount = mesh.blendShapeCount;
							var weights = new float[blendShapeCount];
							for (var i = 0; i < blendShapeCount; i++)
								weights[i] = skinnedMesh.GetBlendShapeWeight(i);
							return weights;
						}
						return null;
					}));

					if(recordAnimationPointer)
					{
						// TODO add other animation pointer export plans

						exportPlans.Add(new ExportPlan("baseColorFactor", typeof(Color), x => x.GetComponent<MeshRenderer>() ? x.GetComponent<MeshRenderer>().sharedMaterial : null, (tr0, mat) =>
						{
							var r = tr0.GetComponent<Renderer>();

							if (r.HasPropertyBlock())
							{
								r.GetPropertyBlock(materialPropertyBlock);
	#if UNITY_2021_1_OR_NEWER
								if (materialPropertyBlock.HasColor("_BaseColor")) return materialPropertyBlock.GetColor("_BaseColor");
								if (materialPropertyBlock.HasColor("_Color")) return materialPropertyBlock.GetColor("_Color");
	#else
								var c = materialPropertyBlock.GetColor("_BaseColor");
								if (c.r != 0 || c.g != 0 || c.b != 0 || c.a != 0) return c;
								c = materialPropertyBlock.GetColor("_Color");
								if (c.r != 0 || c.g != 0 || c.b != 0 || c.a != 0) return c;
								// this leaves an edge case where someone is actually animating color to black:
								// in that case, the un-animated color would now be exported...
	#endif
							}

							if (mat is Material m && m)
							{
								if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
								if (m.HasProperty("_Color")) return m.GetColor("_Color");
							}

							return null;
						}));
					}
				}

				foreach (var plan in exportPlans)
				{
					if (plan.GetTarget(tr)) {
						tracks.Add(new Track(tr, plan, time));
					}
				}
#endif
			}

#if USE_ANIMATION_POINTER
			internal class Track
			{
				public Object animatedObject => plan.GetTarget(tr);
				public string propertyName => plan.propertyName;
				// TODO sample as floats?
				public float[] times => samples.Keys.Select(x => (float) x).ToArray();
				public object[] values => samples.Values.ToArray();

				private Transform tr;
				private ExportPlan plan;
				private Dictionary<double, object> samples;
				private object lastSample;

				public Track(Transform tr, ExportPlan plan, double time)
				{
					this.tr = tr;
					this.plan = plan;
					samples = new Dictionary<double, object>();
					SampleIfChanged(time);
				}

				public void SampleIfChanged(double time)
				{
					var value = plan.Sample(tr);
					if (value == null || (value is Object o && !o))
						return;
					if (!value.Equals(lastSample))
					{
						samples[time] = value;
						lastSample = value;
					}
				}
			}
#endif

			public void Update(double time)
			{
#if USE_ANIMATION_POINTER
				foreach (var track in tracks)
				{
					track.SampleIfChanged(time);
				}
#endif

#if USE_REGULAR_ANIMATION
				var newTr = new FrameData(tr, smr, !tr.gameObject.activeSelf, recordBlendShapes, inWorldSpace);
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
#endif
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
				if (!AllowRecordingTransform(tr)) continue;
				data.Add(tr, new AnimationData(tr, 0, !tr.gameObject.activeSelf, recordBlendShapes, recordRootInWorldSpace && tr == root, recordAnimationPointer));
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
				if (!AllowRecordingTransform(tr)) continue;
				if (!data.ContainsKey(tr))
				{
					// insert "empty" frame with scale=0,0,0 because this object might have just appeared in this frame
					var emptyData = new AnimationData(tr, lastRecordedTime, true, recordBlendShapes, recordAnimationPointer);
					data.Add(tr, emptyData);
					// insert actual keyframe
					data[tr].Update(currentTime);
				}
				else
					data[tr].Update(currentTime);
			}

			lastRecordedTime = time;
		}

		internal void EndRecording(out Dictionary<Transform, AnimationData> param)
		{
			param = null;
			if (!isRecording) return;

			isRecording = false;
			param = data;
		}

		public void EndRecording(string filename, string sceneName = "scene", GLTFSettings settings = null)
		{
			if (!isRecording) return;

			var dir = Path.GetDirectoryName(filename);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
			using (var filestream = new FileStream(filename, FileMode.Create, FileAccess.Write))
			{
				EndRecording(filestream, sceneName, settings);
			}
		}

		public void EndRecording(Stream stream, string sceneName = "scene", GLTFSettings settings = null)
		{
			if (!isRecording) return;
			isRecording = false;

#if USE_ANIMATION_POINTER
			Debug.Log("Gltf Recording saved. Tracks: " + data.Count + ", Total Keyframes: " + data.Sum(x => x.Value.tracks.Sum(y => y.values.Count())));
#elif USE_REGULAR_ANIMATION
			Debug.Log("Gltf Recording saved. Tracks: " + data.Count + ", Total Keyframes: " + data.Sum(x => x.Value.keys.Count));
#endif

			if (!settings)
			{
				var adjustedSettings = Object.Instantiate(GLTFSettings.GetOrCreateSettings());
				adjustedSettings.ExportDisabledGameObjects = true;
				adjustedSettings.ExportAnimations = false;
				settings = adjustedSettings;
			}

			var logHandler = new StringBuilderLogHandler();

			var exporter = new GLTFSceneExporter(new Transform[] { root }, new ExportOptions(settings)
			{
				AfterSceneExport = PostExport,
				logger = new Logger(logHandler),
			});

			exporter.SaveGLBToStream(stream, sceneName);

			logHandler.LogAndClear();
		}

		private void PostExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
		{
			// this would include animation from the original root
			// exporter.ExportAnimationFromNode(ref root);

			GLTFAnimation anim = new GLTFAnimation();
			anim.Name = "Recording";

			CollectAndProcessAnimation(exporter, anim);

			if (anim.Channels.Count > 0 && anim.Samplers.Count > 0)
				gltfRoot.Animations.Add(anim);
		}

		private static ProfilerMarker processAnimationMarker = new ProfilerMarker("Process Animation");
		private static ProfilerMarker simplifyKeyframesMarker = new ProfilerMarker("Simplify Keyframes");
		private static ProfilerMarker convertValuesMarker = new ProfilerMarker("Convert Values to Arrays");
		private void CollectAndProcessAnimation(GLTFSceneExporter gltfSceneExporter, GLTFAnimation anim)
		{
			foreach (var kvp in data)
			{
				processAnimationMarker.Begin();
#if USE_ANIMATION_POINTER
				foreach (var tr in kvp.Value.tracks)
				{
					if (tr.times.Length == 0) continue;
					var times = tr.times;
					var values = tr.values;
					gltfSceneExporter.RemoveUnneededKeyframes(ref times, ref values);
					gltfSceneExporter.AddAnimationData(tr.animatedObject, tr.propertyName, anim, times, values);
				}
#endif

#if USE_REGULAR_ANIMATION
				if (kvp.Value.keys.Count < 1)
				{
					processAnimationMarker.End();
					continue;
				}

				var times = kvp.Value.keys.Keys.Select(x => (float)x).ToArray();
				var values = kvp.Value.keys.Values.ToArray();
				var positions = values.Select(x => x.position).ToArray();
				var rotations = values.Select(x => x.rotation).ToArray();
				var scales = values.Select(x => x.scale).ToArray();
				float[] weights = null;
				int weightCount = 0;
				if (values.All(x => x.weights != null))
				{
					weights = values.SelectMany(x => x.weights).ToArray();
					weightCount = values.First().weights.Length;
				}

				// gltfSceneExporter.RemoveUnneededKeyframes(ref times, ref positions, ref rotations, ref scales, ref weights, ref weightCount);

				// no need to add single-keyframe tracks, that's recorded as base data anyways
				// if (times.Length > 1)
				// 	gltfSceneExporter.AddAnimationData(kvp.Key, anim, times, positions, rotations, scales, weights);
#endif
				processAnimationMarker.End();
			}
		}

#if USE_REGULAR_ANIMATION
		// TODO deprecate this once the ExportPlan approach is fully tested.
		internal readonly struct FrameData
		{
			public readonly Vector3 position;
			public readonly Quaternion rotation;
			public readonly Vector3 scale;
			public readonly float[] weights;

			public FrameData(Transform tr, SkinnedMeshRenderer smr, bool zeroScale, bool recordBlendShapes, bool inWorldSpace)
			{
				if (!inWorldSpace)
				{
					position = tr.localPosition;
					rotation = tr.localRotation;
					scale = zeroScale ? Vector3.zero : tr.localScale;
				}
				else
				{
					position = tr.position;
					rotation = tr.rotation;
					scale = zeroScale ? Vector3.zero : tr.lossyScale;
				}

				if (recordBlendShapes)
				{
					if (smr && smr.sharedMesh && smr.sharedMesh.blendShapeCount > 0)
					{
						var mesh = smr.sharedMesh;
						var blendShapeCount = mesh.blendShapeCount;
						weights = new float[blendShapeCount];
						for (var i = 0; i < blendShapeCount; i++)
							weights[i] = smr.GetBlendShapeWeight(i);
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
					if (weights != null)
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
#endif

		private class StringBuilderLogHandler : ILogHandler
		{
			private readonly StringBuilder sb = new StringBuilder();

			private string LogTypeToLog(LogType logType)
			{
#if UNITY_EDITOR
				// create strings with <color> tags
				switch (logType)
				{
					case LogType.Error:
						return "<color=red>[" + logType + "]</color>";
					case LogType.Assert:
						return "<color=red>[" + logType + "]</color>";
					case LogType.Warning:
						return "<color=yellow>[" + logType + "]</color>";
					case LogType.Log:
						return "[" + logType + "]";
					case LogType.Exception:
						return "<color=red>[" + logType + "]</color>";
					default:
						return "[" + logType + "]";
				}
#else
				return "[" + logType + "]";
#endif
			}

			public void LogFormat(LogType logType, Object context, string format, params object[] args) => sb.AppendLine($"{LogTypeToLog(logType)} {string.Format(format, args)} [Context: {context}]");
			public void LogException(Exception exception, Object context) => sb.AppendLine($"{LogTypeToLog(LogType.Exception)} {exception} [Context: {context}]");

			public void LogAndClear()
			{
				if(sb.Length > 0)
				{
					var str = sb.ToString();
#if UNITY_2019_1_OR_NEWER
					var logType = LogType.Log;
#if UNITY_EDITOR
					if (str.IndexOf("[Error]", StringComparison.Ordinal) > -1 ||
					    str.IndexOf("[Exception]", StringComparison.Ordinal) > -1 ||
					    str.IndexOf("[Assert]", StringComparison.Ordinal) > -1)
						logType = LogType.Error;
					else if (str.IndexOf("[Warning]", StringComparison.Ordinal) > -1)
						logType = LogType.Warning;
#endif
					Debug.LogFormat(logType, LogOption.NoStacktrace, null, "Export Messages:" + "\n{0}", sb.ToString());
#else
					Debug.Log(string.Format("Export Messages:" + "\n{0}", str));
#endif
				}
				sb.Clear();
			}
		}
	}
}
