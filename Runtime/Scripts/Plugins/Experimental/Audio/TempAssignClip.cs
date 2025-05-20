using UnityEngine;

namespace UnityGLTF.Plugins
{
    /// <summary>
    /// Helper class to assign audio clips to AudioSources when importing a glTF file
    /// </summary>
    internal class TempAssignClip : MonoBehaviour
    {
        public int audioSourceIndex;
        public string audioPath;
    }
}