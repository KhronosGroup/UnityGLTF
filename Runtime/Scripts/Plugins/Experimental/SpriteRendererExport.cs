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
                
                // TODO No support for SpriteAtlas at the moment. This would require
                // - understanding which sprite is from which atlas
                // - extracting the sprite from the atlas and processing it further here

                // TODO DrawMode = Sliced or Tiled is not supported right now
                
                var texture = sprite.texture;
                var verts = sprite.vertices;
                var tris = sprite.triangles;
                var uvs = sprite.uv;

                if (renderer.drawMode == SpriteDrawMode.Sliced)
                {
                    // TODO adjust verts, tris and UVs for 9-sliced mode
                }
                
                var mesh = new Mesh();
                mesh.vertices = verts.Select(v => new Vector3(v.x, v.y, 0)).ToArray();
                mesh.triangles = tris.Select(t => (int) t).ToArray();
                mesh.uv = uvs;

                var unlitMat = new Material(Shader.Find("UnityGLTF/UnlitGraph"));
                unlitMat.hideFlags = HideFlags.DontSave;
                unlitMat.SetTexture("baseColorTexture", texture);
                // TODO check linear conversion based on color space
                unlitMat.SetColor("baseColorFactor", renderer.color);
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
