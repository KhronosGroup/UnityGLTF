using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityGLTF.Plugins;

namespace UnityGLTF
{
    [CustomEditor(typeof(GLTFPlugin), true)]
    public class GLTFPluginEditor: Editor
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

    internal abstract class PackageInstallEditor : GLTFPluginEditor
    {
        private bool isInstalling = false;
        protected abstract string PackageName { get; }
        private static GUIStyle IndentedButton;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;
            var t = target as GLTFPlugin;
            if (!t || !t.PackageMissing)
            {
                var rect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Uninstall Package"), false, () => {
                        Client.Remove(PackageName);
                    });
                    menu.ShowAsContext();
                    Event.current.Use();
                }

                GUI.enabled = wasEnabled;
                return;
            }
            
            if (isInstalling)
            {
                GUI.enabled = wasEnabled;
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Installing...");
                EditorGUI.EndDisabledGroup();
                return;
            }

            EditorGUI.indentLevel++;
            if (IndentedButton == null)
            {
                IndentedButton = new GUIStyle(EditorStyles.miniButton);
                IndentedButton.margin = new RectOffset(EditorGUI.indentLevel * 16, 0, 0, 0);
            }
            
            if (GUILayout.Button("Install " + PackageName, IndentedButton))
            {
                isInstalling = true;
                var request = Client.Add(PackageName);
                
                void WatchInstall()
                {
                    if (!request.IsCompleted) return;
                    
                    isInstalling = false;
                    if (request.Status >= StatusCode.Failure)
                        Debug.LogError(request.Error.message);
                    EditorApplication.update -= WatchInstall;
                }
                
                EditorApplication.update += WatchInstall;
            }
            EditorGUI.indentLevel--;
            GUI.enabled = wasEnabled;
        }
    }
    
    [CustomEditor(typeof(DracoImport))]
    internal class DracoImportEditor : PackageInstallEditor
    {
        protected override string PackageName => "com.unity.cloud.draco";
    }
    
    [CustomEditor(typeof(MeshoptImport))]
    internal class MeshoptImportEditor : PackageInstallEditor
    {
        protected override string PackageName => "com.unity.meshopt.decompress";
    }
    
    [CustomEditor(typeof(Ktx2Import))]
    internal class Ktx2ImportEditor : PackageInstallEditor
    {
        protected override string PackageName => "com.unity.cloud.ktx";
    }
}