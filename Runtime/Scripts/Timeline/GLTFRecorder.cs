using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GLTF.Schema;
using Unity.Profiling;
using UnityEngine;
using UnityGLTF.Timeline.Samplers;
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
		private Dictionary<Transform, AnimationData> recordingAnimatedTransforms = new Dictionary<Transform, AnimationData>(64);
		private double startTime;
		private double lastSampleTimeSinceStart;
		private bool hasRecording;
		private bool isRecording;
		
		private readonly bool recordBlendShapes;
		private readonly bool recordRootInWorldSpace;
		private readonly bool recordAnimationPointer;

		public bool HasRecording => hasRecording;
		public bool IsRecording => isRecording;

		public double LastSampleTimeSinceStart => lastSampleTimeSinceStart;

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
			public float[] Times;
			public object[] Values;
			
			public Object AnimatedObject { get; }
			public string PropertyName { get; }
			
			internal PostAnimationData(Object animatedObject, string propertyName, float[] times, object[] values) {
				this.AnimatedObject = animatedObject;
				this.PropertyName = propertyName;
				this.Times = times;
				this.Values = values;
			}
		}
		
		public void StartRecording(double time)
		{
			startTime = time;
			lastSampleTimeSinceStart = 0;
			var trs = root.GetComponentsInChildren<Transform>(true);
			recordingAnimatedTransforms.Clear();

			foreach (var tr in trs)
			{
				if (!AllowRecordingTransform(tr)) continue;
				recordingAnimatedTransforms.Add(tr, new AnimationData(tr, lastSampleTimeSinceStart, recordBlendShapes, recordRootInWorldSpace && tr == root, recordAnimationPointer));
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

			if (time <= lastSampleTimeSinceStart)
			{
				Debug.LogWarning("Can't record backwards in time, please avoid this.");
				return;
			}

			var timeSinceStart = time - startTime;
			var trs = root.GetComponentsInChildren<Transform>(true);
			foreach (var tr in trs)
			{
				if (!AllowRecordingTransform(tr)) continue;
				if (!recordingAnimatedTransforms.ContainsKey(tr))
				{
					// insert "empty" frame with scale=0,0,0 because this object might have just appeared in this frame
					var emptyData = new AnimationData(tr, lastSampleTimeSinceStart, true, recordBlendShapes, recordAnimationPointer);
					recordingAnimatedTransforms.Add(tr, emptyData);
				}
				recordingAnimatedTransforms[tr].Update(timeSinceStart);
			}

			lastSampleTimeSinceStart = time;
		}
		
		internal void EndRecording(out Dictionary<Transform, AnimationData> param)
		{
			param = null;
			if (!hasRecording) return;
			param = recordingAnimatedTransforms;
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
			Debug.Log("Gltf Recording saved. Tracks: " + recordingAnimatedTransforms.Count + ", Total Keyframes: " + recordingAnimatedTransforms.Sum(x => x.Value.tracks.Sum(y => y.Values.Count())));

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

			foreach (var kvp in recordingAnimatedTransforms)
			{
				processAnimationMarker.Begin();
				foreach (var tr in kvp.Value.tracks)
				{
					if (tr.Times.Length == 0) continue;

					PostAnimationData postAnimation = null;
					if (tr.PropertyName == "scale" && tr is AnimationTrack<Transform, Vector3> scaleTrack) {
						var (mergedTimes, mergedScales) = mergeVisibilityAndScaleTracks(kvp.Value.visibilityTrack, scaleTrack);
						postAnimation = new PostAnimationData(tr.AnimatedObject, tr.PropertyName, mergedTimes.Select(dbl => (float)dbl).ToArray(), mergedScales.Cast<object>().ToArray());	
					}
					else {
						postAnimation = new PostAnimationData(tr.AnimatedObject, tr.PropertyName, tr.Times.Select(dbl => (float)dbl).ToArray(), tr.Values);	
					}
					OnBeforeAddAnimationData?.Invoke(postAnimation);

					if (calculateTranslationBounds && tr.PropertyName == "translation")
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
					gltfSceneExporter.AddAnimationData(tr.AnimatedObject, tr.PropertyName, anim, postAnimation.Times, postAnimation.Values);
				}
				processAnimationMarker.End();
			}
		}

		private static (double[], Vector3[]) mergeVisibilityAndScaleTracks(
			VisibilityTrack visibilityTrack,
			AnimationTrack<Transform, Vector3> scaleTrack
		) {
			if (visibilityTrack == null && scaleTrack == null) return (null, null);
			if (visibilityTrack == null) return (scaleTrack.Times, scaleTrack.values);
			var visTimes = visibilityTrack.Times;
			var visValues = visibilityTrack.values;
			var visScaleValues = visValues.Select(vis => vis ? Vector3.one : Vector3.zero).ToArray();
			if (scaleTrack == null)
				return (visTimes, visScaleValues);
			// both tracks are present, need to merge, but visibility always takes precedence

			var mergedLinkedList = new LinkedList<(double, bool, Vector3)>();

			for (int visibleIndex = 0; visibleIndex < visValues.Length; visibleIndex++) {
				var vis = visValues[visibleIndex];
				mergedLinkedList.AddLast(
					new LinkedListNode<(double, bool, Vector3)>(
						new(visTimes[visibleIndex], vis, vis ? Vector3.one : Vector3.zero)
					)
				);
			}
			var scaleTimes = scaleTrack.Times;
			var scaleValues = scaleTrack.values;
			
			LinkedListNode<(double, bool, Vector3)> nextNode = mergedLinkedList.First;
			
			for (int scaleIndex = 0; scaleIndex < scaleTimes.Length; scaleIndex++) {
				var scaleTime = scaleTimes[scaleIndex];
				var scale = scaleValues[scaleIndex];
				
				// prev visibility is "visible" & scale change happens before next vis change
				if ((nextNode.Previous?.Value.Item2 ?? false) && scaleTime < nextNode.Value.Item1)
					mergedLinkedList.AddBefore(
						nextNode,
						new LinkedListNode<(double, bool, Vector3)>(new(scaleTime, true, scale))
					);
			}

			var mergedTimes = new double[mergedLinkedList.Count];
			var mergedScales = new Vector3[mergedLinkedList.Count];

			var ind = 0;
			foreach (var (time, _, scale) in mergedLinkedList) {
				mergedTimes[ind] = time;
				mergedScales[ind] = scale;
				ind++;
			}
			return (mergedTimes, mergedScales);
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
