#if HAVE_TIMELINE

using UnityEngine;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF.Timeline
{
	public class GLTFRecorderBehaviour : PlayableBehaviour
	{
	    private GLTFRecorder recorder = null;

	    private void BeginRecording(double getTime, Transform getExportRoot)
	    {
	        if (!getExportRoot)
	        {
	            Debug.LogError("Can't record: export root is null");
	            recorder = null;
	            return;
	        }

	        Time.captureFramerate = Clip.m_CaptureFrameRate;

	        // will be initialized lazy to ensure that the first frame is correct and not pre-timeline refresh
	        recorder = null;
	    }

	    private void StopRecording(double getTime)
	    {
	        recorder?.EndRecording(Clip.m_File);
	    }

	    private void ProcessRecording(double getTime, Transform getExportRoot)
	    {
		    if (recorder == null)
		    {
			    recorder = new GLTFRecorder(getExportRoot, Clip.m_RecordBlendShapes, recordAnimationPointer: Clip.m_RecordAnimationPointer);
			    recorder.AnimationName = Clip.m_AnimationName;
			    recorder.StartRecording(getTime);
		    }
		    else if (getTime > recorder.LastRecordedTime)
		    {
			    recorder?.UpdateRecording(getTime);
		    }
	    }

	    public GLTFRecorderClip Clip;
	    private bool m_isPaused = false;

	    private static bool IsPlaying()
	    {
	#if UNITY_EDITOR
	        return EditorApplication.isPlaying;
	#else
	        return true;
	#endif
	    }

	    public override void OnPlayableDestroy(Playable playable)
	    {
		    if (!IsPlaying()) return;

	        StopRecording(playable.GetTime());
	    }

	    public override void OnGraphStart(Playable playable)
	    {
		    if (!IsPlaying()) return;

	        BeginRecording(playable.GetTime(), Clip.GetExportRoot(playable.GetGraph()));
	    }

	    public override void OnGraphStop(Playable playable)
	    {
		    if (!IsPlaying()) return;

	        StopRecording(playable.GetTime());
	    }

	    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	    {
		    if (!IsPlaying()) return;

	        double time = playable.GetTime();
	        GLTFRecorderHelper.Add(() => OnFrameEnd(time, playable, info, playerData));
	    }

	    public override void OnBehaviourPlay(Playable playable, FrameData info)
	    {
	        m_isPaused = false;
	    }

	    public override void OnBehaviourPause(Playable playable, FrameData info)
	    {
	        m_isPaused = true;
	    }

	    public void OnFrameEnd(double time, Playable playable, FrameData info, object playerData)
	    {
		    if (!playable.IsValid()) return;
		    var root = Clip.GetExportRoot(playable.GetGraph());
	        if (!root || m_isPaused) return;

	        ProcessRecording(time, root);
	    }
	}
}

#endif
