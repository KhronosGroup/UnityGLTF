using UnityEngine;

namespace UnityGLTF.Plugins.Experimental
{
    [CreateAssetMenu(fileName = "AudioSource", menuName = "UnityGLTF/KHR_audio/AudioSource", order = 1)]
    public class AudioSourceScriptableObject : ScriptableObject
    {
        public AudioClip clip;
        public float gain = 1.0f;
        public bool autoPlay = true;
        public bool loop = true;
    }
}