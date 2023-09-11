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

[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("UnityGLTF.ShaderGraph")]

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
    public class GLTFImporter : ScriptedImporter, IGLTFImportRemap
    {
	    private const int ImporterVersion = 9;

	    private static void EnsureShadersAreLoaded()
	    {
		    const string PackagePrefix = "Packages/org.khronos.unitygltf/";
		    
		    // We want to ensure shaders are already imported when they may
		    // be needed by the importer.
		    var shaders = new string[] {
			    PackagePrefix + "Runtime/Shaders/ShaderGraph/PBRGraph.shadergraph",
			    PackagePrefix + "Runtime/Shaders/ShaderGraph/UnlitGraph.shadergraph",
			    PackagePrefix + "Runtime/Shaders/PbrMetallicRoughness.shader",
			    PackagePrefix + "Runtime/Shaders/PbrSpecularGlossiness.shader",
			    PackagePrefix + "Runtime/Shaders/Unlit.shader",
		    };
		    
		    // Some TextureImporter settings are only available on a concrete TextureImporter
		    // instance, so we have to keep a texture around to ensure we can access those methods...
		    var textures = new string[]
		    {
				PackagePrefix + "Editor/Scripts/Internal/DefaultImportSettings/DefaultTexture.png",
		    };

		    foreach (var shaderPath in shaders)
		    {
			    AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
		    }
		    foreach (var file in textures)
		    {
			    AssetDatabase.LoadAssetAtPath<Texture2D>(file);
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
        [Tooltip("Compress textures after import using the platform default settings. If you need more control, use a .gltf file instead.")]
        [SerializeField] internal GLTFImporterTextureCompressionQuality _textureCompression = GLTFImporterTextureCompressionQuality.None;
        
        // for humanoid importer
        [SerializeField] internal bool m_OptimizeGameObjects = false;
        [SerializeField] internal HumanDescription m_HumanDescription = new HumanDescription();

        // asset remapping
        [SerializeField] internal Material[] m_Materials = new Material[0];
        [SerializeField] internal Texture[] m_Textures = new Texture[0];
        [SerializeField] internal bool m_HasSceneData = true;
        [SerializeField] internal bool m_HasAnimationData = true;
        [SerializeField] internal bool m_HasMaterialData = true;
        [SerializeField] internal bool m_HasTextureData = true;
        [SerializeField] [NonReorderable] private AnimationClipImportInfo[] m_Animations = new AnimationClipImportInfo[0];

		// Import messages (extensions, warnings, errors, ...)
        [NonReorderable] [SerializeField] internal List<ExtensionInfo> _extensions;
        [NonReorderable] [SerializeField] private List<TextureInfo> _textures;
        [SerializeField] internal string _mainAssetIdentifier;

        // TODO make internal and allow access for relevant assemblies
        internal List<TextureInfo> Textures => _textures;

        [Serializable]
        internal class ExtensionInfo
        {
	        public string name;
	        public bool supported;
	        public bool used;
	        public bool required;
        }
        
        // Matches TextureCompressionQuality and adds "None" as option
        internal enum GLTFImporterTextureCompressionQuality
		{
			None = -50,
	        Fast = 0,
	        Normal = 50,
	        Best = 100
		}

        [Serializable]
        public class TextureInfo
        {
	        public Texture2D texture;
	        public bool shouldBeLinear;
        }

        [Serializable]
        public class AnimationClipImportInfo
        {
	        [HideInInspector]
	        public string name;

	        // TODO cutting ain't trivial. One would need to create new keyframes by linear interpolation so that the result is still the same
	        // public AnimationClip sourceClip;
	        // public bool loopTime;
	        // public float startTime;
	        // public float endTime;
		}

#if !UNIYT_2020_2_OR_NEWER
	    private class NonReorderableAttribute : Attribute {}
#endif

        private static string[] GatherDependenciesFromSourceFile(string path)
        {
	        var dependencies = new List<string>();

	        // only supported glTF for now - would be harder to check for external references in glb assets.
	        if (!path.ToLowerInvariant().EndsWith(".gltf"))
		        return dependencies.ToArray();
	        
	        // read minimal JSON, check if there's a bin buffer, and load that.
	        // all other assets should be "proper assets" and be found by the asset database, but we're not importing .bin
	        // since it's too common as a file type.

	        var dir = Path.GetDirectoryName(path);

	        void CheckAndAddDependency(string uri)
	        {
		        var combinedPath = FileLoader.CombinePaths(dir, uri);
		        if (!File.Exists(Path.Combine(dir, uri)))
			        uri = Uri.UnescapeDataString(uri);
		        if (File.Exists(combinedPath))
					dependencies.Add(combinedPath);
		        // TODO check if inside the project/any package, could be an absolute path
		        else if (File.Exists(uri))
			        dependencies.Add(uri);
	        }

	        using (var reader = new StreamReader(path))
	        {
		        try
		        {
			        GLTFParser.ParseJson(reader.BaseStream, out var gltf);
			        var externalBuffers = gltf?.Buffers?.Where(b => b?.Uri != null && b.Uri.ToLowerInvariant().EndsWith(".bin"));
			        var externalImages = gltf?.Images?.Where(b => b?.Uri != null);

			        if (externalBuffers != null)
			        {
				        foreach (var buffer in externalBuffers)
				        {
					        CheckAndAddDependency(buffer.Uri);
				        }
			        }

			        if (externalImages != null)
			        {
				        foreach (var image in externalImages)
				        {
					        CheckAndAddDependency(image.Uri);
				        }
			        }
		        }
		        catch (Exception e)
		        {
			        Debug.LogError($"Exception when importing glTF dependencies for {path}:\n" + e);
			        throw;
		        }
	        }
	        return dependencies.Distinct().ToArray();
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
                    var originalImportName = gltfScene.name;
                    while (
                        gltfScene.transform.childCount == 1 &&
                        gltfScene.GetComponents<Component>().Length == 1) // Transform component
                    {
                        var parent = gltfScene;
                        gltfScene = gltfScene.transform.GetChild(0).gameObject;
                        var importName = gltfScene.name;
                        t = gltfScene.transform;
                        t.parent = null; // To keep transform information in the new parent
                        DestroyImmediate(parent); // Get rid of the parent
                        if (animationPathPrefix != "")
	                        animationPathPrefix += "/";
                        animationPathPrefix += importName;
                    }
                    animationPathPrefix += "/";

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

                    gltfScene.name = originalImportName;
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
                    var materials = new List<UnityEngine.Material>();
                    if (importer._defaultLoadedMaterial != null)
                    {
	                    importer._defaultLoadedMaterial.UnityMaterialWithVertexColor.name = "Default";
						materials.Add(importer._defaultLoadedMaterial.UnityMaterialWithVertexColor);
                    }

                    foreach (var entry in importer.MaterialCache)
                    {
	                    if (entry != null && entry.UnityMaterialWithVertexColor)
	                    {
		                    var mat = entry.UnityMaterialWithVertexColor;
		                    var matName = string.IsNullOrEmpty(mat.name) ? mat.shader.name : mat.name;
		                    if (matName == mat.shader.name)
		                    {
			                    matName = matName.Substring(Mathf.Min(
				                    matName.LastIndexOf("/", StringComparison.Ordinal) + 1, matName.Length - 1));
		                    }

		                    mat.name = matName;
		                    materials.Add(mat);
	                    }
                    }

                    // apply material remap
                    foreach (var r in renderers)
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
			                    {
				                    if (!value) map.Remove(si);
				                    else m[i] = value as Material;
			                    }
		                    }
	                    }
	                    r.sharedMaterials = m;
                    };

                    // Get textures - only the ones actually referenced by imported materials
                    /*
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
					*/

                    // all imported textures - the ones that are directly referenced and
                    // the ones that are invalid (missing files with temp generated data)
                    var invalidTextures = importer.InvalidImageCache
						.Select(x => x)
						.Where(x => x)
						.ToList();

	                var textures = importer.TextureCache
		                .Where(x => x != null)
		                .Select(x => x.Texture)
		                .Where(x => x)
		                .Union(invalidTextures).Distinct().ToList();

	                // if we're not importing materials or textures, we can clear the lists
	                // so that no assets are actually created.
	                if (!_importMaterials)
	                {
		                materials.Clear();
		                textures.Clear();
	                }

                    /*
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
                    */

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
		                        if (invalidTextures.Contains(tex))
		                            tex.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
		                        
		                        if (_textureCompression != GLTFImporterTextureCompressionQuality.None)
		                        {
			                        // platform-dependant texture compression
			                        var buildTargetName = BuildPipeline.GetBuildTargetName(ctx.selectedBuildTarget);
			                        var format = TextureImporterHelper.GetAutomaticFormat(tex, buildTargetName);
			                        var convertedFormat = (TextureFormat)(int)format;
			                        if ((int)convertedFormat > -1)
			                        {
				                        // Debug.Log("Compressing texture " + tex.name + "(format: " + tex.format + ", mips: " + tex.mipmapCount + ") to: " + convertedFormat);
				                        EditorUtility.CompressTexture(tex, convertedFormat, (TextureCompressionQuality) (int) _textureCompression);
				                        // Debug.Log("Mips now: " + tex.mipmapCount); // TODO figure out why mipmaps disappear here
			                        }
		                        }

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
	                        if (map.TryGetValue(si, out var remappedMaterial) && remappedMaterial)
		                        mat.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

	                        ctx.AddObjectToAsset(GetUniqueName(mat.name), mat);
                        }
                    }

                    m_Materials = materials.ToArray();
                    m_Textures = textures.ToArray();
                    m_HasSceneData = gltfScene;
                    m_HasMaterialData = importer.Root.Materials != null && importer.Root.Materials.Count > 0;
                    m_HasTextureData = importer.Root.Textures != null && importer.Root.Textures.Count > 0;
                    m_HasAnimationData = importer.Root.Animations != null && importer.Root.Animations.Count > 0;
                    var newAnimations = new AnimationClipImportInfo[animations != null ? animations.Length : 0];
                    if (animations != null)
                    {
	                    for (var i = 0; i < animations.Length; ++i)
	                    {
		                    // get previous import info if it exists
		                    // TODO won't work if there are multiple animations with the same name in the source
		                    // We need to find the source instance index (e.g. 3rd time a clip is named "DoSomething")
		                    // And then find the matching name index (also the 3rd occurrence of "DoSomething" in m_Animations)
		                    var prev = Array.Find(m_Animations, x => x.name == animations[i].name);
		                    if (prev != null)
		                    {
			                    newAnimations[i] = prev;
		                    }
		                    else
		                    {
			                    var newClipInfo = new AnimationClipImportInfo();
			                    newClipInfo.name = animations[i].name;
			                    // newClipInfo.startTime = 0;
			                    // newClipInfo.endTime = animations[i].length;
			                    // newClipInfo.loopTime = true;
			                    newAnimations[i] = newClipInfo;
		                    }
	                    }
                    }
                    m_Animations = newAnimations;

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
			    ImportContext = context,
			    SwapUVs = _swapUvs,
			    ImportNormals = _importNormals,
			    ImportTangents = _importTangents
		    };

		    using (var stream = File.OpenRead(projectFilePath))
		    {
			    GLTFParser.ParseJson(stream, out var gltfRoot);
			    stream.Position = 0; // Make sure the read position is changed back to the beginning of the file
			    var loader = new GLTFSceneImporter(gltfRoot, stream, importOptions);
			    loader.LoadUnreferencedImagesAndMaterials = true;
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
	        if (type == typeof(Texture))
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
