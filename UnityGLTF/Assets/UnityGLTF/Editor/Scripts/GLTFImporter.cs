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
using UnityGLTF.Plugins;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
using UnityEngine.Rendering;
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

        // asset remapping
        [SerializeField] private Material[] m_Materials = new Material[0];
        [SerializeField] private Texture[] m_Textures = new Texture[0];


        // TODO make internal and allow access for relevant assemblies
        public Material[] ImportedMaterials => m_Materials;
        public Texture[] ImportedTextures => m_Textures;

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
	        using (var reader = new StreamReader(path))
	        {
		        var gltf = GLTFRoot.Deserialize(reader);
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
	        }
	        return dependencies.ToArray();
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
	        var plugins = new List<GltfImportPluginContext>();
	        var context = new GLTFImportContext(ctx, plugins);
	        var settings = GLTFSettings.GetOrCreateSettings();
	        foreach (var plugin in settings.ImportPlugins)
	        {
		        if (plugin != null && plugin.Enabled)
		        {
			        var instance = plugin.CreateInstance(context);
			        if(instance != null) plugins.Add(instance);
		        }
	        }

	        foreach (var plugin in plugins)
	        {
		        plugin.OnBeforeImport();
	        }

            GameObject gltfScene = null;
            AnimationClip[] animations = null;
            Mesh[] meshes = null;

            var uniqueNames = new List<string>() { "main asset" };
            EnsureShadersAreLoaded();

#if UNITY_2020_2_OR_NEWER && !UNITY_2021_3_OR_NEWER
	        // We don't need this custom dependency in 2021.3 and later, because there we always use ShaderGraph.
            ctx.DependsOnCustomDependency($"{nameof(GraphicsSettings)}.{nameof(GraphicsSettings.currentRenderPipeline)}");
