#if HAVE_TMPRO
using TMPro;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace UnityGLTF.Plugins
{
    [AddComponentMenu("")]
    internal class CanvasExportCaptureMeshHelper : MonoBehaviour, IMeshModifier
    {
        private Mesh mesh;
        
        public void ModifyMesh(Mesh mesh)
        {
            // legacy, ignore
        }

        public void ModifyMesh(VertexHelper verts)
        {
            if (!mesh)
            {
                mesh = new Mesh();
                mesh.hideFlags = HideFlags.DontSave;
            }
            
            verts.FillMesh(mesh);
        }

        public bool GetMeshAndMaterial(out Mesh mesh, out Material material, Shader shader)
        {
            var g = GetComponent<Graphic>();
            if (!g)
            {
                mesh = null;
                material = null;
                return false;
            }
            
            g.SetVerticesDirty();
            g.Rebuild(CanvasUpdate.PreRender);

            mesh = this.mesh;

            material = default(Material);

            bool hasTMPro = false;
#if HAVE_TMPRO
            var tmPro = GetComponent<TextMeshProUGUI>();
            hasTMPro = tmPro != null;
            if (hasTMPro)
            {
                mesh = tmPro.mesh;
                material = tmPro.fontSharedMaterial;
            }
#endif
            if (!material)
            {
                material = new Material(shader);
                material.SetOverrideTag("RenderType", "Transparent");
            }
            
            if (!hasTMPro)
            {
                var mat = material;
                mat.hideFlags = HideFlags.DontSave;
                var tex = g.mainTexture;

                if (tex)
                {
                    if (mat.HasProperty("baseColorTexture"))
                        mat.SetTexture("baseColorTexture", tex);
                    else if (mat.HasProperty("_MainTex"))
                        mat.SetTexture("_MainTex", tex);
                }
                
                var col = g.color;
                if (mat.HasProperty("baseColorFactor"))
                    mat.SetColor("baseColorFactor", col);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", col);
                
                if (mat.HasProperty("alphaCutoff"))
                    mat.SetFloat("alphaCutoff", 0);
            }

            return mesh && material;
        }

        public void CaptureTo(Transform root, GameObject shadow, Shader shader)
        {
            var g = GetComponent<Graphic>();
            var cr = g.GetComponent<CanvasRenderer>();
            if (!g || !g.enabled || cr.cull)
            {
                mesh = null;
                return;
            }
            
            if (!shadow.TryGetComponent<MeshFilter>(out var mf))
                mf = shadow.AddComponent<MeshFilter>();
            
            if (!shadow.TryGetComponent<MeshRenderer>(out var mr))
                mr = shadow.AddComponent<MeshRenderer>();

            GetMeshAndMaterial(out mesh, out var material, shader);

            mf.sharedMesh = mesh;
            mr.sharedMaterial = material;
        }
    }
}