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
using Unity.Collections;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityGLTF
{
#if UNITY_2020_2_OR_NEWER
#if ENABLE_DEFAULT_GLB_IMPORTER
    [ScriptedImporter(ImporterVersion, new[] { "glb", "gltf" })]
#else
    [ScriptedImporter(ImporterVersion, null, overrideExts: new[] { "glb", "gltf" })]
#endif
#else
	[ScriptedImporter(ImporterVersion, new[] { "glb" })]
#endif
    public class GLTFImporter : ScriptedImporter
    {
	    private const int ImporterVersion = 7;

	    private static void EnsureShadersAreLoaded()
	    {
		    const string PackagePrefix = "Packages/org.khronos.unitygltf/";
		    var shaders = new string[] {
			    PackagePrefix + "Runtime/Shaders/ShaderGraph/PBRGraph.shadergraph",
			    PackagePrefix + "Runtime/Shaders/ShaderGraph/UnlitGraph.shadergraph",
			    PackagePrefix + "Runtime/Shaders/PbrMetallicRoughness.shader",
			    PackagePrefix + "Runtime/Shaders/PbrSpecularGlossiness.shader",
			    PackagePrefix + "Runtime/Shaders/Unlit.shader",
		    };

		    foreach (var shaderPath in shaders)
		    {
			    AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
		    }
	    }

	    [Tooltip("Turn this off to create an explicit GameObject for the glTF scene. A scene root will always be created if there's more than one root node.")]
        [SerializeField] internal bool _removeEmptyRootObjects = true;
        [SerializeField] internal float _scaleFactor = 1.0f;
        [SerializeField] internal int _maximumLod = 300;
        [SerializeField] internal bool _readWriteEnabled = true;
        [SerializeField] internal bool _generateColliders = false;
        [SerializeField] internal bool _swapUvs = false;
        [SerializeField] internal bool _generateLightmapUVs = false;
        [SerializeField] internal GLTFImporterNormals _importNormals = GLTFImporterNormals.Import;
        [SerializeField] internal GLTFImporterNormals _importTangents = GLTFImporterNormals.Import;
        [SerializeField] internal AnimationMethod _importAnimations = AnimationMethod.Mecanim;
        [SerializeField] internal bool _addAnimatorComponent = false;
        [SerializeField] internal bool _animationLoopTime = true;
        [SerializeField] internal bool _animationLoopPose = false;
        [SerializeField] internal bool _importMaterials = true;
        [Tooltip("Enable this to get the same main asset identifiers as glTFast uses. This is recommended for new asset imports. Note that changing this for already imported assets will break their scene references and require manually re-adding the affected assets.")]
        [SerializeField] internal bool _useSceneNameIdentifier = false;

        // for humanoid importer
        [SerializeField] internal bool m_OptimizeGameObjects = false;
        [SerializeField] internal HumanDescription m_HumanDescription = new HumanDescription();

        // material remapping
        [SerializeField] private Material[] m_Materials = new Material[0];

        [Serializable]
        internal class ExtensionInfo
        {
	        public string name;
	        public bool supported;
	        public bool used;
	        public bool required;
        }

        [Serializable]
        public class TextureInfo
        {
	        public Texture2D texture;
	        public bool shouldBeLinear;
        }

#if !UNIYT_2020_2_OR_NEWER
	    private class NonReorderableAttribute : Attribute {}
#endif

        // Import messages (extensions, warnings, errors, ...)
        [NonReorderable] [SerializeField] internal List<ExtensionInfo> _extensions;
        [NonReorderable] [SerializeField] private List<TextureInfo> _textures;
        [SerializeField] internal string _mainAssetIdentifier;

        internal List<TextureInfo> Textures => _textures;

        private static string[] GatherDependenciesFromSourceFile(string path)
        {
	        // only supported glTF for now - would be harder to check for external references in glb assets.
	        if (!path.ToLowerInvariant().EndsWith(".gltf")) return Array.Empty<string>();

	        var dependencies = new List<string>();

	        // read minimal JSON, check if there's a bin buffer, and load that.
	        // all other assets should be "proper assets" and be found by the asset database, but we're not importing .bin
	        // since it's too common as a file type.
	        var gltf = GLTFRoot.Deserialize(new StreamReader(path));
	        var externalBuffers = gltf?.Buffers?.Where(b => b?.Uri != null && b.Uri.ToLowerInvariant().EndsWith(".bin"));
	        if (externalBuffers != null)
	        {
		        var dir = Path.GetDirectoryName(path);
		        foreach (var buffer in externalBuffers)
		        {
			        var uri = buffer.Uri;
			        if (!File.Exists(Path.Combine(dir, uri)))
				        uri = Uri.UnescapeDataString(uri);
			        if (File.Exists(Path.Combine(dir, uri)))
						dependencies.Add(Path.Combine(dir, uri));
			        // TODO check if inside the project/any package, could be an absolute path
			        else if (File.Exists(uri))
				        dependencies.Add(uri);
		        }
	        }

	        return dependencies.ToArray();
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string sceneName = null;
            GameObject gltfScene = null;
            AnimationClip[] animations = null;
            Mesh[] meshes = null;

            var uniqueNames = new List<string>() { "main asset" };
            EnsureShadersAreLoaded();

            string GetUniqueName(string desiredName)
            {
	            var uniqueName = ObjectNames.GetUniqueName(uniqueNames.ToArray(), desiredName);
	            if (!uniqueNames.Contains(uniqueName)) uniqueNames.Add(uniqueName);
	            return uniqueName;
            }

#if UNITY_2017_3_OR_NEWER
            // We explicitly turn the new identifier on for new imports, that is, when no meta file existed before this import.
            // We do this early, so that when imports fail, we still get the new identifier. Unity already sets import settings on failed imports.
            if (!_useSceneNameIdentifier)
            {
	            var importer = GetAtPath(ctx.assetPath);
	            if (importer.importSettingsMissing)
	            {
		            _useSceneNameIdentifier = true;
	            }
            }
#endif

            try
            {
                sceneName = Path.GetFileNameWithoutExtension(ctx.assetPath);
                CreateGLTFScene(ctx.assetPath, out gltfScene, out animations);
                var rootGltfComponent = gltfScene.GetComponent<InstantiatedGLTFObject>();
                if (rootGltfComponent) DestroyImmediate(rootGltfComponent);

                // Remove empty roots
                if (_removeEmptyRootObjects)
                {
                    var t = gltfScene.transform;
                    var existingAnimator = t.GetComponent<Animator>();
                    var hadAnimator = (bool)existingAnimator;
                    var existingAvatar = existingAnimator ? existingAnimator.avatar : default;
                    if (existingAnimator)
	                    DestroyImmediate(existingAnimator);

                    while (
                        gltfScene.transform.childCount == 1 &&
                        gltfScene.GetComponents<Component>().Length == 1)
                    {
                        var parent = gltfScene;
                        var importName = parent.name;
                        gltfScene = gltfScene.transform.GetChild(0).gameObject;
                        gltfScene.name = importName; // root name is always name of the file anyways
                        t = gltfScene.transform;
                        t.parent = null; // To keep transform information in the new parent
                        DestroyImmediate(parent); // Get rid of the parent
                    }

                    // Re-add animator if it was removed
                    if (hadAnimator)
					{
	                    var newAnimator = gltfScene.AddComponent<Animator>();
	                    newAnimator.avatar = existingAvatar;
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

                    if (!Mathf.Approximately(_scaleFactor, 1.0f))
                    {
	                    vertexBuffer.Clear();
	                    mesh.GetVertices(vertexBuffer);
	                    for (var i = 0; i < vertexBuffer.Count; ++i)
	                    {
	                        vertexBuffer[i] *= _scaleFactor;
	                    }
	                    mesh.SetVertices(vertexBuffer);
                    }
                    if (_generateLightmapUVs)
                    {
	                    var uv2 = mesh.uv2;
	                    if (uv2 == null || uv2.Length < 1)
	                    {
		                    var hasTriangleTopology = true;
		                    for (var i = 0; i < mesh.subMeshCount; i++)
								hasTriangleTopology &= mesh.GetTopology(i) == MeshTopology.Triangles;

		                    // uv2 = Unwrapping.GeneratePerTriangleUV(mesh);
		                    // mesh.SetUVs(1, uv2);

		                    // There seems to be a bug in Unity's splitting code:
		                    // for some meshes, the result is broken after splitting.
		                    if (hasTriangleTopology)
								Unwrapping.GenerateSecondaryUVSet(mesh);
	                    }
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
                        mesh.normals = new Vector3[0];
                    else if (_importNormals == GLTFImporterNormals.Calculate && mesh.GetTopology(0) == MeshTopology.Triangles)
                        mesh.RecalculateNormals();
                    else if (_importNormals == GLTFImporterNormals.Import && mesh.normals.Length == 0 && mesh.GetTopology(0) == MeshTopology.Triangles)
	                    mesh.RecalculateNormals();

					if (_importTangents == GLTFImporterNormals.None)
						mesh.tangents = new Vector4[0];
					else if (_importTangents == GLTFImporterNormals.Calculate && mesh.GetTopology(0) == MeshTopology.Triangles)
						mesh.RecalculateTangents();
					else if (_importTangents == GLTFImporterNormals.Import && mesh.tangents.Length == 0 && mesh.GetTopology(0) == MeshTopology.Triangles)
						mesh.RecalculateTangents();

					mesh.UploadMeshData(!_readWriteEnabled);

                    if (_generateColliders)
                    {
                        var collider = mf.gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = mesh;
                    }

                    return mesh;
                }).Where(x => x).ToArray();

                if (animations != null)
                {
	                foreach (var clip in animations)
	                {
		                ctx.AddObjectToAsset(GetUniqueName(clip.name), clip);
	                }
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

                if (_importAnimations == AnimationMethod.MecanimHumanoid)
                {
	                var avatar = HumanoidSetup.AddAvatarToGameObject(gltfScene);
	                if (avatar && avatar.isValid)
						ctx.AddObjectToAsset("avatar", avatar);
                }

                var renderers = gltfScene.GetComponentsInChildren<Renderer>();

                if (_importMaterials)
                {
                    // Get materials
                    var map = GetExternalObjectMap();
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
                                mat.name = matName;
                            }

                            return mat;
                        });
                    }).Distinct().ToArray();

                    // apply material remap
                    foreach(var r in renderers)
                    {
	                    // remap materials to external objects
	                    var m = r.sharedMaterials;
	                    for (var i = 0; i < m.Length; i++)
	                    {
		                    var si = new SourceAssetIdentifier(m[i]);
		                    if (map.ContainsKey(si))
			                    m[i] = map[si] as Material;
	                    }
	                    r.sharedMaterials = m;
                    };

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
                                            tex.name = texName;
                                        }
                                        matTextures.Add(tex);
                                    }

                                    List<TexMaterialMap> materialMaps;
                                    if (!texMaterialMap.TryGetValue(tex, out materialMaps))
                                    {
                                        materialMaps = new List<TexMaterialMap>();
                                        texMaterialMap.Add(tex, materialMaps);
                                    }

                                    materialMaps.Add(new TexMaterialMap(mat, propertyName, propertyName == "_BumpMap" || propertyName == "normalTexture" || propertyName.EndsWith("NormalTexture")));
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
#if UNITY_2020_2_OR_NEWER
		                        ctx.DependsOnArtifact(
#else
		                        ctx.DependsOnSourceAsset(
#endif
			                        AssetDatabase.GetAssetPath(tex));
	                        }
	                        else
	                        {
		                        ctx.AddObjectToAsset(GetUniqueName(tex.name), tex);
	                        }
                        }
                    }

