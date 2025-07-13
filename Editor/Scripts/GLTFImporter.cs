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
using GLTF;
using UnityEditor.Build;
using UnityGLTF.Extensions;
using UnityGLTF.Plugins;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
using UnityEditorInternal;
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
    public class GLTFImporter : ScriptedImporter
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
        [Tooltip("Reduces identical resources. e.g. when identical meshes are found, only one will be imported.")]
        [SerializeField] internal DeduplicateOptions _deduplicateResources = DeduplicateOptions.None;
        [SerializeField] internal int _maximumLod = 300;
        [SerializeField] internal bool _readWriteEnabled = true;
        
        // Just for backwards compatibility > should remove in future > use _addColliders instead
		[Obsolete("Use _addColliders instead")]
        [SerializeField] internal bool _generateColliders = false;
        [SerializeField] internal GLTFSceneImporter.ColliderType _addColliders = GLTFSceneImporter.ColliderType.None;
        
        [SerializeField] internal bool _swapUvs = false;
        [SerializeField] internal bool _generateLightmapUVs = false;
	    [Tooltip("When false, the index of the BlendShape is used as name.")]
        [SerializeField] internal bool _importBlendShapeNames = true;
	    [Tooltip("Blend shape frame weight import multiplier. Default is 1. For compatibility with some FBX animations you may need to use 100.")]
	    [SerializeField] internal BlendShapeFrameWeightSetting _blendShapeFrameWeight = new BlendShapeFrameWeightSetting(BlendShapeFrameWeightSetting.MultiplierOption.Multiplier1);
        [SerializeField] internal GLTFImporterNormals _importNormals = GLTFImporterNormals.Import;
        [SerializeField] internal GLTFImporterNormals _importTangents = GLTFImporterNormals.Import;
        [SerializeField] internal CameraImportOption _importCamera = CameraImportOption.ImportAndCameraDisabled;
        [SerializeField] internal AnimationMethod _importAnimations = AnimationMethod.Mecanim;
        [SerializeField] internal bool _mecanimHumanoidFlip = false;
        [SerializeField] internal bool _addAnimatorComponent = false;
        [SerializeField] internal bool _animationLoopTime = true;
        [SerializeField] internal bool _animationLoopPose = false;
        [SerializeField] internal bool _importMaterials = true;
        [SerializeField] internal bool _enableGpuInstancing = false;
        [SerializeField] internal bool _texturesReadWriteEnabled = true;
        [SerializeField] internal bool _generateMipMaps = true;
        [Tooltip("Enable this to get the same main asset identifiers as glTFast uses. This is recommended for new asset imports. Note that changing this for already imported assets will break their scene references and require manually re-adding the affected assets.")]
        [SerializeField] internal bool _useSceneNameIdentifier = false;
        [Tooltip("Compress textures after import using the platform default settings. If you need more control, use a .gltf file instead.")]
        [SerializeField] internal GLTFImporterTextureCompressionQuality _textureCompression = GLTFImporterTextureCompressionQuality.None;
        [SerializeField, Multiline] internal string _gltfAsset = default;
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

        internal List<TextureInfo> Textures => _textures;

        // Import Plugin Overrides
        [SerializeField] // but we can serialize their YAML representations as string
        internal List<PluginInfo> _importPlugins = new List<PluginInfo>();
        
        [Serializable]
        internal class ExtensionInfo
        {
	        public string name;
	        [HideInInspector]
	        public bool supported;
	        [HideInInspector]
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
		public class PluginInfo
		{
			public string typeName;
			public bool overrideEnabled;
			public bool enabled;
			public string jsonSettings;
		}

        [Serializable]
        public class TextureInfo
        {
	        public Texture2D texture;
	        public bool shouldBeLinear;
	        public bool shouldBeNormalMap;
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
	        
	        // Add shader dependencies to ensure they're imported first
	        dependencies.Add(AssetDatabase.GUIDToAssetPath(PBRGraphMap.PBRGraphGuid));
	        dependencies.Add(AssetDatabase.GUIDToAssetPath(UnlitGraphMap.UnlitGraphGuid));

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
			        Debug.LogError($"Exception when importing glTF dependencies for {path}:\n" + e, GetAtPath(path));
			        throw;
		        }
	        }
	        return dependencies.Distinct().ToArray();
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
	        var settings = GLTFSettings.GetDefaultSettings();
	        
	        // make a copy, and apply import override settings
	        foreach (var importPlugin in _importPlugins)
	        {
		        if (importPlugin == null || !importPlugin.overrideEnabled) continue;
		        var existing = settings.ImportPlugins.Find(x => x.GetType().FullName == importPlugin.typeName);
		        if (existing)
		        {
			        existing.Enabled = importPlugin.enabled;
			        JsonUtility.FromJsonOverwrite(importPlugin.jsonSettings, existing);
		        }
	        }
	        var context = new GLTFImportContext(ctx, settings) { ImportScaleFactor = _scaleFactor };

            GameObject gltfScene = null;
            AnimationClip[] animations = null;
            Mesh[] meshes = null;

	        var uniqueNames = new Dictionary<int, int>(100);
	        uniqueNames.Add("main asset".GetHashCode(), 0);
            EnsureShadersAreLoaded();

