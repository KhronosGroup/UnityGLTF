#if UNITY_2017_1_OR_NEWER

// glTFast is on the path to being official, so it should have highest priority as importer by default
// This ifdef is included for completeness.
// Other glTF importers should specify this via AsmDef dependency, for example
// `com.atteneder.gltfast@3.0.0: HAVE_GLTFAST` and then checking here `#if HAVE_GLTFAST`
#if HAVE_GLTFAST
#define ANOTHER_IMPORTER_HAS_HIGHER_PRIORITY
#endif

#if !ANOTHER_IMPORTER_HAS_HIGHER_PRIORITY && !UNITYGLTF_FORCE_DEFAULT_IMPORTER_OFF
#define ENABLE_DEFAULT_GLB_IMPORTER
#endif
#if UNITYGLTF_FORCE_DEFAULT_IMPORTER_ON
#define ENABLE_DEFAULT_GLB_IMPORTER
#endif

using UnityEditor;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Object = UnityEngine.Object;
using UnityGLTF.Loader;
using GLTF.Schema;
using GLTF;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityGLTF
{
#if UNITY_2020_2_OR_NEWER
#if ENABLE_DEFAULT_GLB_IMPORTER
    [ScriptedImporter(2, new[] { "glb", "gltf" })]
#else
    [ScriptedImporter(2, null, overrideExts: new[] { "glb", "gltf" })]
#endif
#else
	[ScriptedImporter(2, new[] { "glb" })]
#endif
    public class GLTFImporter : ScriptedImporter
    {
	    [Tooltip("Turn this off to create an explicit GameObject for the glTF scene. A scene root will always be created if there's more than one root node.")]
        [SerializeField] private bool _removeEmptyRootObjects = true;
        [SerializeField] private float _scaleFactor = 1.0f;
        [SerializeField] private int _maximumLod = 300;
        [SerializeField] private bool _readWriteEnabled = true;
        [SerializeField] private bool _generateColliders = false;
        [SerializeField] private bool _swapUvs = false;
        [SerializeField] private bool _generateLightmapUVs = false;
        [SerializeField] private GLTFImporterNormals _importNormals = GLTFImporterNormals.Import;
        [SerializeField] private AnimationMethod _importAnimations = AnimationMethod.Mecanim;
        [SerializeField] private bool _importMaterials = true;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string sceneName = null;
            GameObject gltfScene = null;
            UnityEngine.Mesh[] meshes = null;

            var uniqueNames = new List<string>() { "main asset" };

            string GetUniqueName(string desiredName)
            {
	            var uniqueName = ObjectNames.GetUniqueName(uniqueNames.ToArray(), desiredName);
	            if (!uniqueNames.Contains(uniqueName)) uniqueNames.Add(uniqueName);
	            return uniqueName;
            }

            try
            {
                sceneName = Path.GetFileNameWithoutExtension(ctx.assetPath);
                gltfScene = CreateGLTFScene(ctx.assetPath);
                var rootGltfComponent = gltfScene.GetComponent<InstantiatedGLTFObject>();
                if (rootGltfComponent) DestroyImmediate(rootGltfComponent);

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
                        DestroyImmediate(parent); // Get rid of the parent
                    }
                }

                // Ensure there are no hide flags present (will cause problems when saving)
                gltfScene.hideFlags &= ~(HideFlags.HideAndDontSave);
                foreach (Transform child in gltfScene.transform)
                {
                    child.gameObject.hideFlags &= ~(HideFlags.HideAndDontSave);
                }

                // scale all localPosition values if necessary
                if (!Mathf.Approximately(_scaleFactor, 1))
                {
	                var transforms = gltfScene.GetComponentsInChildren<Transform>();
	                foreach (var tr in transforms)
	                {
		                tr.localPosition *= _scaleFactor;
	                }
                }

                // Get meshes
                var meshHash = new HashSet<UnityEngine.Mesh>();
                var meshFilters = gltfScene.GetComponentsInChildren<MeshFilter>().Select(x => (x.gameObject, x.sharedMesh)).ToList();
                meshFilters.AddRange(gltfScene.GetComponentsInChildren<SkinnedMeshRenderer>().Select(x => (x.gameObject, x.sharedMesh)));
                var vertexBuffer = new List<Vector3>();
                meshes = meshFilters.Select(mf =>
                {
                    var mesh = mf.sharedMesh;
                    if (meshHash.Contains(mesh))
	                    return null;
                    meshHash.Add(mesh);

                    vertexBuffer.Clear();
                    mesh.GetVertices(vertexBuffer);
                    for (var i = 0; i < vertexBuffer.Count; ++i)
                    {
                        vertexBuffer[i] *= _scaleFactor;
                    }
                    mesh.SetVertices(vertexBuffer);
                    if (_generateLightmapUVs)
                    {
	                    var uv2 = mesh.uv2;
	                    if(uv2 == null || uv2.Length < 1)
		                    Unwrapping.GenerateSecondaryUVSet(mesh);
                    }
                    if (_swapUvs)
                    {
                        var uv = mesh.uv;
                        var uv2 = mesh.uv2;
                        mesh.uv = uv2;
                        if(uv.Length > 0)
							mesh.uv2 = uv;
                    }
                    if (_importNormals == GLTFImporterNormals.None)
                    {
                        mesh.normals = new Vector3[0];
                    }
                    if (_importNormals == GLTFImporterNormals.Calculate && mesh.GetTopology(0) == MeshTopology.Triangles)
                    {
                        mesh.RecalculateNormals();
                    }
                    mesh.UploadMeshData(!_readWriteEnabled);

                    if (_generateColliders)
                    {
                        var collider = mf.gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = mesh;
                    }

	                var meshName = string.IsNullOrEmpty(mesh.name) ? mf.gameObject.name : mesh.name;
	                mesh.name = meshName;

                    return mesh;
                }).Where(x => x).ToArray();

                var animations = gltfScene.GetComponentsInChildren<Animation>();
                var clips = animations.SelectMany(x => AnimationUtility.GetAnimationClips(x.gameObject));
                foreach (var clip in clips)
                {
	                ctx.AddObjectToAsset(GetUniqueName(clip.name), clip);
                }

                var animators = gltfScene.GetComponentsInChildren<Animator>();
                var clips2 = animators.SelectMany(x => AnimationUtility.GetAnimationClips(x.gameObject));
                foreach (var clip in clips2)
                {
	                ctx.AddObjectToAsset(GetUniqueName(clip.name), clip);
                }
                // we can't add the Animators as subassets here, since they require their state machines to be direct subassets themselves.
                // foreach (var anim in animators)
                // {
	            //     ctx.AddObjectToAsset(GetUniqueName(anim.runtimeAnimatorController.name), anim.runtimeAnimatorController as AnimatorController);
	            //     foreach (var layer in (anim.runtimeAnimatorController as AnimatorController).layers)
	            //     {
		        //         ctx.AddObjectToAsset(GetUniqueName(layer.name + "-state"), layer.stateMachine);
	            //     }
                // }

                var renderers = gltfScene.GetComponentsInChildren<Renderer>();

                if (_importMaterials)
                {
                    // Get materials
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
                                    matName = matName.Substring(Mathf.Min(matName.LastIndexOf("/", StringComparison.Ordinal) + 1, matName.Length - 1));
                                }

                                // Ensure name is unique
                                matName = ObjectNames.NicifyVariableName(matName);
                                mat.name = matName;
                            }

                            return mat;
                        });
                    }).Distinct().ToArray();

                    // Get textures
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

                                        tex.name = texName;
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
                    }).Distinct().ToArray();

                    // Save textures as separate assets and rewrite refs
                    // TODO: Support for other texture types
                    if (textures.Length > 0)
                    {
                        foreach (var tex in textures)
                        {
	                        if (AssetDatabase.Contains(tex) && AssetDatabase.GetAssetPath(tex) != ctx.assetPath)
	                        {
		                        ctx.DependsOnArtifact(AssetDatabase.GetAssetPath(tex));
	                        }
	                        else
	                        {
		                        ctx.AddObjectToAsset(GetUniqueName(tex.name), tex);
	                        }
                        }
                    }

					AssetDatabase.Refresh();

                    // Save materials as separate assets and rewrite refs
                    if (materials.Length > 0)
                    {
                        foreach (var mat in materials)
                        {
                            ctx.AddObjectToAsset(GetUniqueName(mat.name), mat);
                        }
                    }

					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
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
#if !UNITYGLTF_IMPORT_IDENTIFIER_V2
			// Set main asset
			ctx.AddObjectToAsset("main asset", gltfScene);