#endif

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
                CreateGLTFScene(context, out gltfScene, out animations, out var importer);

                if (gltfScene)
                {
	                var rootGltfComponent = gltfScene.GetComponent<InstantiatedGLTFObject>();
	                if (rootGltfComponent) DestroyImmediate(rootGltfComponent);
                }

                // Remove empty roots
                if (gltfScene && _removeEmptyRootObjects)
                {
                    var t = gltfScene.transform;
                    var existingAnimator = t.GetComponent<Animator>();
                    var hadAnimator = (bool)existingAnimator;
                    var existingAvatar = existingAnimator ? existingAnimator.avatar : default;
                    if (existingAnimator)
	                    DestroyImmediate(existingAnimator);

                    var animationPathPrefix = "";
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
                        animationPathPrefix += "/" + importName;
                    }

                    // Re-add animator if it was removed
                    if (hadAnimator)
					{
	                    var newAnimator = gltfScene.AddComponent<Animator>();
	                    newAnimator.avatar = existingAvatar;
					}

                    // Re-target animation clips - when we strip the root, all animations also change and have a different path now.
                    if (animations != null)
					{
						foreach (var clip in animations)
						{
							if (clip == null) continue;

							// change all animation clip targets
							var bindings = AnimationUtility.GetCurveBindings(clip);
							var curves = new AnimationCurve[bindings.Length];
							var newBindings = new EditorCurveBinding[bindings.Length];
							for (var index = 0; index < bindings.Length; index++)
							{
								var binding = bindings[index];
								curves[index] = AnimationUtility.GetEditorCurve(clip, binding);

								var newBinding = bindings[index];
								if (binding.path.StartsWith(animationPathPrefix, StringComparison.Ordinal))
									newBinding.path = binding.path.Substring(animationPathPrefix.Length);
								newBindings[index] = newBinding;
							}

							var emptyCurves = new AnimationCurve[curves.Length];
							AnimationUtility.SetEditorCurves(clip, bindings, emptyCurves);
							AnimationUtility.SetEditorCurves(clip, newBindings, curves);
						}
					}
                }

                if (gltfScene)
                {
	                // Ensure there are no hide flags present (will cause problems when saving)
	                gltfScene.hideFlags &= ~(HideFlags.HideAndDontSave);
	                foreach (Transform child in gltfScene.transform)
	                {
	                    child.gameObject.hideFlags &= ~(HideFlags.HideAndDontSave);
	                }
                }

                // scale all localPosition values if necessary
                if (gltfScene && !Mathf.Approximately(_scaleFactor, 1))
                {
	                var transforms = gltfScene.GetComponentsInChildren<Transform>();
	                foreach (var tr in transforms)
	                {
		                tr.localPosition *= _scaleFactor;
	                }
                }

                // Get meshes
                var meshHash = new HashSet<Mesh>();
                var meshFilters = new List<(GameObject gameObject, Mesh sharedMesh)>();
                if (gltfScene)
                {
		            meshFilters = gltfScene.GetComponentsInChildren<MeshFilter>().Select(x => (x.gameObject, x.sharedMesh)).ToList();
	                meshFilters.AddRange(gltfScene.GetComponentsInChildren<SkinnedMeshRenderer>().Select(x => (x.gameObject, x.sharedMesh)));
                }

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

                if (gltfScene && _importAnimations == AnimationMethod.MecanimHumanoid)
                {
	                var avatar = HumanoidSetup.AddAvatarToGameObject(gltfScene);
	                if (avatar && avatar.isValid)
						ctx.AddObjectToAsset("avatar", avatar);
                }

                var renderers = gltfScene ? gltfScene.GetComponentsInChildren<Renderer>() : Array.Empty<Renderer>();

                if (_importMaterials)
                {
                    // Get materials
                    var map = GetExternalObjectMap();
                    var materialHash = new HashSet<UnityEngine.Material>();
                    var materials = renderers.SelectMany(r =>
                    {
	                    return r.sharedMaterials.Select(mat =>
	                    {
		                    if (mat && materialHash.Add(mat))
		                    {
			                    var matName = string.IsNullOrEmpty(mat.name) ? mat.shader.name : mat.name;
			                    if (matName == mat.shader.name)
			                    {
				                    matName = matName.Substring(Mathf.Min(
					                    matName.LastIndexOf("/", StringComparison.Ordinal) + 1, matName.Length - 1));
			                    }

			                    mat.name = matName;
		                    }

		                    return mat;
	                    });
                    }).Distinct().ToList();

                    // TODO check if we really only want to do this for files that don't have scenes/nodes
                    if (renderers.Length == 0)
                    {
	                    // add materials directly from glTF
	                    foreach (var entry in importer.MaterialCache)
	                    {
		                    if (!materials.Contains(entry.UnityMaterial))
		                    {
			                    materials.Add(entry.UnityMaterial);
		                    }
	                    }
                    }

                    // apply material remap
                    foreach(var r in renderers)
                    {
	                    // remap materials to external objects
	                    var m = r.sharedMaterials;
	                    for (var i = 0; i < m.Length; i++)
	                    {
		                    var mat = m[i];
		                    if (mat)
		                    {
			                    var si = new SourceAssetIdentifier(mat);
			                    if (map.TryGetValue(si, out var value))
				                    m[i] = value as Material;
		                    }
	                    }
	                    r.sharedMaterials = m;
                    };

                    // Get textures
                    var textureHash = new HashSet<Texture2D>();
                    var texMaterialMap = new Dictionary<Texture2D, List<TexMaterialMap>>();
                    var textures = materials.SelectMany(mat =>
                    {
	                    if (!mat) return Enumerable.Empty<Texture2D>();
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
                    }).Distinct().ToList();

                    // texture asset remapping
                    foreach (var entry in texMaterialMap)
                    {
	                    var tex = entry.Key;
	                    var texPropertyList = entry.Value;
	                    foreach (var propertyEntry in texPropertyList)
	                    {
		                    var si = new SourceAssetIdentifier(tex);
		                    if (map.TryGetValue(si, out var value))
		                    {
			                    propertyEntry.Material.SetTexture(propertyEntry.Property, value as Texture);
			                    tex.hideFlags = HideFlags.HideInHierarchy;
		                    }
		                    else if (tex.hideFlags == HideFlags.HideInInspector)
		                    {
			                    // clean up mock textures we only generated on import for remapping
			                    propertyEntry.Material.SetTexture(propertyEntry.Property, null);
			                    textures.Remove(tex);
		                    }
	                    }
                    }

                    // Save textures as separate assets and rewrite refs
                    // TODO: Support for other texture types
                    if (textures.Count > 0)
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
                    if (materials.Count > 0)
                    {
                        foreach (var mat in materials)
                        {
	                        if (!mat) continue;
	                        // ensure materials that are overriden aren't shown in the hierarchy.
	                        var si = new SourceAssetIdentifier(mat);
	                        if (map.ContainsKey(si))
		                        mat.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

	                        ctx.AddObjectToAsset(GetUniqueName(mat.name), mat);
                        }
                    }

                    m_Materials = materials.ToArray();
                    m_Textures = textures.ToArray();

#if !UNITY_2022_1_OR_NEWER
			        AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
#endif
				}
                else
                {
	                // Workaround to get the default primitive material for the current render pipeline
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
            catch (Exception e)
            {
	            Debug.LogException(e);
                if (gltfScene) DestroyImmediate(gltfScene);
                throw;
            }

#if UNITY_2017_3_OR_NEWER
	        if (gltfScene)
	        {
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
	        if (gltfScene)
	        {
				ctx.SetMainObject(gltfScene);
	        }
	        else if (m_Materials.Length > 0)
	        {
		        if (m_Materials.Length == 1)
		        {
			        ctx.SetMainObject(m_Materials[0]);
		        }
	        }
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

	        foreach(var plugin in context.Plugins)
		        plugin.OnAfterImport();
		}

        private const string ColorSpaceDependency = nameof(GLTFImporter) + "_" + nameof(PlayerSettings.colorSpace);

#if UNITY_2020_2_OR_NEWER
        [InitializeOnLoadMethod]
        private static void UpdateColorSpace()
        {
	        AssetDatabase.RegisterCustomDependency(ColorSpaceDependency, Hash128.Compute((int) PlayerSettings.colorSpace));
        }
#endif

	    private void CreateGLTFScene(GLTFImportContext context, out GameObject scene,
		    out AnimationClip[] animationClips, out GLTFSceneImporter importer)
	    {
		    var projectFilePath = context.AssetContext.assetPath;

		    // TODO: replace with GltfImportContext
		    var importOptions = new ImportOptions
		    {
			    DataLoader = new FileLoader(Path.GetDirectoryName(projectFilePath)),
			    AnimationMethod = _importAnimations,
			    AnimationLoopTime = _animationLoopTime,
			    AnimationLoopPose = _animationLoopPose,
			    ImportContext = context
		    };

		    using (var stream = File.OpenRead(projectFilePath))
		    {
			    GLTFParser.ParseJson(stream, out var gltfRoot);
			    stream.Position = 0; // Make sure the read position is changed back to the beginning of the file
			    var loader = new GLTFSceneImporter(gltfRoot, stream, importOptions);
			    loader.MaximumLod = _maximumLod;
			    loader.IsMultithreaded = true;

			    // Need to call with RunSync, otherwise the draco loader will freeze the editor
			    AsyncHelpers.RunSync(() => loader.LoadSceneAsync());

			    if (gltfRoot.ExtensionsUsed != null)
			    {
				    _extensions = gltfRoot.ExtensionsUsed
					    .Select(x => new ExtensionInfo()
					    {
						    name = x,
						    supported = true,
						    used = true,
						    required = gltfRoot.ExtensionsRequired?.Contains(x) ?? false,
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

			    // for Editor import, we also want to load unreferenced assets that wouldn't be loaded at runtime
			    AsyncHelpers.RunSync(() => loader.LoadUnreferencedAssetsAsync());

			    importer = loader;
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

#if UNITY_2020_2_OR_NEWER && !UNITY_2021_3_OR_NEWER
    class RenderPipelineWatcher
    {
	    [InitializeOnLoadMethod]
	    static void RegisterForRenderPipelineChanges()
	    {
#if UNITY_2021_2_OR_NEWER
		    RenderPipelineManager.activeRenderPipelineTypeChanged += OnRenderPipelineTypeChanged;
#else
		    var lastPipeline = GraphicsSettings.currentRenderPipeline;
		    EditorApplication.update += () =>
		    {
			    if (GraphicsSettings.currentRenderPipeline != lastPipeline)
			    {
				    lastPipeline = GraphicsSettings.currentRenderPipeline;
				    OnRenderPipelineTypeChanged();
			    }
		    };
#endif
	    }

	    static void OnRenderPipelineTypeChanged()
	    {
		    var pipelineName = GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.GetType().Name : "BuiltIn";
			AssetDatabase.RegisterCustomDependency($"{nameof(GraphicsSettings)}.{nameof(GraphicsSettings.currentRenderPipeline)}", Hash128.Compute(pipelineName));
			AssetDatabase.Refresh();
	    }
    }
#endif

}
#endif
