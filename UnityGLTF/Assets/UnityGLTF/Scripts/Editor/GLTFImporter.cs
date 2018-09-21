#if UNITY_2017_1_OR_NEWER
using UnityEditor.Experimental.AssetImporters;
using UnityEditor;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Object = UnityEngine.Object;
using System.Collections;
using UnityGLTF.Loader;
using GLTF.Schema;
using GLTF;

namespace UnityGLTF
{
    [ScriptedImporter(1, new[] { "glb" })]
    public class GLTFImporter : ScriptedImporter
    {
        [SerializeField] private bool _removeEmptyRootObjects = true;
        [SerializeField] private float _scaleFactor = 1.0f;
        [SerializeField] private int _maximumLod = 300;
        [SerializeField] private bool _readWriteEnabled = true;
        [SerializeField] private bool _generateColliders = false;
        [SerializeField] private bool _swapUvs = false;
        [SerializeField] private GLTFImporterNormals _importNormals = GLTFImporterNormals.Import;
        [SerializeField] private bool _importMaterials = true;
        [SerializeField] private bool _useJpgTextures = false;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string sceneName = null;
            GameObject gltfScene = null;
            UnityEngine.Mesh[] meshes = null;
            try
            {
                sceneName = Path.GetFileNameWithoutExtension(ctx.assetPath);
                gltfScene = CreateGLTFScene(ctx.assetPath);

                // Remove empty roots
                if (_removeEmptyRootObjects)
                {
                    var t = gltfScene.transform;
                    while (
                        gltfScene.transform.childCount == 1 &&
                        gltfScene.GetComponents<Component>().Length == 1)
                    {
                        var parent = gltfScene;
                        gltfScene = gltfScene.transform.GetChild(0).gameObject;
                        t = gltfScene.transform;
                        t.parent = null; // To keep transform information in the new parent
                        Object.DestroyImmediate(parent); // Get rid of the parent
                    }
                }

                // Ensure there are no hide flags present (will cause problems when saving)
                gltfScene.hideFlags &= ~(HideFlags.HideAndDontSave);
                foreach (Transform child in gltfScene.transform)
                {
                    child.gameObject.hideFlags &= ~(HideFlags.HideAndDontSave);
                }

                // Zero position
                gltfScene.transform.position = Vector3.zero;

                // Get meshes
                var meshNames = new List<string>();
                var meshHash = new HashSet<UnityEngine.Mesh>();
                var meshFilters = gltfScene.GetComponentsInChildren<MeshFilter>();
                var vertexBuffer = new List<Vector3>();
                meshes = meshFilters.Select(mf =>
                {
                    var mesh = mf.sharedMesh;
                    vertexBuffer.Clear();
                    mesh.GetVertices(vertexBuffer);
                    for (var i = 0; i < vertexBuffer.Count; ++i)
                    {
                        vertexBuffer[i] *= _scaleFactor;
                    }
                    mesh.SetVertices(vertexBuffer);
                    if (_swapUvs)
                    {
                        var uv = mesh.uv;
                        var uv2 = mesh.uv2;
                        mesh.uv = uv2;
                        mesh.uv2 = uv2;
                    }
                    if (_importNormals == GLTFImporterNormals.None)
                    {
                        mesh.normals = new Vector3[0];
                    }
                    if (_importNormals == GLTFImporterNormals.Calculate)
                    {
                        mesh.RecalculateNormals();
                    }
                    mesh.UploadMeshData(!_readWriteEnabled);

                    if (_generateColliders)
                    {
                        var collider = mf.gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = mesh;
                    }

                    if (meshHash.Add(mesh))
                    {
                        var meshName = string.IsNullOrEmpty(mesh.name) ? mf.gameObject.name : mesh.name;
                        mesh.name = ObjectNames.GetUniqueName(meshNames.ToArray(), meshName);
                        meshNames.Add(mesh.name);
                    }

                    return mesh;
                }).ToArray();

                var renderers = gltfScene.GetComponentsInChildren<Renderer>();

                if (_importMaterials)
                {
                    // Get materials
                    var materialNames = new List<string>();
                    var materialHash = new HashSet<UnityEngine.Material>();
                    var materials = renderers.SelectMany(r =>
                    {
                        return r.sharedMaterials.Select(mat =>
                        {
                            if (materialHash.Add(mat))
                            {
                                var matName = string.IsNullOrEmpty(mat.name) ? mat.shader.name : mat.name;
                                if (matName == mat.shader.name)
                                {
                                    matName = matName.Substring(Mathf.Min(matName.LastIndexOf("/") + 1, matName.Length - 1));
                                }

                                // Ensure name is unique
                                matName = string.Format("{0} {1}", sceneName, ObjectNames.NicifyVariableName(matName));
                                matName = ObjectNames.GetUniqueName(materialNames.ToArray(), matName);

                                mat.name = matName;
                                materialNames.Add(matName);
                            }

                            return mat;
                        });
                    }).ToArray();

                    // Get textures
                    var textureNames = new List<string>();
                    var textureHash = new HashSet<Texture2D>();
                    var texMaterialMap = new Dictionary<Texture2D, List<TexMaterialMap>>();
                    var textures = materials.SelectMany(mat =>
                    {
                        var shader = mat.shader;
                        if (!shader) return Enumerable.Empty<Texture2D>();

                        var matTextures = new List<Texture2D>();
                        for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); ++i)
                        {
                            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                            {
                                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                                var tex = mat.GetTexture(propertyName) as Texture2D;
                                if (tex)
                                {
                                    if (textureHash.Add(tex))
                                    {
                                        var texName = tex.name;
                                        if (string.IsNullOrEmpty(texName))
                                        {
                                            if (propertyName.StartsWith("_")) texName = propertyName.Substring(Mathf.Min(1, propertyName.Length - 1));
                                        }

                                        // Ensure name is unique
                                        texName = string.Format("{0} {1}", sceneName, ObjectNames.NicifyVariableName(texName));
                                        texName = ObjectNames.GetUniqueName(textureNames.ToArray(), texName);

                                        tex.name = texName;
                                        textureNames.Add(texName);
                                        matTextures.Add(tex);
                                    }

                                    List<TexMaterialMap> materialMaps;
                                    if (!texMaterialMap.TryGetValue(tex, out materialMaps))
                                    {
                                        materialMaps = new List<TexMaterialMap>();
                                        texMaterialMap.Add(tex, materialMaps);
                                    }

                                    materialMaps.Add(new TexMaterialMap(mat, propertyName, propertyName == "_BumpMap"));
                                }
                            }
                        }
                        return matTextures;
                    }).ToArray();

