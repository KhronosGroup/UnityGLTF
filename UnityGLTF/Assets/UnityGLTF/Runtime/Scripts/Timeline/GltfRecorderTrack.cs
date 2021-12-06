#if HAVE_TIMELINE

using UnityEngine.Timeline;

namespace UnityGLTF.Timeline
{
	[System.Serializable]
	[TrackClipType(typeof(GltfRecorderClip))]
	[TrackColor(0.7f, 0.0f, 0.0f)]
	public class GltfRecorderTrack : TrackAsset
	{

	}
}

#endif
