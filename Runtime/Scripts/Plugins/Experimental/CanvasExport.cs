using System.Collections.Generic;
using System.Reflection;
using GLTF.Schema;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UnityGLTF.Plugins
{
    public class CanvasExport : GLTFExportPlugin
    {
        public override string DisplayName => "Bake to Mesh: Canvas";
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
                shader = Shader.Find("Hidden/UnityGLTF/UnlitGraph-Transparent");
#if UNITY_EDITOR
                if (!shader) shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("83f2caca07949794fb997734c4b0520f"));
#endif
            }
            
            var g = transform;

            // force refresh
            var r = transform.GetComponent<CanvasRenderer>();
            if (r) r.GetType().GetMethod("RequestRefresh", (BindingFlags)(-1)).Invoke(r, null);
            
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