                    var folderName = Path.GetDirectoryName(ctx.assetPath);

                    // Save textures as separate assets and rewrite refs
                    // TODO: Support for other texture types
                    if (textures.Length > 0)
                    {
                        var texturesRoot = string.Concat(folderName, "/", "Textures/");
                        Directory.CreateDirectory(texturesRoot);

                        foreach (var tex in textures)
                        {
                            var ext = _useJpgTextures ? ".jpg" : ".png";
                            var texPath = string.Concat(texturesRoot, tex.name, ext);
                            File.WriteAllBytes(texPath, _useJpgTextures ? tex.EncodeToJPG() : tex.EncodeToPNG());

                            AssetDatabase.ImportAsset(texPath);
                        }
                    }

                    // Save materials as separate assets and rewrite refs
                    if (materials.Length > 0)
                    {
                        var materialRoot = string.Concat(folderName, "/", "Materials/");
                        Directory.CreateDirectory(materialRoot);

                        foreach (var mat in materials)
                        {
                            var materialPath = string.Concat(materialRoot, mat.name, ".mat");
                            var newMat = mat;
                            CopyOrNew(mat, materialPath, m =>
                            {
                                // Fix references
                                newMat = m;
                                foreach (var r in renderers)
                                {
                                    var sharedMaterials = r.sharedMaterials;
                                    for (var i = 0; i < sharedMaterials.Length; ++i)
                                    {
                                        var sharedMaterial = sharedMaterials[i];
                                        if (sharedMaterial.name == mat.name) sharedMaterials[i] = m;
                                    }
                                    sharedMaterials = sharedMaterials.Where(sm => sm).ToArray();
                                    r.sharedMaterials = sharedMaterials;
                                }
                            });
                            // Fix textures
                            // HACK: This needs to be a delayed call.
                            // Unity needs a frame to kick off the texture import so we can rewrite the ref
                            if (textures.Length > 0)
                            {
                                EditorApplication.delayCall += () =>
                                {
                                    for (var i = 0; i < textures.Length; ++i)
                                    {
                                        var tex = textures[i];
                                        var texturesRoot = string.Concat(folderName, "/", "Textures/");
                                        var ext = _useJpgTextures ? ".jpg" : ".png";
                                        var texPath = string.Concat(texturesRoot, tex.name, ext);

                                        // Grab new imported texture
                                        var materialMaps = texMaterialMap[tex];
                                        var importer = (TextureImporter)TextureImporter.GetAtPath(texPath);
                                        var importedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                                        if (importer != null)
                                        {
                                            var isNormalMap = false;
                                            foreach (var materialMap in materialMaps)
                                            {
                                                if (materialMap.Material == mat)
                                                {
                                                    isNormalMap |= materialMap.IsNormalMap;
                                                    newMat.SetTexture(materialMap.Property, importedTex);
                                                }
                                            };

                                            if (isNormalMap)
                                            {
                                                // Try to auto-detect normal maps
                                                importer.textureType = TextureImporterType.NormalMap;
                                            }
                                            else if (importer.textureType == TextureImporterType.Sprite)
                                            {
                                                // Force disable sprite mode, even for 2D projects
                                                importer.textureType = TextureImporterType.Default;
                                            }

                                            importer.SaveAndReimport();
                                        }
                                        else
                                        {
                                            Debug.LogWarning(string.Format("GLTFImporter: Unable to import texture at path: {0}", texPath));
                                        }
                                    }
                                };
                            }
                        }
                    }
                }
                else
                {
                    var temp = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    temp.SetActive(false);
                    var defaultMat = new[] { temp.GetComponent<Renderer>().sharedMaterial };
                    DestroyImmediate(temp);

                    foreach (var rend in renderers)
                    {
                        rend.sharedMaterials = defaultMat;
                    }
                }
            }
            catch
            {
                if (gltfScene) DestroyImmediate(gltfScene);
                throw;
            }

