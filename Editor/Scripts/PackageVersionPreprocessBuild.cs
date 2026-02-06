using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UnityGLTF
{
     public class PackageVersionPreprocessBuild : IPreprocessBuildWithReport
     {
         public int callbackOrder
         {
             get => 0;
         }
         
         private static void SetPackageVersion(bool resetValue = false)
         {
             var defaultSettings = GLTFSettings.GetOrCreateSettings();
             if (!defaultSettings)
               return;

             defaultSettings.packageVersion = resetValue ? null : defaultSettings.GetUnityGltfVersion();
             EditorUtility.SetDirty(defaultSettings);
             AssetDatabase.SaveAssets();
             AssetDatabase.Refresh();
         }
         
         public void OnPreprocessBuild(BuildReport report)
         {
             SetPackageVersion();
         }
         
         [PostProcessBuild(1)]
         public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
         {
             SetPackageVersion(true);
         }
    }
}