using UnityEngine;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityGLTF.Interactivity.Playback
{
    [CustomEditor(typeof(GLTFInteractivityAnimationWrapper))]
    public class GLTFInteractivityAnimationWrapper_Inspector : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Required to support animations during interactivity playback.");
            EditorGUILayout.LabelField("Only works when this asset is imported with Legacy animation mode.");
        }
    }
}