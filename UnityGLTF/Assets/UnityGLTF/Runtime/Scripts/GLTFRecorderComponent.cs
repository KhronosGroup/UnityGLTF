using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityGLTF.Timeline;
#if UNITY_EDITOR
using UnityEditor.ShortcutManagement;
#endif

namespace UnityGLTF
{
	public class GLTFRecorderComponent : MonoBehaviour
	{
	    public string outputFile = "Assets/Recordings/Recorded_<Timestamp>.glb";
	    public Transform exportRoot;
	    public bool allowRecordingInEditor = false;

	    public bool IsRecording => recorder?.IsRecording ?? false;
	    private GLTFRecorder recorder;

	    public UnityEvent recordingStarted;
		public UnityEvent<string> recordingEnded;

#if UNITY_EDITOR
	    [Shortcut("gltf-recording-toggle", KeyCode.F11, displayName = "Start/Stop GLTF Recording")]
	    private static void ToggleRecording()
	    {
	        var recorderComponent = FindObjectOfType<GLTFRecorderComponent>();
	        if(recorderComponent) {
	            if (!recorderComponent.exportRoot || string.IsNullOrEmpty(recorderComponent.outputFile))
	            {
	                Debug.LogError($"Can't record, please assign exportRoot and outputFile on {nameof(GLTFRecorderComponent)}", recorderComponent);
	                return;
	            }

	            if (!recorderComponent.allowRecordingInEditor && !Application.isPlaying)
	            {
	                Debug.LogWarning("Can't start recording: application needs to be playing", recorderComponent);
	                return;
	            }

	            if(recorderComponent.IsRecording)
	            {
	                recorderComponent.StopRecording();
	                Debug.Log("Recording Stopped", recorderComponent);
	            }
	            else {
	                recorderComponent.StartRecording();
	                Debug.Log("Recording Started", recorderComponent);
	            }
	        }
	        else
	        {
	            Debug.LogWarning($"Trying to start/stop recording but no {nameof(GLTFRecorderComponent)} is found. Please make sure a recorder exists in the scene (add {nameof(recorderComponent)} and assign a recording target");
	        }
	    }
#endif

	    [ContextMenu("Start Recording")]
	    public void StartRecording()
	    {
		    if (!isActiveAndEnabled) return;

	        recorder = new GLTFRecorder(exportRoot);
	        recorder.StartRecording(Time.timeAsDouble);
	        recordingStarted?.Invoke();

	        StartCoroutine(_UpdateRecording());
	    }

	    private void OnDisable()
	    {
		    if(IsRecording)
			    StopRecording();
	    }

	    [ContextMenu("Stop Recording")]
	    public void StopRecording()
	    {
	        var filename = outputFile.Replace("<Timestamp>", System.DateTime.Now.ToString("yyyyMMdd-HHmmss"));
	        recorder.EndRecording(filename);
	        recordingEnded?.Invoke(filename);
	    }

	    private IEnumerator _UpdateRecording()
	    {
	        while (true)
	        {
	            yield return new WaitForEndOfFrame();
	            if (!IsRecording)
	                yield break;

	            recorder.UpdateRecording(Time.timeAsDouble);
	        }
	    }
	}
}
