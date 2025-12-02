using UnityEngine;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityGLTF.Interactivity.Playback
{
    [CustomEditor(typeof(GLTFInteractivityData))]
    public class GLTFInteractivityData_Inspector : Editor
    { 
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Contains serialized data representing this GLTF's interactivity graph.");
            EditorGUILayout.Space();
            var data = (GLTFInteractivityData)target;
            data.showData = EditorGUILayout.Toggle("Show Data", data.showData);

            if (data.showData)
                base.OnInspectorGUI();
        }
    }
}