using System.Collections.Generic;
using System.Linq;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    class SpriteRendererExport : GLTFExportPlugin
    {
        public override string DisplayName => "Bake to Mesh: Sprites";

        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return new Context();
        }

        class Context : GLTFExportPluginContext
        {
            readonly List<GameObject> _meshes = new List<GameObject>();
            public override void BeforeNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
            {
                var renderer = transform.GetComponent<SpriteRenderer>();
                if (!renderer) return;

                var sprite = renderer.sprite;
                if (!sprite) return;

                var texture = sprite.texture;
                var verts = sprite.vertices;
                var tris = sprite.triangles;
                var uvs = sprite.uv;

                var mesh = new Mesh();
                mesh.vertices = verts.Select(v => new Vector3(v.x, v.y, 0)).ToArray();
                mesh.triangles = tris.Select(t => (int) t).ToArray();
                mesh.uv = uvs;

                var unlitMat = new Material(Shader.Find("UnityGLTF/UnlitGraph"));
                unlitMat.hideFlags = HideFlags.DontSave;
                unlitMat.SetTexture("baseColorTexture", texture);
                unlitMat.SetColor("baseColorFactor", renderer.color.linear);
                UnlitGraphMap map = new UnlitGraphMap(unlitMat);
                map.AlphaMode = AlphaMode.BLEND;

                var go = new GameObject("SpriteMesh");
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = unlitMat;
                go.transform.SetParent(transform, false);
                go.hideFlags = HideFlags.DontSave;

                _meshes.Add(go);
            }

            public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
            {
                foreach (var mesh in _meshes)
                {
                    if (Application.isPlaying)
                        Destroy(mesh);
                    else
                        DestroyImmediate(mesh);
                }
                _meshes.Clear();
            }
        }
    }
}
