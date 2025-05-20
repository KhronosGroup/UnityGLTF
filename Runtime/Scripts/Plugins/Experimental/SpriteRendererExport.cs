using System.Collections.Generic;
using System.Linq;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Rendering;

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
                
                var texture = sprite.texture;
                
                var mesh = new Mesh();

        

                if (renderer.drawMode != SpriteDrawMode.Simple)
                {
                 // access internal method "GetCurrentMeshData" from SpriteRenderer by reflection
                    var meshDataMethod = typeof(SpriteRenderer).GetMethod("GetCurrentMeshData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (meshDataMethod == null)
                    {
                        Debug.LogError("Exporting non-Simple SpriteDrawMode is not supported in this Unity version. Please use Unity 2023.2 or later.");
                        return;
                    }
                    var meshDataArray = (Mesh.MeshDataArray) meshDataMethod.Invoke(renderer, null);
                    Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, MeshUpdateFlags.Default);
                }
                else
                {
                    var verts = sprite.vertices;
                    var tris = sprite.triangles;
                    var uvs = sprite.uv;
                    mesh.vertices = verts.Select(v => new Vector3(v.x, v.y, 0)).ToArray();
                    mesh.triangles = tris.Select(t => (int) t).ToArray();
                    mesh.uv = uvs;
                }
                
                var unlitMat = new Material(Shader.Find("UnityGLTF/UnlitGraph"));
                unlitMat.hideFlags = HideFlags.DontSave;
                unlitMat.SetTexture("baseColorTexture", texture);
                // TODO check linear conversion based on color space
                unlitMat.SetColor("baseColorFactor", renderer.color);
                UnlitGraphMap map = new UnlitGraphMap(unlitMat);
                map.AlphaMode = AlphaMode.BLEND;
                map.DoubleSided = true;

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

        private static (Vector2[] verts, ushort[] tris, Vector2[] uvs) Generate9Slice(SpriteRenderer renderer, Sprite sprite)
        {
            // Generate verts, tris and UVs for 9-sliced mode
            var border = sprite.border; // x=left, y=bottom, z=right, w=top
            var size = renderer.size;
            var rect = sprite.rect;
            var pivotX = sprite.pivot.x / rect.width;
            var pivotY = sprite.pivot.y / rect.height;
            var uvs = sprite.uv;

            // Calculate outer bounds based on sprite's size and pivot
            float leftX = -pivotX * size.x;
            float rightX = (1 - pivotX) * size.x;
            float bottomY = -pivotY * size.y;
            float topY = (1 - pivotY) * size.y;

            // Calculate the actual pixel size of the border regions
            float borderLeftPixels = border.x;
            float borderRightPixels = border.z;
            float borderBottomPixels = border.y;
            float borderTopPixels = border.w;

            // Calculate the size of the stretchable regions in the original sprite
            float stretchableWidthPixels = rect.width - borderLeftPixels - borderRightPixels;
            float stretchableHeightPixels = rect.height - borderBottomPixels - borderTopPixels;

            // Calculate the scale factor between the original sprite and the resized sprite
            float widthScale = 2 / rect.width * rect.width / rect.height;
            float heightScale = 2 / rect.height;

            // Calculate the size of the corner regions in the final mesh (maintain original pixel size)
            float leftBorderWidth = borderLeftPixels * widthScale;
            float rightBorderWidth = borderRightPixels * widthScale;
            float bottomBorderHeight = borderBottomPixels * heightScale;
            float topBorderHeight = borderTopPixels * heightScale;

            // Calculate the actual positions of the border lines in the mesh
            float leftBorderX = leftX + leftBorderWidth;
            float rightBorderX = rightX - rightBorderWidth;
            float bottomBorderY = bottomY + bottomBorderHeight;
            float topBorderY = topY - topBorderHeight;

            // We need to get the exact UV coordinates directly from the sprite's texture
            Rect texRect = sprite.textureRect;
            
            // Calculate texture coordinates for the sprite borders in texture space
            float leftBorderTexU = texRect.x + border.x;
            float rightBorderTexU = texRect.x + texRect.width - border.z;
            float bottomBorderTexV = texRect.y + border.y;
            float topBorderTexV = texRect.y + texRect.height - border.w;
            
            // Get atlas coordinates - handle sprites that are part of a texture atlas
            // by getting the min/max UV bounds from the original sprite
            Vector2 uvMin = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 uvMax = new Vector2(float.MinValue, float.MinValue);
            foreach (var uv in uvs)
            {
                uvMin.x = Mathf.Min(uvMin.x, uv.x);
                uvMin.y = Mathf.Min(uvMin.y, uv.y);
                uvMax.x = Mathf.Max(uvMax.x, uv.x);
                uvMax.y = Mathf.Max(uvMax.y, uv.y);
            }
            
            // Map directly from texture coordinates to UV space
            // This ensures the exact texture pixels are used, avoiding stretching issues
            Vector2 TexCoordToUV(float texU, float texV)
            {
                // Calculate the normalized position within the sprite's rect
                float normalizedU = (texU - texRect.x) / texRect.width;
                float normalizedV = (texV - texRect.y) / texRect.height;
                
                // Map to the sprite's actual UV space in the texture
                float u = Mathf.Lerp(uvMin.x, uvMax.x, normalizedU);
                float v = Mathf.Lerp(uvMin.y, uvMax.y, normalizedV);
                
                return new Vector2(u, v);
            }
            
            // Calculate exact UV coordinates for the 9-slice borders
            Vector2 uvBottomLeft = TexCoordToUV(texRect.x, texRect.y);
            Vector2 uvBottomLeftBorder = TexCoordToUV(leftBorderTexU, texRect.y);
            Vector2 uvBottomRightBorder = TexCoordToUV(rightBorderTexU, texRect.y);
            Vector2 uvBottomRight = TexCoordToUV(texRect.x + texRect.width, texRect.y);
            
            Vector2 uvLeftBottom = TexCoordToUV(texRect.x, bottomBorderTexV);
            Vector2 uvLeftBottomBorder = TexCoordToUV(leftBorderTexU, bottomBorderTexV);
            Vector2 uvRightBottomBorder = TexCoordToUV(rightBorderTexU, bottomBorderTexV);
            Vector2 uvRightBottom = TexCoordToUV(texRect.x + texRect.width, bottomBorderTexV);
            
            Vector2 uvLeftTop = TexCoordToUV(texRect.x, topBorderTexV);
            Vector2 uvLeftTopBorder = TexCoordToUV(leftBorderTexU, topBorderTexV);
            Vector2 uvRightTopBorder = TexCoordToUV(rightBorderTexU, topBorderTexV);
            Vector2 uvRightTop = TexCoordToUV(texRect.x + texRect.width, topBorderTexV);
            
            Vector2 uvTopLeft = TexCoordToUV(texRect.x, texRect.y + texRect.height);
            Vector2 uvTopLeftBorder = TexCoordToUV(leftBorderTexU, texRect.y + texRect.height);
            Vector2 uvTopRightBorder = TexCoordToUV(rightBorderTexU, texRect.y + texRect.height);
            Vector2 uvTopRight = TexCoordToUV(texRect.x + texRect.width, texRect.y + texRect.height);

            // Define vertices (16 vertices for 9-slice, 4 corners + 8 edges + 4 center vertices)
            Vector2[] newVerts = new Vector2[16]
            {
                // Bottom-left section (0-3)
                new Vector2(leftX, bottomY),          // Bottom-left corner
                new Vector2(leftBorderX, bottomY),    // Bottom-left border
                new Vector2(leftX, bottomBorderY),    // Left-bottom border
                new Vector2(leftBorderX, bottomBorderY), // Inner bottom-left

                // Bottom-center section (4-7)
                new Vector2(rightBorderX, bottomY),   // Bottom-right border
                new Vector2(rightBorderX, bottomBorderY), // Inner bottom-right
                
                // Bottom-right section (6-9)
                new Vector2(rightX, bottomY),         // Bottom-right corner 
                new Vector2(rightX, bottomBorderY),   // Right-bottom border

                // Middle-left section (8-11)
                new Vector2(leftX, topBorderY),       // Left-top border
                new Vector2(leftBorderX, topBorderY), // Inner top-left
                
                // Middle-center section (10-13)
                new Vector2(rightBorderX, topBorderY), // Inner top-right
                
                // Middle-right section (12-15)
                new Vector2(rightX, topBorderY),      // Right-top border
                
                // Top-left section (14-17)
                new Vector2(leftX, topY),             // Top-left corner
                new Vector2(leftBorderX, topY),       // Top-left border

                // Top-center section (18-21)
                new Vector2(rightBorderX, topY),      // Top-right border
                
                // Top-right section (20-23)
                new Vector2(rightX, topY)             // Top-right corner
            };

            // Define UVs using our precise coordinates calculated from the texture
            Vector2[] newUVs = new Vector2[16]
            {
                // Bottom-left section
                uvBottomLeft,          // Bottom-left corner (0)
                uvBottomLeftBorder,    // Bottom-left border (1)
                uvLeftBottom,          // Left-bottom border (2)
                uvLeftBottomBorder,    // Inner bottom-left (3)

                // Bottom-center section
                uvBottomRightBorder,   // Bottom-right border (4)
                uvRightBottomBorder,   // Inner bottom-right (5)
                
                // Bottom-right section
                uvBottomRight,         // Bottom-right corner (6)
                uvRightBottom,         // Right-bottom border (7)

                // Middle-left section
                uvLeftTop,             // Left-top border (8)
                uvLeftTopBorder,       // Inner top-left (9)
                
                // Middle-center section
                uvRightTopBorder,      // Inner top-right (10)
                
                // Middle-right section
                uvRightTop,            // Right-top border (11)
                
                // Top-left section
                uvTopLeft,             // Top-left corner (12)
                uvTopLeftBorder,       // Top-left border (13)
                
                // Top-center section
                uvTopRightBorder,      // Top-right border (14)
                
                // Top-right section
                uvTopRight             // Top-right corner (15)
            };

            // Define triangles (9 quads = 18 triangles = 54 indices)
            ushort[] newTris = new ushort[54];
            int index = 0;

            // Helper function to add a quad (2 triangles) to the triangles array
            // Using correct winding order for triangles: a-b-c and a-c-d
            void AddQuad(ushort a, ushort b, ushort c, ushort d)
            {
                // First triangle (counter-clockwise winding: bottom-left to top-right)
                newTris[index++] = a;
                newTris[index++] = c;
                newTris[index++] = b;
                
                // Second triangle (counter-clockwise winding: bottom-left to bottom-right to top-right)
                newTris[index++] = a;
                newTris[index++] = d;
                newTris[index++] = c;
            }

            // Bottom-left quad
            AddQuad(0, 1, 3, 2);
            
            // Bottom-center quad
            AddQuad(1, 4, 5, 3);
            
            // Bottom-right quad
            AddQuad(4, 6, 7, 5);
            
            // Middle-left quad
            AddQuad(2, 3, 9, 8);
            
            // Middle-center quad
            AddQuad(3, 5, 10, 9);
            
            // Middle-right quad
            AddQuad(5, 7, 11, 10);
            
            // Top-left quad
            AddQuad(8, 9, 13, 12);
            
            // Top-center quad
            AddQuad(9, 10, 14, 13);
            
            // Top-right quad
            AddQuad(10, 11, 15, 14);
            
            return (newVerts, newTris, newUVs);
        }
    }
}
