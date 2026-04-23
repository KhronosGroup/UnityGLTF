using UnityEngine;
using UnityGLTF;

namespace Interactivity.Schema
{
    public static class RefResolver
    {
        public static bool TryRefToStaticJson(GLTFSceneExporter sceneExporter, object value, out string staticJson)
        {
            staticJson = null;
            if (value == null)
                return false;

            switch (value)
            {
                case GameObject go:
                    var goIndex = sceneExporter.GetTransformIndex(go.transform);
                    if (goIndex == -1)
                        return false;
                    staticJson = $"/nodes/{goIndex}/";
                    return true;
                case Transform tr:
                    var transformIndex = sceneExporter.GetTransformIndex(tr);
                    if (transformIndex == -1)
                        return false;
                    staticJson = $"/nodes/{transformIndex}/";
                    return true;
                case Material mat:
                    var materialIndex = sceneExporter.GetMaterialIndex(mat);
                    if (materialIndex == -1)
                        return false;
                    staticJson = $"/materials/{materialIndex}/";
                    return true;
                case Mesh mesh:
                    Debug.LogError("Resolving a Mesh reference to static json pointer is not supported!", mesh);
                    return false;
                case Component comp:
                    var goOfComp = comp.gameObject;
                    var goOfCompIndex = sceneExporter.GetTransformIndex(goOfComp.transform);
                    if (goOfCompIndex == -1)
                        return false;
                    staticJson = $"/nodes/{goOfCompIndex}/";
                    return true;
            }

            return false;

        }
        
    }
}