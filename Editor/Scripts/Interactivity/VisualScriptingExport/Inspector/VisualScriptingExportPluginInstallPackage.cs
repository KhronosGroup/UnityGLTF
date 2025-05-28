#if !HAVE_VISUAL_SCRIPTING
namespace UnityGLTF.Interactivity.VisualScripting
{
    using UnityGLTF;
    using UnityGLTF.Plugins;
    using UnityEditor;

    [CustomEditor(typeof(VisualScriptingExportPlugin))]
    internal class VisualScriptingExportEditor : PackageInstallEditor
    {
        protected override string PackageName => "com.unity.visualscripting";
    }
}
#endif
