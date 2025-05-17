using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public static class Util
    {
        [System.Diagnostics.Conditional("DEBUG_MESSAGES")]
        public static void Log( string message, UnityEngine.Object context = null)
        {
            Debug.Log(message, context);
        }

        [System.Diagnostics.Conditional("DEBUG_MESSAGES")]
        public static void LogWarning(string message, UnityEngine.Object context = null)
        {
            Debug.LogWarning(message, context);
        }

        [System.Diagnostics.Conditional("DEBUG_MESSAGES")]
        public static void LogError(string message, UnityEngine.Object context = null)
        {
            Debug.LogError(message, context);
        }

        [System.Diagnostics.Conditional("DEBUG_MESSAGES")]
        public static void Log(string className, string message, UnityEngine.Object context = null)
        {
            Debug.Log($"{className}: {message}", context);
        }

        [System.Diagnostics.Conditional("DEBUG_MESSAGES")]
        public static void LogWarning(string className, string message, UnityEngine.Object context = null)
        {
            Debug.LogWarning($"{className}: {message}", context);
        }

        [System.Diagnostics.Conditional("DEBUG_MESSAGES")]
        public static void LogError(string className, string message, UnityEngine.Object context = null)
        {
            Debug.LogError($"{className}: {message}", context);
        }
    }
}