#if UNITY_2017_3_OR_NEWER
			// Set main asset
			ctx.AddObjectToAsset("main asset", gltfScene);

			// Add meshes
			foreach (var mesh in meshes)
			{
				ctx.AddObjectToAsset("mesh " + mesh.name, mesh);
			}

			ctx.SetMainObject(gltfScene);
#else
            // Set main asset
            ctx.SetMainAsset("main asset", gltfScene);

            // Add meshes
            foreach (var mesh in meshes)
            {
                ctx.AddSubAsset("mesh " + mesh.name, mesh);
            }
#endif
        }

        private GameObject CreateGLTFScene(string projectFilePath)
        {
            ILoader fileLoader = new FileLoader(Path.GetDirectoryName(projectFilePath));
            using (var stream = File.OpenRead(projectFilePath))
            {
                GLTFRoot gLTFRoot;
                GLTFParser.ParseJson(stream, out gLTFRoot);
                var loader = new GLTFSceneImporter(gLTFRoot, fileLoader, stream);

                loader.MaximumLod = _maximumLod;
                loader.isMultithreaded = true;

                // HACK: Force the coroutine to run synchronously in the editor
                var stack = new Stack<IEnumerator>();
                stack.Push(loader.LoadScene());

                while (stack.Count > 0)
                {
                    var enumerator = stack.Pop();
                    if (enumerator.MoveNext())
                    {
                        stack.Push(enumerator);
                        var subEnumerator = enumerator.Current as IEnumerator;
                        if (subEnumerator != null)
                        {
                            stack.Push(subEnumerator);
                        }
                    }
                }
                return loader.LastLoadedScene;
            }
        }

        private void CopyOrNew<T>(T asset, string assetPath, Action<T> replaceReferences) where T : Object
        {
            var existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existingAsset)
            {
                EditorUtility.CopySerialized(asset, existingAsset);
                replaceReferences(existingAsset);
                return;
            }

            AssetDatabase.CreateAsset(asset, assetPath);
        }

        private class TexMaterialMap
        {
            public UnityEngine.Material Material { get; set; }
            public string Property { get; set; }
            public bool IsNormalMap { get; set; }

            public TexMaterialMap(UnityEngine.Material material, string property, bool isNormalMap)
            {
                Material = material;
                Property = property;
                IsNormalMap = isNormalMap;
            }
        }
    }
}
#endif
