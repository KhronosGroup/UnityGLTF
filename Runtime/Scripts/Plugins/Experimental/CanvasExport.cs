using System.Collections.Generic;
using System.Reflection;
using GLTF.Schema;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UnityGLTF.Plugins
{
    [ExperimentalPlugin]
    public class CanvasExport : GLTFExportPlugin
    {
        public override string DisplayName => "Bake to Mesh: Canvas UI";
        public override string Description => "Bakes UI Canvas components to meshes and materials. Render order is estimated by slight transform offsets; results might differ in viewers depending on how transparent objects are sorted.";
        public override bool EnabledByDefault => false;
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return new CanvasExportContext();
        }
    }
    
    public class CanvasExportContext: GLTFExportPluginContext
    {
        private static Shader shader;
        
        public override void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot root, Transform transform, Node node)
        {
            // emit mesh and material if this is a Graphic element in a Canvas that's not disabled
            if (!shader)
            {
                shader = Shader.Find("UnityGLTF/UnlitGraph");
#if UNITY_EDITOR
                if (!shader) shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("59541e6caf586ca4f96ccf48a4813a51"));
#endif
                if (!shader)
                {
                    Debug.LogError("CanvasExport: Shader not found. Please make sure the UnityGLTF/UnlitGraph shader is included in build or add the UnityGLTFShaderVariantCollection in Project Settings/Graphics.");
                    return;
                }
            }
            
            var g = transform;

            // force refresh
            var r = transform.GetComponent<CanvasRenderer>();
            if (r)
            {
                var rMethod = r.GetType().GetMethod("RequestRefresh", (BindingFlags)(-1));
                if (rMethod != null)
                    rMethod.Invoke(r, null);
            }
            
            var canvas = g.GetComponent<Graphic>() ? g.GetComponent<Graphic>().canvas : null;
            var canvasRect = canvas ? canvas.GetComponent<RectTransform>().rect : new Rect(0,0,1000,1000);
            var cap = default(CanvasExportCaptureMeshHelper);
            if (canvas && !g.gameObject.TryGetComponent<CanvasExportCaptureMeshHelper>(out cap))
                cap = g.gameObject.AddComponent<CanvasExportCaptureMeshHelper>();
            
            if (cap)
            {
                cap.hideFlags = HideFlags.DontSave;
                
                var gotMeshAndMaterial = cap.GetMeshAndMaterial(out var mesh, out var material, shader);
                
                if (gotMeshAndMaterial)
                {
                    var uniquePrimitives = new List<GLTFSceneExporter.UniquePrimitive>();
                    uniquePrimitives.Add(new GLTFSceneExporter.UniquePrimitive()
                    {
                        Mesh = mesh,
                        SkinnedMeshRenderer = null,
                        Materials = new [] { material },
                    });
                    node.Mesh = exporter.ExportMesh(transform.name, uniquePrimitives);
                    var t = node.Translation;
                    t.Z += -canvasRect.width * 0.005f; // heuristic for avoiding Z-fighting, might need to be exposed later
                    node.Translation = t;
                    // exporter.RegisterPrimitivesWithNode(node, uniquePrimitives);
                }

                if (Application.isPlaying) Object.Destroy(cap);
                else Object.DestroyImmediate(cap);
            }
        }
    }
}