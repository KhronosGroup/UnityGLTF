using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Plugins.Experimental
{
    public class KHRGlobalAudioEmitterBehaviour : MonoBehaviour
    {
        public List<AudioSourceScriptableObject> sources;
        public float gain = 1.0f;
    }
}