#if UNITY_2020_2_OR_NEWER && !UNITY_2021_3_OR_NEWER
	        // We don't need this custom dependency in 2021.3 and later, because there we always use ShaderGraph.
            ctx.DependsOnCustomDependency($"{nameof(GraphicsSettings)}.{nameof(GraphicsSettings.currentRenderPipeline)}");
#endif

            string GetUniqueName(string desiredName)
            {
	            var hash = desiredName.GetHashCode();
	            if (uniqueNames.ContainsKey(hash))
	            {
		            uniqueNames[hash]++;
		            return $"{desiredName} ({uniqueNames[hash]})";
	            }
	            else
	            {
		            uniqueNames.Add(hash, 0);
		            return desiredName;
	            }
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
	                // To avoid removing a Root object which is animated, we collect from all animation clips the paths that are animated.
	                var pathBindings = new List<string>();
	                if (animations != null)
	                {
		                foreach (var aniClip in animations)
		                {
			                var bindings = AnimationUtility.GetCurveBindings(aniClip);
			                var distinctPaths = bindings.Select( x => x.path).Distinct();
							pathBindings.AddRange(distinctPaths);
		                }
	                }
	                pathBindings = pathBindings.Distinct().ToList();
	                
                    var t = gltfScene.transform;
                    var existingAnimator = t.GetComponent<Animator>();
                    var hadAnimator = (bool)existingAnimator;
                    var existingAvatar = existingAnimator ? existingAnimator.avatar : default;
                    var rootIsAnimated = false;
                    if (existingAnimator)
                    {
	                    var firstChildName = t.childCount > 0 ? t.GetChild(0).gameObject.name : "";
	                    // check if the object is animated, when true, cancel here
						rootIsAnimated = firstChildName != "" && pathBindings.Contains(firstChildName);
						
						if (!rootIsAnimated)
							DestroyImmediate(existingAnimator);
                    }

                    var animationPathPrefix = "";
                    var originalImportName = gltfScene.name;
                    if (!rootIsAnimated)
                    {
	                    while (
		                    gltfScene.transform.childCount == 1 &&
		                    gltfScene.GetComponents<Component>().Length == 1) // Transform component
	                    {
		                    // check if the object is animated, when true, cancel here
		                    if (pathBindings.Contains(animationPathPrefix != ""
			                        ? $"{animationPathPrefix}/{gltfScene.transform.GetChild(0).gameObject.name}"
			                        : gltfScene.transform.GetChild(0).gameObject.name))
			                    break;
		                    
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
                    } // (!rootIsAnimated)

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
	                var transforms = gltfScene.GetComponentsInChildren<Transform>(true);
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
		            meshFilters = gltfScene.GetComponentsInChildren<MeshFilter>(true).Select(x => (x.gameObject, x.sharedMesh)).ToList();
	                meshFilters.AddRange(gltfScene.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(x => (x.gameObject, x.sharedMesh)));
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

	                    if (mesh.bindposes != null && mesh.bindposes.Length > 0)
	                    {
		                    var bindPoses = mesh.bindposes;
		                    for (var i = 0; i < bindPoses.Length; ++i)
		                    {
			                    bindPoses[i].GetTRSProperties(out var p, out var q, out var s);
			                    bindPoses[i].SetTRS(p * _scaleFactor, q, s);
		                    }

		                    mesh.bindposes = bindPoses;
	                    }
                    }
                    if (_generateLightmapUVs)
                    {
	                    var hasTriangleTopology = true;
	                    for (var i = 0; i < mesh.subMeshCount; i++)
		                    hasTriangleTopology &= mesh.GetTopology(i) == MeshTopology.Triangles;
	                    
	                    if (hasTriangleTopology)
	                    {
		                    // Clean uv2 if it exists. This matches Unity's ModelImporter behavior. See https://github.com/KhronosGroup/UnityGLTF/issues/871
		                    if (mesh.uv2 != null)
			                    mesh.uv2 = null;
		                    
		                    // Unity's Unwrapping.GenerateSecondaryUVSet() does not work with submesh baseVertex offsets.
		                    // So if we have any of those, we need to remove the baseVertex offsets first, otherwise we get garbage meshes.
		                    // See https://github.com/KhronosGroup/UnityGLTF/issues/668
		                    var count = mesh.subMeshCount;
		                    if (count > 1)
		                    {
			                    for (var i = 0; i < count; i++)
			                    {
				                    var subMeshDescriptor = mesh.GetSubMesh(i);
				                    if (subMeshDescriptor.baseVertex == 0) continue;
				                    
				                    // Read indices with applyBaseVertex = true and write them back without baseVertex offset
				                    var indices = mesh.GetIndices(i, true);
				                    mesh.SetIndices(indices, MeshTopology.Triangles, i, false, 0);
				                    
				                    // Update the descriptor
				                    subMeshDescriptor.baseVertex = 0;
				                    mesh.SetSubMesh(i, subMeshDescriptor, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds);
			                    }
		                    }

		                    // TODO We might want to be able to set the unwrap settings via the importer.
		                    UnwrapParam.SetDefaults(out var unwrapSettings);
							Unwrapping.GenerateSecondaryUVSet(mesh, unwrapSettings);
	                    }
                    }

					mesh.UploadMeshData(!_readWriteEnabled);
					mesh.RecalculateBounds(MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);

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
	                var avatar = HumanoidSetup.AddAvatarToGameObject(gltfScene, _mecanimHumanoidFlip);
	                if (avatar)
						ctx.AddObjectToAsset("avatar", avatar);
                }

                var renderers = gltfScene ? gltfScene.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();

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
		                    
		                    // In case the material is explicit set to instancing (e.g. EXT_mesh_gpu_instancing is used), don't override it.
		                    if (!mat.enableInstancing)
								mat.enableInstancing = _enableGpuInstancing;
		                    
		                    // If we're in built-in RP, don#t use GPU instancing since Shader Graph doesn't support it.
		                    if (!GraphicsSettings.currentRenderPipeline)
		                    {
			                    // Shader Graphs are not compatible with GPU instancing, so we need to turn it off
			                    // even if the user has explicitly turned the option to import materials with GPU instancing on.
			                    mat.enableInstancing = false;
		                    }
		                    
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
			                        var format = TextureImporterHelper.GetAutomaticFormat(tex, ctx.selectedBuildTarget);
			                        var convertedFormat = (TextureFormat)(int)format;
			                        if ((int)convertedFormat > -1)
			                        {
				                        //Debug.Log("Compressing texture " + tex.name + "(format: " + tex.format + ", mips: " + tex.mipmapCount + ") to: " + convertedFormat);
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
	            Debug.LogException(e, this);
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
			ctx.DependsOnCustomDependency(NormalMapEncodingDependency);
#endif
	        if (gltfScene)
	        {
				ctx.SetMainObject(gltfScene);
	        }
	        else if (m_Materials.Length > 0)
	        {
		        // Create a "MaterialLibrary" asset that will hold one or more materials imported from glTF
		        var library = ScriptableObject.CreateInstance<MaterialLibrary>();
		        ctx.AddObjectToAsset("material library", library);
		        ctx.SetMainObject(library);
		        /*
		        if (m_Materials.Length == 1)
		        {
			        ctx.SetMainObject(m_Materials[0]);
		        }
		        */
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
	        
	        // run texture verification and warn about wrong configuration
	        if (!GLTFImporterHelper.TextureImportSettingsAreCorrect(this))
		        Debug.LogWarning("Some Textures have incorrect linear/sRGB settings. Use the \"Fix All\" button in the importer to adjust.", this);

	        if (context.SceneImporter != null)
		        context.SceneImporter.Dispose();
		}
		        

        private const string ColorSpaceDependency = nameof(GLTFImporter) + "_" + nameof(PlayerSettings.colorSpace);
        private const string NormalMapEncodingDependency = nameof(GLTFImporter) + "_normalMapEncoding";

#if UNITY_2020_2_OR_NEWER
        [InitializeOnLoadMethod]
        private static void UpdateCustomDependencies()
        {
	        AssetDatabase.RegisterCustomDependency(ColorSpaceDependency, Hash128.Compute((int) PlayerSettings.colorSpace));

	        BuildTargetGroup activeTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
#if UNITY_2023_1_OR_NEWER
	        var normalEncoding = PlayerSettings.GetNormalMapEncoding(NamedBuildTarget.FromBuildTargetGroup(activeTargetGroup));
#else				
			var normalEncoding = PlayerSettings.GetNormalMapEncoding(activeTargetGroup);
#endif	        
	        
			AssetDatabase.RegisterCustomDependency(NormalMapEncodingDependency, Hash128.Compute((int) normalEncoding));
        }

#if UNITY_2021_3_OR_NEWER
        // This asset postprocessor ensures that dependencies are updated whenever any asset is reimported.
        // So for example if any texture is reimported (because color spaces or texture encoding settings have been changed)
        // then we can update the custom dependencies here and potentially reimport required glTF assets.
        class UpdateCustomDependenciesAfterImport : AssetPostprocessor
        {
	        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	        {
		        UpdateCustomDependencies();
	        }
        }
#endif
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
			    ImportTangents = _importTangents,
			    ImportBlendShapeNames = _importBlendShapeNames,
			    BlendShapeFrameWeight = _blendShapeFrameWeight,
			    CameraImport = _importCamera,
			    DeduplicateResources = _deduplicateResources,
		    };

		    using (var stream = File.OpenRead(projectFilePath))
		    {
			    GLTFParser.ParseJson(stream, out var gltfRoot);
			    
			    // Early writing of _extensions – if there are any import errors
			    // we want to be able to show proper warnings/errors for them.
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
			    
			    stream.Position = 0; // Make sure the read position is changed back to the beginning of the file
			    var loader = new GLTFSceneImporter(gltfRoot, stream, importOptions);
			    loader.KeepCPUCopyOfTexture = _texturesReadWriteEnabled;
			    loader.GenerateMipMapsForTextures = _generateMipMaps;
			    loader.LoadUnreferencedImagesAndMaterials = true;
			    loader.MaximumLod = _maximumLod;
			    loader.IsMultithreaded = true;

			    // For backwards compatibility, _addColliders has replaced _generateColliders
#pragma warning disable CS0618
			    if (_generateColliders)
			    {
				    _addColliders = GLTFSceneImporter.ColliderType.Mesh;
				    _generateColliders = false;
			    }
#pragma warning restore CS0618
			    
			    loader.Collider = _addColliders;

			    // Need to call with RunSync, otherwise the draco loader will freeze the editor
			    AsyncHelpers.RunSync(() => loader.LoadSceneAsync());

			    _textures = loader.TextureCache
				    .Where(x => x != null)
				    .Select(x => new TextureInfo() { texture = x.Texture, shouldBeLinear = x.IsLinear, shouldBeNormalMap = x.IsNormal })
				    .ToList();

			    scene = loader.LastLoadedScene;
			    animationClips = loader.CreatedAnimationClips;

			    _gltfAsset = loader.Root.Asset?.ToString(true);
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