#if !UNITY_2022_1_OR_NEWER
					AssetDatabase.Refresh();
#endif

                    // Save materials as separate assets and rewrite refs
                    if (materials.Length > 0)
                    {
                        foreach (var mat in materials)
                        {
	                        // ensure materials that are overriden aren't shown in the hierarchy.
	                        var si = new SourceAssetIdentifier(mat);
	                        if (map.ContainsKey(si))
		                        mat.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

	                        ctx.AddObjectToAsset(GetUniqueName(mat.name), mat);
                        }
                    }

                    m_Materials = materials;

#if !UNITY_2022_1_OR_NEWER
			        AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
#endif
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
	        if (!_useSceneNameIdentifier)
	        {
		        // Set main asset
		        _mainAssetIdentifier = "main asset";
		        ctx.AddObjectToAsset(_mainAssetIdentifier, gltfScene);
	        }
	        else
	        {
		        // This will be a breaking change, but would be aligned to the import naming from glTFast - allows switching back and forth between importers.
		        _mainAssetIdentifier = $"scenes/{gltfScene.name}";
		        ctx.AddObjectToAsset(_mainAssetIdentifier, gltfScene);
	        }

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

#if UNITY_2020_2_OR_NEWER
			ctx.DependsOnCustomDependency(ColorSpaceDependency);
