using UnityEditor;
using UnityEngine;

namespace UnityGLTF.Interactivity.VisualScripting
{
#if HAVE_VISUAL_SCRIPTING
    [CustomEditor(typeof(VisualScriptingExportPlugin))]
    public class InteractivitySettingsEditor : GLTFPluginEditor
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
#endif
}