#else
			// This will be a breaking change, but would be aligned to the import naming from glTFast - allows switching back and forth between importers.
			ctx.AddObjectToAsset($"scenes/{gltfScene.name}", gltfScene);
#endif

			// Add meshes
			foreach (var mesh in meshes)
			{
				try
				{
					ctx.AddObjectToAsset(GetUniqueName("mesh-" + mesh.name), mesh);
				} catch(System.InvalidOperationException e) {
					Debug.LogWarning(e.ToString(), mesh);
				}
			}

			ctx.SetMainObject(gltfScene);
#else
            // Set main asset
            ctx.SetMainAsset("main asset", gltfScene);

            // Add meshes
            foreach (var mesh in meshes)
            {
                try {
					ctx.AddSubAsset("mesh " + mesh.name, mesh);
				} catch (System.InvalidOperationException e) {
					Debug.LogWarning(e.ToString(), mesh);
				}
            }
#endif
		}

		private GameObject CreateGLTFScene(string projectFilePath)
        {
			var importOptions = new ImportOptions
			{
				DataLoader = new FileLoader(Path.GetDirectoryName(projectFilePath)),
				AnimationMethod = _importAnimations,
			};
			using (var stream = File.OpenRead(projectFilePath))
			{
				GLTFRoot gLTFRoot;
				GLTFParser.ParseJson(stream, out gLTFRoot);
				stream.Position = 0; // Make sure the read position is changed back to the beginning of the file
				var loader = new GLTFSceneImporter(gLTFRoot, stream, importOptions);
				loader.MaximumLod = _maximumLod;
				loader.IsMultithreaded = true;

				loader.LoadSceneAsync().Wait();
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