#endif
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

        private const string ColorSpaceDependency = nameof(GLTFImporter) + "_" + nameof(PlayerSettings.colorSpace);

#if UNITY_2020_2_OR_NEWER
        [InitializeOnLoadMethod]
        private static void UpdateColorSpace()
        {
	        AssetDatabase.RegisterCustomDependency(ColorSpaceDependency, Hash128.Compute((int) PlayerSettings.colorSpace));
        }
#endif

		private void CreateGLTFScene(string projectFilePath, out GameObject scene, out AnimationClip[] animationClips)
        {
			var importOptions = new ImportOptions
			{
				DataLoader = new FileLoader(Path.GetDirectoryName(projectFilePath)),
				AnimationMethod = _importAnimations,
				AnimationLoopTime = _animationLoopTime,
				AnimationLoopPose = _animationLoopPose,
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

				if (gLTFRoot.ExtensionsUsed != null)
				{
					_extensions = gLTFRoot.ExtensionsUsed
						.Select(x => new ExtensionInfo()
						{
							name = x,
							supported = true,
							used = true,
							required = gLTFRoot.ExtensionsRequired?.Contains(x) ?? false,
						})
						.ToList();
				}
				else
				{
					_extensions = new List<ExtensionInfo>();
				}

				_textures = loader.TextureCache
					.Where(x => x != null)
					.Select(x => new TextureInfo() { texture = x.Texture, shouldBeLinear = x.IsLinear })
					.ToList();

				scene = loader.LastLoadedScene;
				animationClips = loader.CreatedAnimationClips;
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

        public override bool SupportsRemappedAssetType(Type type)
        {
	        if (type == typeof(Material))
		        return true;
	        return base.SupportsRemappedAssetType(type);
        }
    }
}
#endif
