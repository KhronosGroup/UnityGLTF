using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class GLTFInteractivityData : MonoBehaviour
    {
        public string interactivityJson;
        public PointerResolver pointerReferences;
        public GLTFInteractivityAnimationWrapper animationWrapper;
        [HideInInspector] public bool showData = false;
    }
}
