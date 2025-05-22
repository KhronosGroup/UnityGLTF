using UnityEngine;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityGLTF.Interactivity.Playback
{
    [CustomEditor(typeof(GLTFInteractivityPlayback))]
    public class GLTFInteractivityPlayback_Inspector : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Handles all Unity event hooks for interactive GLTF playback.");
            EditorGUILayout.LabelField("Make sure colliders are enabled in the importer for hover/selection events.");
        }
    }
}