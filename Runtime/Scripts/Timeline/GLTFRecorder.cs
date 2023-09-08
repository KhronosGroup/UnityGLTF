#define USE_ANIMATION_POINTER

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
		public GLTFRecorder(Transform root, bool recordBlendShapes = true, bool recordRootInWorldSpace = false, bool recordAnimationPointer = false, bool addBoundsMarkerNodes = false)
		{
			if (!root)
				throw new ArgumentNullException(nameof(root), "Please provide a root transform to record.");

			this.root = root;
			this.recordBlendShapes = recordBlendShapes;
			this.recordRootInWorldSpace = recordRootInWorldSpace;
			this.recordAnimationPointer = recordAnimationPointer;
			this.addBoundsMarkerNodes = addBoundsMarkerNodes;
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
		private readonly bool addBoundsMarkerNodes;

		public bool IsRecording => isRecording;
		public double LastRecordedTime => lastRecordedTime;
		public string AnimationName = "Recording";

		internal class AnimationData
		{
			private Transform tr;
			private SkinnedMeshRenderer smr;
			private bool recordBlendShapes;
			private bool inWorldSpace = false;
			private bool recordAnimationPointer;

#if USE_ANIMATION_POINTER
			private static List<ExportPlan> exportPlans;
			private static MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			internal List<Track> tracks = new List<Track>();

			internal class ExportPlan
			{
				public string propertyName;
				public Type dataType;
				public Func<Transform, Object> GetTarget;
				public Func<Transform, Object, AnimationData, object> GetData;

				public ExportPlan(string propertyName, Type dataType, Func<Transform, Object> GetTarget, Func<Transform, Object, AnimationData, object> GetData)
				{
					this.propertyName = propertyName;
					this.dataType = dataType;
					this.GetTarget = GetTarget;
					this.GetData = GetData;
				}

				public object Sample(AnimationData data)
				{
					var target = GetTarget(data.tr);
					return GetData(data.tr, target, data);
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

				if (exportPlans == null)
				{
					exportPlans = new List<ExportPlan>();
					exportPlans.Add(new ExportPlan("translation", typeof(Vector3), x => x, (tr0, _, options) => options.inWorldSpace ? tr0.position : tr0.localPosition));
					exportPlans.Add(new ExportPlan("rotation", typeof(Quaternion), x => x, (tr0, _, options) =>
					{
						var q = options.inWorldSpace ? tr0.rotation : tr0.localRotation;
						return new Quaternion(q.x, q.y, q.z, q.w);
					}));
					exportPlans.Add(new ExportPlan("scale", typeof(Vector3), x => x, (tr0, _, options) => options.inWorldSpace ? tr0.lossyScale : tr0.localScale));

					if (recordBlendShapes)
					{
						exportPlans.Add(new ExportPlan("weights", typeof(float[]),
							x => x.GetComponent<SkinnedMeshRenderer>(), (tr0, x, options) =>
							{
								if (x is SkinnedMeshRenderer skinnedMesh && skinnedMesh.sharedMesh)
								{
									var mesh = skinnedMesh.sharedMesh;
									var blendShapeCount = mesh.blendShapeCount;
									if (blendShapeCount == 0) return null;
									var weights = new float[blendShapeCount];
									for (var i = 0; i < blendShapeCount; i++)
										weights[i] = skinnedMesh.GetBlendShapeWeight(i);
									return weights;
								}

								return null;
							}));
					}

					if (recordAnimationPointer)
					{
						// TODO add other animation pointer export plans

						exportPlans.Add(new ExportPlan("baseColorFactor", typeof(Color), x => x.GetComponent<MeshRenderer>() ? x.GetComponent<MeshRenderer>().sharedMaterial : null, (tr0, mat, options) =>
						{
							var r = tr0.GetComponent<Renderer>();

							if (r.HasPropertyBlock())
							{
								r.GetPropertyBlock(materialPropertyBlock);
	#if UNITY_2021_1_OR_NEWER
								if (materialPropertyBlock.HasColor("_BaseColor")) return materialPropertyBlock.GetColor("_BaseColor").linear;
								if (materialPropertyBlock.HasColor("_Color")) return materialPropertyBlock.GetColor("_Color").linear;
								if (materialPropertyBlock.HasColor("baseColorFactor")) return materialPropertyBlock.GetColor("baseColorFactor").linear;
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
								if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor").linear;
								if (m.HasProperty("_Color")) return m.GetColor("_Color").linear;
								if (m.HasProperty("baseColorFactor")) return m.GetColor("baseColorFactor").linear;
							}

							return null;
						}));
					}
				}

				foreach (var plan in exportPlans)
				{
					if (plan.GetTarget(tr)) {
						tracks.Add(new Track(this, plan, time));
					}
				}
			}

			internal class Track
			{
				public Object animatedObject => plan.GetTarget(tr.tr);
				public string propertyName => plan.propertyName;
				// TODO sample as floats?
				public float[] times => samples.Keys.Select(x => (float) x).ToArray();
				public object[] values => samples.Values.ToArray();

				private AnimationData tr;
				private ExportPlan plan;
				private Dictionary<double, object> samples;
				private object lastSample;

				public Track(AnimationData tr, ExportPlan plan, double time)
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

			public void Update(double time)
			{
				foreach (var track in tracks)
				{
					track.SampleIfChanged(time);
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

			Debug.Log("Gltf Recording saved. Tracks: " + data.Count + ", Total Keyframes: " + data.Sum(x => x.Value.tracks.Sum(y => y.values.Count())));

			if (!settings)
			{
				var adjustedSettings = Object.Instantiate(GLTFSettings.GetOrCreateSettings());
				adjustedSettings.ExportDisabledGameObjects = true;
				adjustedSettings.ExportAnimations = false;
				settings = adjustedSettings;
			}

			if (!recordBlendShapes)
					settings.BlendShapeExportProperties = GLTFSettings.BlendShapeExportPropertyFlags.None;

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
			anim.Name = AnimationName;

			CollectAndProcessAnimation(exporter, anim, addBoundsMarkerNodes, out Bounds translationBounds);

			if (addBoundsMarkerNodes)
			{
				Debug.Log("Animation bounds: " + translationBounds.center + " => " + translationBounds.size);

				// create Cube primitive
				var cube = GameObject.CreatePrimitive(PrimitiveType.Quad);
				cube.transform.localScale = Vector3.one * 0.0001f;
				cube.hideFlags = HideFlags.HideAndDontSave;

				var collider = cube.GetComponent<MeshCollider>();
				SafeDestroy(collider);

				var boundsRoot = new GameObject("AnimationBounds");
				boundsRoot.hideFlags = HideFlags.HideAndDontSave;
				boundsRoot.transform.position = translationBounds.center;

				var extremePointsOnBounds = new Vector3[6];
				extremePointsOnBounds[0] = translationBounds.center + new Vector3(+translationBounds.extents.x, 0, 0);
				extremePointsOnBounds[1] = translationBounds.center + new Vector3(-translationBounds.extents.x, 0, 0);
				extremePointsOnBounds[2] = translationBounds.center + new Vector3(0, +translationBounds.extents.y, 0);
				extremePointsOnBounds[3] = translationBounds.center + new Vector3(0, -translationBounds.extents.y, 0);
				extremePointsOnBounds[4] = translationBounds.center + new Vector3(0, 0, +translationBounds.extents.z);
				extremePointsOnBounds[5] = translationBounds.center + new Vector3(0, 0, -translationBounds.extents.z);

				foreach (var point in extremePointsOnBounds)
				{
					var cubeInstance = Object.Instantiate(cube);
					cubeInstance.name = "AnimationBounds";
					cubeInstance.transform.position = point;
					cubeInstance.transform.parent = boundsRoot.transform;
				}

				// export and add explicitly to the scene list, otherwise these nodes at the root level will be ignored
				var nodeId = exporter.ExportNode(boundsRoot);
				gltfRoot.Scenes[0].Nodes.Add(nodeId);

				// move skinned meshes to the center of the bounds –
				// they will be moved by their bones anyways, but this prevents wrong bounds calculations from them.
				foreach (var skinnedMeshRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
					skinnedMeshRenderer.transform.position = translationBounds.center;

				SafeDestroy(boundsRoot);
				SafeDestroy(cube);
			}

			// check if the root node is outside these bounds, move it to the center if necessary

			if (anim.Channels.Count > 0 && anim.Samplers.Count > 0)
				gltfRoot.Animations.Add(anim);
		}

		private static void SafeDestroy(Object obj)
		{
			if (Application.isEditor)
				Object.DestroyImmediate(obj);
			else
				Object.Destroy(obj);
		}

		private static ProfilerMarker processAnimationMarker = new ProfilerMarker("Process Animation");
		private static ProfilerMarker simplifyKeyframesMarker = new ProfilerMarker("Simplify Keyframes");
		private static ProfilerMarker convertValuesMarker = new ProfilerMarker("Convert Values to Arrays");

		private void CollectAndProcessAnimation(GLTFSceneExporter gltfSceneExporter, GLTFAnimation anim, bool calculateTranslationBounds, out Bounds translationBounds)
		{
			var gotFirstValue = false;
			translationBounds = new Bounds();

			foreach (var kvp in data)
			{
				processAnimationMarker.Begin();
				foreach (var tr in kvp.Value.tracks)
				{
					if (tr.times.Length == 0) continue;
					var times = tr.times;
					var values = tr.values;

					if (calculateTranslationBounds && tr.propertyName == "translation")
					{
						for (var i = 0; i < values.Length; i++)
						{
							var vec = (Vector3) values[i];
							if (!gotFirstValue)
							{
								translationBounds = new Bounds(vec, Vector3.zero);
								gotFirstValue = true;
							}
							else
							{
								translationBounds.Encapsulate(vec);
							}
						}
					}

					gltfSceneExporter.RemoveUnneededKeyframes(ref times, ref values);
					gltfSceneExporter.AddAnimationData(tr.animatedObject, tr.propertyName, anim, times, values);
				}
				processAnimationMarker.End();
			}
		}

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
