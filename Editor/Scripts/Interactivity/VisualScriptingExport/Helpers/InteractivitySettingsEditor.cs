using UnityEditor;
using UnityEngine;

namespace UnityGLTF.Interactivity.VisualScripting
{
    [CustomEditor(typeof(VisualScriptingExportPlugin))]
    public class InteractivitySettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var rect = EditorGUILayout.GetControlRect(false);
            rect.xMin = rect.xMax -250;
            if (GUI.Button(rect, "Log supported VisualScripting Units"))
                SupportedUnitExports.LogSupportedUnits();
        }
    }
}