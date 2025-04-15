using UnityEngine;

namespace UnityGLTF.Plugins.Audio
{
    /// <summary>
    /// Helper class to assign audio clips to AudioSources when importing a glTF file
    /// </summary>
    public class TempAssignClip : MonoBehaviour
    {
        public int audioSourceIndex;
        public string audioPath;
    }
}