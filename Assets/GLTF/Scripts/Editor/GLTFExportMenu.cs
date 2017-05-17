using UnityEngine;
using UnityEditor;
using GLTF;

public class GLTFExportMenu
{

    [MenuItem("GLTF/Export Selected")]
    static void ExportSelected()
    {
        var obj = Selection.activeGameObject;
            var exporter = new GLTFExporter(obj.transform);
            var path = EditorUtility.OpenFolderPanel("glTF Export Path", "", "");
		    exporter.SaveGLTFandBin(path, obj.name);
    }
}