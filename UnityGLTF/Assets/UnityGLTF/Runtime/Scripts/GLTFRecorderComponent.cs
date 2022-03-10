using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityGLTF.Timeline;

namespace UnityGLTF
{
	public class GLTFRecorderComponent : MonoBehaviour
	{
	    public string outputFile = "Assets/Recordings/Recorded_<Timestamp>.glb";
	    public Transform exportRoot;
	    public bool recordBlendShapes = true;
	    public KeyCode recordingKey = KeyCode.F11;

	    public bool IsRecording => recorder?.IsRecording ?? false;
	    protected GLTFRecorder recorder;

	    public UnityEvent recordingStarted;
		public UnityEvent<string> recordingEnded;

		private double CurrentTime =>
#if UNITY_2020_1_OR_NEWER
			Time.timeAsDouble;
#else
			Time.time;
#endif

		[ContextMenu("Start Recording")]
		public virtual void StartRecording()
		{
			if (!isActiveAndEnabled) return;

			var settings = GLTFSettings.GetOrCreateSettings();
			var shouldRecordBlendShapes = recordBlendShapes;
			if (settings.BlendShapeExportProperties == GLTFSettings.BlendShapeExportPropertyFlags.None && recordBlendShapes)
			{
				Debug.LogWarning("Attempting to record blend shapes but export is disabled in ProjectSettings/UnityGLTF. Set BlendShapeExportProperties to something other than \"None\" if you want to export them.");
				shouldRecordBlendShapes = false;
			}

			recorder = new GLTFRecorder(exportRoot, shouldRecordBlendShapes);
			recorder.StartRecording(CurrentTime);
			recordingStarted?.Invoke();

			StartCoroutine(_UpdateRecording());
		}

		[ContextMenu("Stop Recording")]
		public virtual void StopRecording()
		{
			var filename = outputFile.Replace("<Timestamp>", System.DateTime.Now.ToString("yyyyMMdd-HHmmss"));
			recorder.EndRecording(filename);
			recordingEnded?.Invoke(filename);
		}

		protected virtual void Update()
		{
			if(Input.GetKeyDown(recordingKey))
				ToggleRecording();
		}

		private void ToggleRecording()
	    {
            if (!exportRoot || string.IsNullOrEmpty(outputFile))
            {
                Debug.LogError($"[GLTFRecorderComponent] Can't record, please assign exportRoot and outputFile on {nameof(GLTFRecorderComponent)}", this);
                return;
            }

            if(IsRecording)
            {
                StopRecording();
                Debug.Log("Recording Stopped", this);
            }
            else {
                StartRecording();
                Debug.Log("Recording Started", this);
            }
	    }

	    protected virtual void OnDisable()
	    {
		    if(IsRecording)
			    StopRecording();
	    }

	    protected virtual void UpdateRecording()
	    {
		    if(CurrentTime > recorder.LastRecordedTime)
				recorder.UpdateRecording(CurrentTime);
	    }

	    private IEnumerator _UpdateRecording()
	    {
	        while (true)
	        {
	            yield return new WaitForEndOfFrame();
	            if (!IsRecording)
	                yield break;

	            UpdateRecording();
	        }
	    }
	}
}
