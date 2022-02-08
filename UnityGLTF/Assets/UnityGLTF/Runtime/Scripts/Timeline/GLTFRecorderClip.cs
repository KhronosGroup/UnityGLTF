#if HAVE_TIMELINE

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityGLTF.Timeline
{
	[System.ComponentModel.DisplayName("glTF Recorder Clip")]
	public class GLTFRecorderClip : PlayableAsset, ITimelineClipAsset
	{
	    public ExposedReference<Transform> m_exportRoot;
	    public string m_File = "Assets/Recording.glb";
	    public int m_CaptureFrameRate = 60;
	    public bool m_RecordBlendShapes = true;

	    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	    {
	        var ret = ScriptPlayable<GLTFRecorderBehaviour>.Create(graph);
	        var behaviour = ret.GetBehaviour();
	        behaviour.Clip = this; // TOD check if needed
	        return ret;
	    }

	    public ClipCaps clipCaps { get; }

	    public Transform GetExportRoot(PlayableGraph graph)
	    {
	        return m_exportRoot.Resolve(graph.GetResolver());
	    }
	}
}

#endif
