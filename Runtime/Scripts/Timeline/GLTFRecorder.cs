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
		private bool hasRecording;
		private bool isRecording;
		
		private readonly bool recordBlendShapes;
		private readonly bool recordRootInWorldSpace;
		private readonly bool recordAnimationPointer;

		public bool HasRecording => hasRecording;
		public bool IsRecording => isRecording;

		public double LastRecordedTime => lastRecordedTime;

		public string AnimationName = "Recording";

		public delegate void OnBeforeAddAnimationDataDelegate(PostAnimationData animationData);
		public delegate void OnPostExportDelegate(PostExportArgs animationData);
		
		/// <summary>
		/// Callback to modify the animation data before it is added to the animation.
		/// Is called once for each track after the recording has ended. This is a non destructive callback,
		/// so the original recorded data is not modified. Every time you call EndRecording to the save the gltf/glb,
		/// you can modify the data again. 
		/// </summary>
		public OnBeforeAddAnimationDataDelegate OnBeforeAddAnimationData;
		
		/// <summary>
		/// Callback to modify or add additional data to the gltf root after the recording has ended and animation
		/// data is added to the animation.
		/// </summary>
		public OnPostExportDelegate OnPostExport;

		public class PostExportArgs
		{
			public Bounds AnimationTranslationBounds { get; private set; }
			public GLTFSceneExporter Exporter { get; private set; }
			public GLTFRoot GltfRoot { get; private set; }		
			
			internal PostExportArgs(Bounds animationTranslationBounds, GLTFSceneExporter exporter, GLTFRoot gltfRoot)
			{
				this.AnimationTranslationBounds = animationTranslationBounds;
				this.Exporter = exporter;
				this.GltfRoot = gltfRoot;
			}
		}

		public class PostAnimationData
		{
			internal AnimationTrack _animationAnimationTrack;
			public float[] Times;
			public object[] Values;
			
			public Object AnimatedObject => _animationAnimationTrack.animatedObject;
			public string PropertyName => _animationAnimationTrack.propertyName;
			
			internal PostAnimationData(AnimationTrack tr, float[] times, object[] values)
			{
				this._animationAnimationTrack = tr;
				this.Times = times;
				this.Values = values;
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
			hasRecording = true;
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
			if (!hasRecording) return;
			param = data;
		}

		public void EndRecording(string filename, string sceneName = "scene", GLTFSettings settings = null)
		{
			if (!hasRecording) return;

			var dir = Path.GetDirectoryName(filename);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
			using (var filestream = new FileStream(filename, FileMode.Create, FileAccess.Write))
			{
				EndRecording(filestream, sceneName, settings);
			}
		}

		public void EndRecording(Stream stream, string sceneName = "scene", GLTFSettings settings = null)
		{
			if (!hasRecording) return;
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

			CollectAndProcessAnimation(exporter, anim, true, out Bounds translationBounds);

			if (anim.Channels.Count > 0 && anim.Samplers.Count > 0)
				gltfRoot.Animations.Add(anim);
			
			OnPostExport?.Invoke( new PostExportArgs(translationBounds, exporter, gltfRoot));
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
					
					var postAnimation = new PostAnimationData(tr, tr.times.Select(dbl => (float)dbl).ToArray(), tr.values);
					OnBeforeAddAnimationData?.Invoke(postAnimation);

					if (calculateTranslationBounds && tr.propertyName == "translation")
					{
						for (var i = 0; i < postAnimation.Values.Length; i++)
						{
							var vec = (Vector3) postAnimation.Values[i];
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

					GLTFSceneExporter.RemoveUnneededKeyframes(ref postAnimation.Times, ref postAnimation.Values);
					gltfSceneExporter.AddAnimationData(tr.animatedObject, tr.propertyName, anim, postAnimation.Times, postAnimation.Values);
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
