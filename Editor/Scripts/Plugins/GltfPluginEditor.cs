using UnityEditor;
using UnityGLTF.Plugins;

namespace UnityGLTF
{
    [CustomEditor(typeof(GLTFPlugin), true)]
    public class GltfPluginEditor: Editor
    {
        // Follows the default implementation of OnInspectorGUI, but skips the script field
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var iterator = serializedObject.GetIterator();
            // skip script field
            iterator.NextVisible(true);
            while (iterator.NextVisible(false))
                EditorGUILayout.PropertyField(iterator, true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}