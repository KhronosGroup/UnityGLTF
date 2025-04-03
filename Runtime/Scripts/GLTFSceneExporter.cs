#if UNITY_EDITOR
#define ANIMATION_EXPORT_SUPPORTED
#endif

#if ANIMATION_EXPORT_SUPPORTED && (UNITY_ANIMATION || !UNITY_2019_1_OR_NEWER)
#define ANIMATION_SUPPORTED
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GLTF.Schema;
using Unity.Profiling;
using UnityEngine;
using UnityGLTF.Extensions;
using UnityGLTF.Plugins;

namespace UnityGLTF
{
	public class ExportContext
	{
		public bool TreatEmptyRootAsScene = false;
		public bool MergeClipsWithMatchingNames = false;
		public LayerMask ExportLayers = -1;
		public ILogger logger;
		internal readonly GLTFSettings settings;

		public ExportContext() : this(GLTFSettings.GetOrCreateSettings()) { }

		public ExportContext(GLTFSettings settings)
		{
			if (!settings) settings = GLTFSettings.GetOrCreateSettings();
			if (settings.UseMainCameraVisibility)
				ExportLayers = Camera.main ? Camera.main.cullingMask : -1;
			this.settings = settings;
		}

		public GLTFSceneExporter.RetrieveTexturePathDelegate TexturePathRetriever = (texture) => texture.name;
		
		// TODO Should we make all the callbacks on ExportContext obsolete?
		// Pro: We can remove them from the API
		// Con: No direct way to "just add callbacks" right now, always needs a plugin.
		// See GLTFSceneExporter for a case here we "just want callbacks" instead of a new class/context
		public GLTFSceneExporter.AfterSceneExportDelegate AfterSceneExport;
		public GLTFSceneExporter.BeforeSceneExportDelegate BeforeSceneExport;
		public GLTFSceneExporter.AfterNodeExportDelegate AfterNodeExport;
		public GLTFSceneExporter.BeforeMaterialExportDelegate BeforeMaterialExport;
		public GLTFSceneExporter.AfterMaterialExportDelegate AfterMaterialExport;
		public GLTFSceneExporter.BeforeTextureExportDelegate BeforeTextureExport;
		public GLTFSceneExporter.AfterTextureExportDelegate AfterTextureExport;
		public GLTFSceneExporter.AfterPrimitiveExportDelegate AfterPrimitiveExport;
		public GLTFSceneExporter.AfterMeshExportDelegate AfterMeshExport;
		
		internal GLTFExportPluginContext GetExportContextCallbacks() => new ExportContextCallbacks(this);

#pragma warning disable CS0618 // Type or member is obsolete
		internal class ExportContextCallbacks : GLTFExportPluginContext
		{
			private readonly ExportContext _exportContext;

			internal ExportContextCallbacks(ExportContext context)
			{
				_exportContext = context;
			}
			
			public override void BeforeSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) => _exportContext.BeforeSceneExport?.Invoke(exporter, gltfRoot);
			public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) => _exportContext.AfterSceneExport?.Invoke(exporter, gltfRoot);
			public override void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node) => _exportContext.AfterNodeExport?.Invoke(exporter, gltfRoot, transform, node);

			public override bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode)
			{
				// static callback, run after options callback
				// we're iterating here because we want to stop calling any once we hit one that can export this material.
				if (_exportContext.BeforeMaterialExport != null)
				{
					var list = _exportContext.BeforeMaterialExport.GetInvocationList();
					foreach (var entry in list)
					{
						var cb = (GLTFSceneExporter.BeforeMaterialExportDelegate) entry;
						if (cb != null && cb.Invoke(exporter, gltfRoot, material, materialNode))
						{
							return true;
						}
					}
				}
				return false;
			}
			public override void AfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode) => _exportContext.AfterMaterialExport?.Invoke(exporter, gltfRoot, material, materialNode);
			public override void BeforeTextureExport(GLTFSceneExporter exporter, ref GLTFSceneExporter.UniqueTexture texture, string textureSlot) => _exportContext.BeforeTextureExport?.Invoke(exporter, ref texture, textureSlot);
			public override void AfterTextureExport(GLTFSceneExporter exporter, GLTFSceneExporter.UniqueTexture texture, int index, GLTFTexture tex) => _exportContext.AfterTextureExport?.Invoke(exporter, texture, index, tex);
			public override void AfterPrimitiveExport(GLTFSceneExporter exporter, Mesh mesh, MeshPrimitive primitive, int index) => _exportContext.AfterPrimitiveExport?.Invoke(exporter, mesh, primitive, index);
			public override void AfterMeshExport(GLTFSceneExporter exporter, Mesh mesh, GLTFMesh gltfMesh, int index) => _exportContext.AfterMeshExport?.Invoke(exporter, mesh, gltfMesh, index);
		}
#pragma warning restore CS0618 // Type or member is obsolete
	}

	[Obsolete("Use UnityGLTF.ExportContext instead. (UnityUpgradable) -> UnityGLTF.ExportContext")]
	public class ExportOptions: ExportContext
	{
		public ExportOptions(): base() { }
		public ExportOptions(GLTFSettings settings): base(settings) { }
	}

	public partial class GLTFSceneExporter
	{
		// Available export callbacks.
		// Callbacks can be either set statically (for exporters that register themselves)
		// or added in the ExportOptions.
		public delegate string RetrieveTexturePathDelegate(Texture texture);
		public delegate void BeforeSceneExportDelegate(GLTFSceneExporter exporter, GLTFRoot gltfRoot);
		public delegate void AfterSceneExportDelegate(GLTFSceneExporter exporter, GLTFRoot gltfRoot);
		public delegate void AfterNodeExportDelegate(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node);
		/// <returns>True: material export is complete. False: continue regular export.</returns>
		public delegate bool BeforeMaterialExportDelegate(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode);
		public delegate void AfterMaterialExportDelegate(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode);
		public delegate void BeforeTextureExportDelegate(GLTFSceneExporter exporter, ref UniqueTexture texture, string textureSlot);
		public delegate void AfterTextureExportDelegate(GLTFSceneExporter exporter, UniqueTexture texture, int index, GLTFTexture tex);
		public delegate void AfterPrimitiveExportDelegate(GLTFSceneExporter exporter, Mesh mesh, MeshPrimitive primitive, int index);
		public delegate void AfterMeshExportDelegate(GLTFSceneExporter exporter, Mesh mesh, GLTFMesh gltfMesh, int index);

		
		private class LegacyCallbacksPlugin : GLTFExportPluginContext
		{
			public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) => GLTFSceneExporter.AfterSceneExport?.Invoke(exporter, gltfRoot);
			public override void BeforeSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) => GLTFSceneExporter.BeforeSceneExport?.Invoke(exporter, gltfRoot);
			public override void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node) => GLTFSceneExporter.AfterNodeExport?.Invoke(exporter, gltfRoot, transform, node);

			public override bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode)
			{
				// static callback, run after options callback
				// we're iterating here because we want to stop calling any once we hit one that can export this material.
				if (GLTFSceneExporter.BeforeMaterialExport != null)
				{
					var list = GLTFSceneExporter.BeforeMaterialExport.GetInvocationList();
					foreach (var entry in list)
					{
						var cb = (BeforeMaterialExportDelegate) entry;
						if (cb != null && cb.Invoke(exporter, gltfRoot, material, materialNode))
						{
							return true;
						}
					}
				}
				return false;
			}
			public override void AfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode) => GLTFSceneExporter.AfterMaterialExport?.Invoke(exporter, gltfRoot, material, materialNode);
		}
		
		private static ILogger Debug = UnityEngine.Debug.unityLogger;
		private List<GLTFExportPluginContext> _plugins = new List<GLTFExportPluginContext>();

		public IReadOnlyList<GLTFExportPluginContext> Plugins => _plugins;
		
		public struct TextureMapType
		{
			public const string BaseColor = "baseColorTexture";
			[Obsolete("Use BaseColor instead")] public const string Main = BaseColor;
			public const string Emissive = "emissiveTexture";
			[Obsolete("Use Emissive instead")] public const string Emission = Emissive;

			public const string Normal = "normalTexture";
			[Obsolete("Use Normal instead")] public const string Bump = Normal;
			public const string MetallicGloss = "metallicGloss";
			public const string MetallicRoughness = "metallicRoughnessTexture";
			public const string SpecGloss = "specularGlossinessTexture"; // not really supported anymore
			public const string Light = Linear;
			public const string Occlusion = "occlusionTexture";

			public const string Linear = "linear";
			public const string sRGB = "sRGB";
			public const string Custom_Unknown = "linearWithAlpha";
			public const string Custom_HDR = "hdr";

			[Obsolete("Use Linear or the right texture slot instead")] public const string MetallicGloss_DontConvert = Linear;

		}

		public struct TextureExportSettings
		{
			public bool isValid;

			// does the texture need a channel conversion when exporting
			public Conversion conversion;
			// do we know something about the alpha channel of this texture
			public AlphaMode alphaMode;
			// is the texture linear or sRGB
			public bool linear;
			// required for metallic-smoothness conversion
			public float smoothnessRangeMin;
			public float smoothnessRangeMax;
			public float metallicRangeMin;
			public float metallicRangeMax;
			public float occlusionRangeMin;
			public float occlusionRangeMax;

			public TextureExportSettings(TextureExportSettings source)
			{
				conversion = source.conversion;
				alphaMode = source.alphaMode;
				linear = source.linear;
				smoothnessRangeMin = source.smoothnessRangeMin;
				smoothnessRangeMax = source.smoothnessRangeMax;
				metallicRangeMin = source.metallicRangeMin;
				metallicRangeMax = source.metallicRangeMax;
				occlusionRangeMin = source.occlusionRangeMin;
				occlusionRangeMax = source.occlusionRangeMax;
				isValid = true;
			}

			public enum Conversion
			{
				None,
				MetalGlossChannelSwap,
				MetalGlossOcclusionChannelSwap,
				NormalChannel,
			}

			public enum AlphaMode
			{
				Never = 0,
				Always = 1,
				Heuristic = 2,
			}

			public static bool operator ==(TextureExportSettings lhs, TextureExportSettings rhs)
			{
				return lhs.Equals(rhs);
			}

			public static bool operator !=(TextureExportSettings lhs, TextureExportSettings rhs)
			{
				return !(lhs == rhs);
			}

			public bool Equals(TextureExportSettings other)
			{
				return
					conversion == other.conversion &&
				    alphaMode == other.alphaMode &&
				    linear == other.linear &&
					Mathf.Approximately(smoothnessRangeMin, other.smoothnessRangeMin) &&
					Mathf.Approximately(smoothnessRangeMax, other.smoothnessRangeMax) &&
					Mathf.Approximately(metallicRangeMin, other.metallicRangeMin) &&
					Mathf.Approximately(metallicRangeMax, other.metallicRangeMax) &&
					Mathf.Approximately(occlusionRangeMin, other.occlusionRangeMin) &&
					Mathf.Approximately(occlusionRangeMax, other.occlusionRangeMax);
			}

			public override bool Equals(object obj)
			{
				return obj is TextureExportSettings other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = (int)conversion;
					hashCode = (hashCode * 397) ^ (int)alphaMode;
					hashCode = (hashCode * 397) ^ linear.GetHashCode();
					hashCode = (hashCode * 397) ^ smoothnessRangeMin.GetHashCode();
					hashCode = (hashCode * 397) ^ smoothnessRangeMax.GetHashCode();
					hashCode = (hashCode * 397) ^ metallicRangeMin.GetHashCode();
					hashCode = (hashCode * 397) ^ metallicRangeMax.GetHashCode();
					hashCode = (hashCode * 397) ^ occlusionRangeMin.GetHashCode();
					hashCode = (hashCode * 397) ^ occlusionRangeMax.GetHashCode();
					return hashCode;
				}
			}
		}

		public TextureExportSettings GetExportSettingsForSlot(string textureSlot)
		{
			var exportSettings = new TextureExportSettings();
			exportSettings.isValid = true;
			exportSettings.metallicRangeMin = 0f;
			exportSettings.metallicRangeMax = 1f;
			exportSettings.smoothnessRangeMin = 0f;
			exportSettings.smoothnessRangeMax = 1f;
			exportSettings.occlusionRangeMin = 0f;
			exportSettings.occlusionRangeMax = 1f;

			switch (textureSlot)
			{
				case TextureMapType.BaseColor: // Main = new TextureExportSettings() { alphaMode = AlphaMode.Heuristic };
					exportSettings.linear = false;
					exportSettings.alphaMode = TextureExportSettings.AlphaMode.Heuristic;
					return exportSettings;
				case TextureMapType.Emissive: // Emission = new TextureExportSettings() { alphaMode = AlphaMode.Heuristic };
					exportSettings.linear = false;
					exportSettings.alphaMode = TextureExportSettings.AlphaMode.Never;
					return exportSettings;
				case TextureMapType.Normal: // Bump = new TextureExportSettings() { alphaMode = AlphaMode.Never, conversion = Conversion.NormalChannel };
					exportSettings.linear = true;
					exportSettings.alphaMode = TextureExportSettings.AlphaMode.Never;
					exportSettings.conversion = TextureExportSettings.Conversion.NormalChannel;
					return exportSettings;
				case TextureMapType.MetallicGloss: // MetallicGloss = new TextureExportSettings() { alphaMode = AlphaMode.Never, conversion = Conversion.MetalGlossChannelSwap, smoothnessMultiplier = 1f};
					exportSettings.linear = true;
					exportSettings.alphaMode = TextureExportSettings.AlphaMode.Never;
					exportSettings.conversion = TextureExportSettings.Conversion.MetalGlossChannelSwap;
					return exportSettings;
				case TextureMapType.MetallicRoughness:
					exportSettings.linear = true;
					exportSettings.alphaMode = TextureExportSettings.AlphaMode.Never;
					return exportSettings;

				case TextureMapType.SpecGloss: // SpecGloss = MetallicGloss; // not really supported anymore
					exportSettings.linear = true;
					exportSettings.alphaMode = TextureExportSettings.AlphaMode.Never;
					exportSettings.conversion = TextureExportSettings.Conversion.MetalGlossChannelSwap;
					return exportSettings;
				case TextureMapType.Occlusion: // Occlusion = Linear;
					exportSettings.linear = true;
					exportSettings.alphaMode = TextureExportSettings.AlphaMode.Never;
					return exportSettings;

				// custom slot types that allow us to export more arbitrary textures
				case TextureMapType.Linear: // MetallicGloss_DontConvert = Linear;
					exportSettings.linear = true;
					exportSettings.alphaMode = TextureExportSettings.AlphaMode.Heuristic;
					return exportSettings;
				case TextureMapType.sRGB: // MetallicGloss_DontConvert = Linear;
					exportSettings.linear = false;
					exportSettings.alphaMode = TextureExportSettings.AlphaMode.Heuristic;
					return exportSettings;
				case TextureMapType.Custom_Unknown:
				case "rgbm": // Custom_Unknown = new TextureExportSettings() { linear = true, alphaMode = AlphaMode.Always };
					exportSettings.linear = true;
					exportSettings.alphaMode = TextureExportSettings.AlphaMode.Always;
					return exportSettings;
				case TextureMapType.Custom_HDR: // Custom_HDR = new TextureExportSettings() { alphaMode = AlphaMode.Always };
					exportSettings.linear = true;
					exportSettings.alphaMode = TextureExportSettings.AlphaMode.Always;
					return exportSettings;
			}

			// assume unknown linear
			exportSettings.linear = true;
			exportSettings.alphaMode = TextureExportSettings.AlphaMode.Heuristic;
			return exportSettings;
		}

		private Material GetConversionMaterial(TextureExportSettings textureMapType)
		{
			switch (textureMapType.conversion)
			{
				case TextureExportSettings.Conversion.NormalChannel:
					return _normalChannelMaterial;
				case TextureExportSettings.Conversion.MetalGlossChannelSwap:
					return SetConversionMaterialSettings(_metalGlossChannelSwapMaterial, textureMapType);
				case TextureExportSettings.Conversion.MetalGlossOcclusionChannelSwap:
					return SetConversionMaterialSettings(_metalGlossOcclusionChannelSwapMaterial, textureMapType);
				default:
					return null;
			}
		}

		private static Material SetConversionMaterialSettings(Material material, TextureExportSettings textureMapType)
		{
			if (material && material.HasProperty("_SmoothnessRangeMin"))
				material.SetFloat("_SmoothnessRangeMin", textureMapType.smoothnessRangeMin);
			if (material && material.HasProperty("_SmoothnessRangeMax"))
				material.SetFloat("_SmoothnessRangeMax", textureMapType.smoothnessRangeMax);

			if (material && material.HasProperty("_MetallicRangeMin"))
				material.SetFloat("_MetallicRangeMin", textureMapType.metallicRangeMin);
			if (material && material.HasProperty("_MetallicRangeMax"))
				material.SetFloat("_MetallicRangeMax", textureMapType.metallicRangeMax);

			if (material && material.HasProperty("_OcclusionRangeMin"))
				material.SetFloat("_OcclusionRangeMin", textureMapType.occlusionRangeMin);
			if (material && material.HasProperty("_OcclusionRangeMax"))
				material.SetFloat("_OcclusionRangeMax", textureMapType.occlusionRangeMax);

			return material;
		}

		private struct ImageInfo
		{
			public Texture2D texture;
			public TextureExportSettings textureMapType;
			public string outputPath;
			public bool canBeExportedFromDisk;
		}

		private struct FileInfo
		{
			public Stream stream;
			public string uniqueFileName;
		}

		public struct ExportFileResult
		{
			public string uri;
			public string mimeType;
			public BufferViewId bufferView;
		}

		public IReadOnlyList<Transform> RootTransforms => _rootTransforms;

		private Transform[] _rootTransforms;
		private GLTFRoot _root;
		private BufferId _bufferId;
		private GLTFBuffer _buffer;
		private List<ImageInfo> _imageInfos;
		private HashSet<string> _imageExportPaths;
		private List<FileInfo> _fileInfos;
		private HashSet<string> _fileNames;
		private List<UniqueTexture> _textures;
		private Dictionary<int, int> _exportedMaterials;
		private bool _shouldUseInternalBufferForImages;
		private Dictionary<int, int> _exportedTransforms;
		private List<Transform> _animatedNodes;

		private int _exportLayerMask;
		private ExportContext _exportContext;

		private Material _metalGlossChannelSwapMaterial;
		private Material _metalGlossOcclusionChannelSwapMaterial;
		
		private Material _normalChannelMaterial;

		private const uint MagicGLTF = 0x46546C67;
		private const uint Version = 2;
		private const uint MagicJson = 0x4E4F534A;
		private const uint MagicBin = 0x004E4942;
		private const int GLTFHeaderSize = 12;
		private const int SectionHeaderSize = 8;

		private bool _visbilityPluginEnabled = false;
		
		public struct UniqueTexture : IEquatable<UniqueTexture>
		{
			public Texture Texture;
			public int MaxSize;
			// additional settings that make exporting a texture unique
			public TextureExportSettings ExportSettings;

			public int GetWidth() => Mathf.Min(MaxSize, Texture.width);
			public int GetHeight() => Mathf.Min(MaxSize, Texture.height);

			public UniqueTexture(Texture tex, string textureSlot, GLTFSceneExporter exporter)
			{
				Texture = tex;
				ExportSettings = exporter.GetExportSettingsForSlot(textureSlot);
				MaxSize = Mathf.Max(tex.width, tex.height);
			}

			public UniqueTexture(Texture tex, TextureExportSettings exportSettings)
			{
				Texture = tex;
				ExportSettings = exportSettings;
				MaxSize = Mathf.Max(tex.width, tex.height);
			}

			public bool Equals(UniqueTexture other)
			{
				return Equals(Texture, other.Texture) && MaxSize == other.MaxSize && ExportSettings == other.ExportSettings;
			}

			public override bool Equals(object obj)
			{
				return obj is UniqueTexture other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					// We dont want to use GetHashCode() for the texture here since it will change the hash after restarting the editor
					#if UNITY_EDITOR
					var hashCode = 0;
					if (Texture && Texture.imageContentsHash.isValid)
						hashCode = Texture.imageContentsHash.GetHashCode();
					else if (Texture)
						hashCode = Texture.GetHashCode();
					#else
					var hashCode = Texture ? Texture.GetHashCode() : 0;
					#endif
					hashCode = (hashCode * 397) ^ ExportSettings.GetHashCode();
					hashCode = (hashCode * 397) ^ MaxSize;
					return hashCode;
				}
			}
		}

		/// <summary>
		/// A Primitive is a combination of Mesh + Material(s). It also contains a reference to the original SkinnedMeshRenderer,
		/// if any, since that's the only way to get the actual current weights to export a blend shape primitive.
		/// </summary>
		public struct UniquePrimitive
		{
			public bool Equals(UniquePrimitive other)
			{
				if (!Equals(Mesh, other.Mesh)) return false;
				if (Materials == null && other.Materials == null) return true;
				if (!(Materials != null && other.Materials != null)) return false;
				if (!Equals(Materials.Length, other.Materials.Length)) return false;
				for (var i = 0; i < Materials.Length; i++)
				{
					if (!Equals(Materials[i], other.Materials[i])) return false;
				}

				return true;
			}

			public override bool Equals(object obj)
			{
				return obj is UniquePrimitive other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var code = (Mesh != null ? Mesh.GetHashCode() : 0) * 397;
					if (Materials != null)
					{
						code = code ^ Materials.Length.GetHashCode() * 397;
						foreach (var mat in Materials)
							code = (code ^ (mat != null ? mat.GetHashCode() : 0)) * 397;
					}

					return code;
				}
			}

			public Mesh Mesh;
			public Material[] Materials;
			public SkinnedMeshRenderer SkinnedMeshRenderer; // needed for BlendShape export, since Unity stores the actually used blend shape weights on the renderer. see ExporterMeshes.ExportBlendShapes
		}

		private readonly Dictionary<UniquePrimitive, MeshId> _primOwner = new Dictionary<UniquePrimitive, MeshId>();

		#region Settings

		private GLTFSettings settings => _exportContext.settings;
		private bool ExportNames => settings.ExportNames;
		private bool ExportAnimations => settings.ExportAnimations;

		#endregion

#region Profiler Markers
		// ReSharper disable InconsistentNaming
		private static ProfilerMarker exportGltfMarker = new ProfilerMarker("Export glTF");
		private static ProfilerMarker gltfSerializationMarker = new ProfilerMarker("Serialize exported data");
		private static ProfilerMarker exportMeshMarker = new ProfilerMarker("Export Mesh");
		private static ProfilerMarker exportPrimitiveMarker = new ProfilerMarker("Export Primitive");
		private static ProfilerMarker exportBlendShapeMarker = new ProfilerMarker("Export BlendShape");
		private static ProfilerMarker exportSkinFromNodeMarker = new ProfilerMarker("Export Skin");
		private static ProfilerMarker exportSparseAccessorMarker = new ProfilerMarker("Export Sparse Accessor");
		private static ProfilerMarker beforeNodeExportMarker = new ProfilerMarker("Before Node Export (Callback)");
		private static ProfilerMarker exportNodeMarker = new ProfilerMarker("Export Node");
		private static ProfilerMarker afterNodeExportMarker = new ProfilerMarker("After Node Export (Callback)");
		private static ProfilerMarker exportAnimationFromNodeMarker = new ProfilerMarker("Export Animation from Node");
		private static ProfilerMarker convertClipToGLTFAnimationMarker = new ProfilerMarker("Convert Clip to GLTF Animation");
		private static ProfilerMarker beforeSceneExportMarker = new ProfilerMarker("Before Scene Export (Callback)");
		private static ProfilerMarker exportSceneMarker = new ProfilerMarker("Export Scene");
		private static ProfilerMarker beforeMaterialExportMarker = new ProfilerMarker("Before Material Export (Callback)");
		private static ProfilerMarker exportMaterialMarker = new ProfilerMarker("Export Material");
		private static ProfilerMarker afterMaterialExportMarker = new ProfilerMarker("After Material Export (Callback)");
		private static ProfilerMarker writeImageToDiskMarker = new ProfilerMarker("Export Image - Write to Disk");
		private static ProfilerMarker afterSceneExportMarker = new ProfilerMarker("After Scene Export (Callback)");

		private static ProfilerMarker exportAccessorMarker = new ProfilerMarker("Export Accessor");
		private static ProfilerMarker exportAccessorMatrix4x4ArrayMarker = new ProfilerMarker("Matrix4x4[]");
		private static ProfilerMarker exportAccessorVector4ArrayMarker = new ProfilerMarker("Vector4[]");
		private static ProfilerMarker exportAccessorUintArrayMarker = new ProfilerMarker("Uint[]");
		private static ProfilerMarker exportAccessorColorArrayMarker = new ProfilerMarker("Color[]");
		private static ProfilerMarker exportAccessorVector3ArrayMarker = new ProfilerMarker("Vector3[]");
		private static ProfilerMarker exportAccessorVector2ArrayMarker = new ProfilerMarker("Vector2[]");
		private static ProfilerMarker exportAccessorIntArrayIndicesMarker = new ProfilerMarker("int[] (Indices)");
		private static ProfilerMarker exportAccessorIntArrayMarker = new ProfilerMarker("int[]");
		private static ProfilerMarker exportAccessorFloatArrayMarker = new ProfilerMarker("float[]");
		private static ProfilerMarker exportAccessorByteArrayMarker = new ProfilerMarker("byte[]");

		private static ProfilerMarker exportAccessorMinMaxMarker = new ProfilerMarker("Calculate min/max");
		private static ProfilerMarker exportAccessorBufferWriteMarker = new ProfilerMarker("Buffer.Write");

		private static ProfilerMarker exportGltfInitMarker = new ProfilerMarker("Init glTF Export");
		private static ProfilerMarker gltfWriteOutMarker = new ProfilerMarker("Write glTF");
		private static ProfilerMarker gltfWriteJsonStreamMarker = new ProfilerMarker("Write JSON stream");
		private static ProfilerMarker gltfWriteBinaryStreamMarker = new ProfilerMarker("Write binary stream");

		private static ProfilerMarker addAnimationDataMarker = new ProfilerMarker("Add animation data to glTF");
		private static ProfilerMarker exportRotationAnimationDataMarker = new ProfilerMarker("Rotation Keyframes");
		private static ProfilerMarker exportPositionAnimationDataMarker = new ProfilerMarker("Position Keyframes");
		private static ProfilerMarker exportScaleAnimationDataMarker = new ProfilerMarker("Scale Keyframes");
		private static ProfilerMarker exportWeightsAnimationDataMarker = new ProfilerMarker("Weights Keyframes");
		private static ProfilerMarker removeAnimationUnneededKeyframesMarker = new ProfilerMarker("Simplify Keyframes");
		private static ProfilerMarker removeAnimationUnneededKeyframesInitMarker = new ProfilerMarker("Init");
		private static ProfilerMarker removeAnimationUnneededKeyframesCheckIdenticalMarker = new ProfilerMarker("Check Identical");
		private static ProfilerMarker removeAnimationUnneededKeyframesCheckIdenticalKeepMarker = new ProfilerMarker("Keep Keyframe");
		private static ProfilerMarker removeAnimationUnneededKeyframesFinalizeMarker = new ProfilerMarker("Finalize");
		// ReSharper restore InconsistentNaming
#endregion

		/// <summary>
		/// Create a GLTFExporter that exports out a transform
		/// </summary>
		/// <param name="rootTransforms">Root transform of object to export</param>
		[Obsolete("Please switch to GLTFSceneExporter(Transform[] rootTransforms, ExportOptions options).  This constructor is deprecated and will be removed in a future release.")]
		public GLTFSceneExporter(Transform[] rootTransforms, RetrieveTexturePathDelegate texturePathRetriever)
			: this(rootTransforms, new ExportContext { TexturePathRetriever = texturePathRetriever })
		{
		}

		public GLTFSceneExporter(Transform rootTransform, ExportContext context) : this(new [] { rootTransform }, context)
		{
		}

		/// <summary>
		/// Create a GLTFExporter that exports out a transform
		/// </summary>
		/// <param name="rootTransforms">Root transform of object to export</param>
		/// <param name="context">Export Settings</param>
		public GLTFSceneExporter(Transform[] rootTransforms, ExportContext context)
		{
			_exportContext = context;
			if (context.logger != null)
				Debug = context.logger;
			else
				Debug = UnityEngine.Debug.unityLogger;
			
			// legacy: implicit plugin for all the static methods on GLTFSceneExporter
			_plugins.Add(new LegacyCallbacksPlugin());
			// legacy: implicit plugin for all the methods on ExportContext
			_plugins.Add(context.GetExportContextCallbacks());
			
			// create export plugin instances
			foreach (var plugin in settings.ExportPlugins)
			{
				if (plugin != null && plugin.Enabled)
				{
					var instance = plugin.CreateInstance(context);
					if (instance != null) _plugins.Add(instance);
				}
			}

			_exportLayerMask = _exportContext.ExportLayers;

			var metalGlossChannelSwapShader = Resources.Load("MetalGlossChannelSwap", typeof(Shader)) as Shader;
			_metalGlossChannelSwapMaterial = new Material(metalGlossChannelSwapShader);

			var metalGlossOcclusionChannelSwapShader = Resources.Load("MetalGlossOcclusionChannelSwap", typeof(Shader)) as Shader;
			_metalGlossOcclusionChannelSwapMaterial = new Material(metalGlossOcclusionChannelSwapShader);
			
			var normalChannelShader = Resources.Load("NormalChannel", typeof(Shader)) as Shader;
			_normalChannelMaterial = new Material(normalChannelShader);

			// Remove invalid transforms
			_rootTransforms = rootTransforms?.Where(x => x).ToArray() ?? Array.Empty<Transform>();

			_exportedTransforms = new Dictionary<int, int>();
			_exportedCameras = new Dictionary<int, int>();
			_exportedLights = new Dictionary<int, int>();
			_animatedNodes = new List<Transform>();
			_skinnedNodes = new List<Transform>();
			_bakedMeshes = new Dictionary<SkinnedMeshRenderer, UnityEngine.Mesh>();

			_root = new GLTFRoot
			{
				Accessors = new List<Accessor>(),
				Animations = new List<GLTFAnimation>(),
				Asset = new Asset
				{
					Version = "2.0",
					Generator = settings.Generator
				},
				Buffers = new List<GLTFBuffer>(),
				BufferViews = new List<BufferView>(),
				Cameras = new List<GLTFCamera>(),
				Images = new List<GLTFImage>(),
				Materials = new List<GLTFMaterial>(),
				Meshes = new List<GLTFMesh>(),
				Nodes = new List<Node>(),
				Samplers = new List<Sampler>(),
				Scenes = new List<GLTFScene>(),
				Skins = new List<Skin>(),
				Textures = new List<GLTFTexture>()
			};

			_imageInfos = new List<ImageInfo>();
			_fileInfos = new List<FileInfo>();
			_fileNames = new HashSet<string>();
			_exportedMaterials = new Dictionary<int, int>();
			_textures = new List<UniqueTexture>();
			_imageExportPaths = new HashSet<string>();

			_buffer = new GLTFBuffer();
			_bufferId = new BufferId
			{
				Id = _root.Buffers.Count,
				Root = _root
			};
			_root.Buffers.Add(_buffer);
			
			foreach (var plugin in settings.ExportPlugins)
			{
				if (plugin != null && plugin.Enabled && plugin.AssetExtras != null)
					_root.Asset.PluginExtras.Add(plugin.DisplayName, plugin.AssetExtras);
			}
			
			_visbilityPluginEnabled = settings.ExportPlugins.Any(x => x is VisibilityExport && x.Enabled);
			if (_visbilityPluginEnabled && !settings.ExportDisabledGameObjects)
			{
				Debug.Log(LogType.Warning,"KHR_node_visibility export plugin is enabled, but Export Disabled GameObjects is not. This may lead to unexpected results.");
			}
		}

		/// <summary>
		/// Gets the root object of the exported GLTF
		/// </summary>
		/// <returns>Root parsed GLTF Json</returns>
		public GLTFRoot GetRoot()
		{
			return _root;
		}

		/// <summary>
		/// Writes a binary GLB file with filename at path.
		/// </summary>
		/// <param name="path">File path for saving the binary file</param>
		/// <param name="fileName">The name of the GLTF file</param>
		public void SaveGLB(string path, string fileName)
		{
			var fullPath = GetFileName(path, fileName, ".glb");
			var dirName = Path.GetDirectoryName(fullPath);
			if (dirName != null && !Directory.Exists(dirName))
				Directory.CreateDirectory(dirName);
			_shouldUseInternalBufferForImages = true;

			using (FileStream glbFile = new FileStream(fullPath, FileMode.Create))
			{
				SaveGLBToStream(glbFile, fileName);
			}

			if (!_shouldUseInternalBufferForImages)
			{
				ExportImages(path);
				ExportFiles(path);
			}
		}

		/// <summary>
		/// In-memory GLB creation helper. Useful for platforms where no filesystem is available (e.g. WebGL).
		/// </summary>
		/// <param name="sceneName"></param>
		/// <returns></returns>
		public byte[] SaveGLBToByteArray(string sceneName)
		{
			_shouldUseInternalBufferForImages = true;
			using (var stream = new MemoryStream())
			{
				SaveGLBToStream(stream, sceneName);
				return stream.ToArray();
			}
		}

		/// <summary>
		/// Writes a binary GLB file into a stream (memory stream, filestream, ...)
		/// </summary>
		/// <param name="path">File path for saving the binary file</param>
		/// <param name="fileName">The name of the GLTF file</param>
		public void SaveGLBToStream(Stream stream, string sceneName)
		{
			exportGltfMarker.Begin();

			exportGltfInitMarker.Begin();
			Stream binStream = new MemoryStream();
			Stream jsonStream = new MemoryStream();
			_shouldUseInternalBufferForImages = true;

			_bufferWriter = new BinaryWriterWithLessAllocations(binStream);

			TextWriter jsonWriter = new StreamWriter(jsonStream, new UTF8Encoding(false));
			exportGltfInitMarker.End();

			beforeSceneExportMarker.Begin();
			foreach (var plugin in _plugins)
				plugin?.BeforeSceneExport(this, _root);
			beforeSceneExportMarker.End();

			_root.Scene = ExportScene(sceneName, _rootTransforms);

			if (ExportAnimations)
			{
				ExportAnimation();
			}

			// Export skins
			for (int i = 0; i < _skinnedNodes.Count; ++i)
			{
				Transform t = _skinnedNodes[i];
				ExportSkinFromNode(t);
			}

			afterSceneExportMarker.Begin();
			foreach (var plugin in _plugins)
				plugin?.AfterSceneExport(this, _root);
			afterSceneExportMarker.End();

			animationPointerResolver?.Resolve(this);

			_buffer.ByteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Length, 4);

			gltfSerializationMarker.Begin();
			_root.Serialize(jsonWriter, true);
			gltfSerializationMarker.End();

			gltfWriteOutMarker.Begin();
			_bufferWriter.Flush();
			jsonWriter.Flush();

			// align to 4-byte boundary to comply with spec.
			AlignToBoundary(jsonStream);
			AlignToBoundary(binStream, 0x00);

			int glbLength = (int)(GLTFHeaderSize + SectionHeaderSize +
				jsonStream.Length + SectionHeaderSize + binStream.Length);

			BinaryWriter writer = new BinaryWriter(stream);

			// write header
			writer.Write(MagicGLTF);
			writer.Write(Version);
			writer.Write(glbLength);

			gltfWriteJsonStreamMarker.Begin();
			// write JSON chunk header.
			writer.Write((int)jsonStream.Length);
			writer.Write(MagicJson);

			jsonStream.Position = 0;
			CopyStream(jsonStream, writer);
			gltfWriteJsonStreamMarker.End();

			gltfWriteBinaryStreamMarker.Begin();
			writer.Write((int)binStream.Length);
			writer.Write(MagicBin);

			binStream.Position = 0;
			CopyStream(binStream, writer);
			gltfWriteBinaryStreamMarker.End();

			writer.Flush();

			gltfWriteOutMarker.End();
			exportGltfMarker.End();
		}

		/// <summary>
		/// Specifies the path and filename for the GLTF Json and binary
		/// </summary>
		/// <param name="path">File path for saving the GLTF and binary files</param>
		/// <param name="fileName">The name of the GLTF file</param>
		public void SaveGLTFandBin(string path, string fileName, bool exportTextures = true)
		{
			exportGltfMarker.Begin();

			exportGltfInitMarker.Begin();
			_shouldUseInternalBufferForImages = false;
			var toLower = fileName.ToLowerInvariant();
			if (toLower.EndsWith(".gltf"))
				fileName = fileName.Substring(0, fileName.Length - 5);
			if (toLower.EndsWith(".bin"))
				fileName = fileName.Substring(0, fileName.Length - 4);
			var fullPath = GetFileName(path, fileName, ".bin");
			var dirName = Path.GetDirectoryName(fullPath);
			if (dirName != null && !Directory.Exists(dirName))
				Directory.CreateDirectory(dirName);

			// sanitized file path can differ
			fileName = Path.GetFileNameWithoutExtension(fullPath);
			var binFile = File.Create(fullPath);

			_bufferWriter = new BinaryWriterWithLessAllocations(binFile);
			exportGltfInitMarker.End();

			beforeSceneExportMarker.Begin();
			foreach (var plugin in _plugins)
				plugin?.BeforeSceneExport(this, _root);
			beforeSceneExportMarker.End();

			if (_rootTransforms != null)
				_root.Scene = ExportScene(fileName, _rootTransforms);

			if (ExportAnimations)
				ExportAnimation();

			// Export skins
			for (int i = 0; i < _skinnedNodes.Count; ++i)
			{
				Transform t = _skinnedNodes[i];
				ExportSkinFromNode(t);
			}

			afterSceneExportMarker.Begin();
			foreach (var plugin in _plugins)
				plugin?.AfterSceneExport(this, _root);
			afterSceneExportMarker.End();

			animationPointerResolver?.Resolve(this);

			// we don't need to create a .bin file if there's no buffer at all
			var anyDataInBinFile = _bufferWriter.BaseStream.Length > 0;
			if (anyDataInBinFile)
			{
				AlignToBoundary(_bufferWriter.BaseStream, 0x00);
				_buffer.Uri = fileName + ".bin";
				_buffer.ByteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Length, 4);
			}
			else
			{
				_buffer = null;
				_root.Buffers.Clear();
			}

			var gltfFile = File.CreateText(Path.ChangeExtension(fullPath, ".gltf"));
			gltfSerializationMarker.Begin();
			_root.Serialize(gltfFile);
			gltfSerializationMarker.End();

			gltfWriteOutMarker.Begin();

			_bufferWriter.Close();

#if WINDOWS_UWP
			gltfFile.Dispose();
			binFile.Dispose();
#else
			gltfFile.Close();
			binFile.Close();
#endif
			if (!anyDataInBinFile)
				File.Delete(fullPath);

			if (exportTextures)
				ExportImages(path);
				
			ExportFiles(path);

			gltfWriteOutMarker.End();
			exportGltfMarker.End();
		}

		/// <summary>
		/// Ensures a specific file extension from an absolute path that may or may not already have that extension.
		/// </summary>
		/// <param name="absolutePathThatMayHaveExtension">Absolute path that may or may not already have the required extension</param>
		/// <param name="requiredExtension">The extension to ensure, with leading dot</param>
		/// <returns>An absolute path that has the required extension</returns>
		public static string GetFileName(string directory, string fileNameThatMayHaveExtension, string requiredExtension)
		{
			var absolutePathThatMayHaveExtension = Path.Combine(directory, EnsureValidFileName(fileNameThatMayHaveExtension));

			if (!requiredExtension.StartsWith(".", StringComparison.Ordinal))
				requiredExtension = "." + requiredExtension;

			if (!Path.GetExtension(absolutePathThatMayHaveExtension).Equals(requiredExtension, StringComparison.OrdinalIgnoreCase))
				return absolutePathThatMayHaveExtension + requiredExtension;

			return absolutePathThatMayHaveExtension;
		}

		/// <summary>
		/// Strip illegal chars and reserved words from a candidate filename (should not include the directory path)
		/// </summary>
		/// <remarks>
		/// http://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name
		/// </remarks>
		private static string EnsureValidFileName(string filename)
		{
			if (filename == null) return "";
			
			var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
			var invalidReStr = string.Format(@"[{0}]+", invalidChars);

			var reservedWords = new []
			{
				"CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
				"COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
				"LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
			};

			var sanitisedNamePart = Regex.Replace(filename, invalidReStr, "_");
			foreach (var reservedWord in reservedWords)
			{
				var reservedWordPattern = string.Format("^{0}\\.", reservedWord);
				sanitisedNamePart = Regex.Replace(sanitisedNamePart, reservedWordPattern, "_reservedWord_.", RegexOptions.IgnoreCase);
			}

			return sanitisedNamePart;
		}

		public void DeclareExtensionUsage(string extension, bool isRequired=false)
		{
			if( _root.ExtensionsUsed == null ){
				_root.ExtensionsUsed = new List<string>();
			}
			if(!_root.ExtensionsUsed.Contains(extension))
			{
				_root.ExtensionsUsed.Add(extension);
			}

			if(isRequired){

				if( _root.ExtensionsRequired == null ){
					_root.ExtensionsRequired = new List<string>();
				}
				if( !_root.ExtensionsRequired.Contains(extension))
				{
					_root.ExtensionsRequired.Add(extension);
				}
			}
		}

		private bool ShouldExportTransform(Transform transform)
		{
			// Root transforms should *always* be exported since this is a deliberate decision by the user calling - it should override any other setting that would prevent the export (e.g. if a user calls Export with disabled or hidden objects the exporter should never prevent this)
			var isRoot = _rootTransforms.Contains(transform);
			if (isRoot) return true;
			
			if (!settings.ExportDisabledGameObjects && !transform.gameObject.activeSelf)
			{
				return false;
			}
			
			if (settings.UseMainCameraVisibility && (_exportLayerMask >= 0 && _exportLayerMask != (_exportLayerMask | 1 << transform.gameObject.layer))) return false;
			if (transform.CompareTag("EditorOnly")) return false;
			return true;
		}

		private SceneId ExportScene(string name, Transform[] rootObjTransforms)
		{
			if (rootObjTransforms == null || rootObjTransforms.Length < 1) return null;
			
			exportSceneMarker.Begin();

			var scene = new GLTFScene();

			if (ExportNames)
			{
				scene.Name = name;
			}

			if(_exportContext.TreatEmptyRootAsScene)
			{
				// if we're exporting with a single object selected, that object can be the scene root, no need for an extra root node.
				if (rootObjTransforms.Length == 1 && rootObjTransforms[0].GetComponents<Component>().Length == 1) // single root with a single transform
				{
					var firstRoot = rootObjTransforms[0];
					var newRoots = new Transform[firstRoot.childCount];
					for (int i = 0; i < firstRoot.childCount; i++)
						newRoots[i] = firstRoot.GetChild(i);
					rootObjTransforms = newRoots;
				}
			}

			scene.Nodes = new List<NodeId>(rootObjTransforms.Length);
			foreach (var transform in rootObjTransforms)
			{
				if (!transform)
				{
					Debug.LogWarning("GLTFSceneExporter", $"Skipping empty transform in root transforms provided for scene export for {name}", transform);
					continue;
				}
				scene.Nodes.Add(ExportNode(transform));
			}

			_root.Scenes.Add(scene);

			exportSceneMarker.End();

			return new SceneId
			{
				Id = _root.Scenes.Count - 1,
				Root = _root
			};
		}

		private NodeId ExportNode(Transform nodeTransform)
		{
			if (_exportedTransforms.TryGetValue(nodeTransform.GetInstanceID(), out var existingNodeId))
				return new NodeId() { Id = existingNodeId, Root = _root };

			foreach (var plugin in _plugins)
				if (!(plugin?.ShouldNodeExport(this, _root, nodeTransform) ?? true)) return null;

			exportNodeMarker.Begin();
			
			var node = new Node();

			if (_visbilityPluginEnabled && !nodeTransform.gameObject.activeSelf)
			{
				DeclareExtensionUsage(KHR_node_visibility_Factory.EXTENSION_NAME, false);
				node.AddExtension(KHR_node_visibility_Factory.EXTENSION_NAME, new KHR_node_visibility { visible = false });
			}
			
			if (ExportNames)
			{
				node.Name = nodeTransform.name;
			}
			
			// TODO think more about how this callback is used – could e.g. be modifying the hierarchy,
			// and we would want to prevent exporting children of this node.
			// Could also be that we want to add a mesh based on some condition
			// (e.g. merged childs, procedural geometry, etc.)
			beforeNodeExportMarker.Begin();
			foreach (var plugin in _plugins)
				plugin?.BeforeNodeExport(this, _root, nodeTransform, node);
			beforeNodeExportMarker.End();

#if ANIMATION_SUPPORTED
			if (nodeTransform.GetComponent<Animation>() || nodeTransform.GetComponent<Animator>())
			{
				_animatedNodes.Add(nodeTransform);
			}
#endif
			if (nodeTransform.GetComponent<SkinnedMeshRenderer>() && ContainsValidRenderer(nodeTransform.gameObject, settings.ExportDisabledGameObjects))
			{
				_skinnedNodes.Add(nodeTransform);
			}

			// export camera attached to node
			Camera unityCamera = nodeTransform.GetComponent<Camera>();
			if (unityCamera != null && unityCamera.enabled)
			{
				node.Camera = ExportCamera(unityCamera);
			}

			var lightPluginEnabled = _plugins.FirstOrDefault(x => x is LightsPunctualExportContext) != null;
			Light unityLight = nodeTransform.GetComponent<Light>();
			if (unityLight != null && unityLight.enabled && lightPluginEnabled)
			{
				node.Light = ExportLight(unityLight);
			}

			var needsInvertedLookDirection = unityLight || unityCamera;
            if (needsInvertedLookDirection)
            {
                node.SetUnityTransform(nodeTransform, true);
            }
            else
            {
                node.SetUnityTransform(nodeTransform, false);
            }

            var id = new NodeId
			{
				Id = _root.Nodes.Count,
				Root = _root
			};

			// Register nodes for animation parsing (could be disabled if animation is disabled)
			_exportedTransforms.Add(nodeTransform.GetInstanceID(), _root.Nodes.Count);

			_root.Nodes.Add(node);

			// children that are primitives get put in a mesh
			FilterPrimitives(nodeTransform, out GameObject[] primitives, out GameObject[] nonPrimitives);
			if (primitives.Length > 0)
			{
				var uniquePrimitives = GetUniquePrimitivesFromGameObjects(primitives);
				if (uniquePrimitives != null)
				{
					node.Mesh = ExportMesh(nodeTransform.name, uniquePrimitives);
					RegisterPrimitivesWithNode(node, uniquePrimitives);

					// Node - BlendShape Weights 
					if (uniquePrimitives[0].SkinnedMeshRenderer)
					{
						var meshObj = uniquePrimitives[0].Mesh;
						var smr = uniquePrimitives[0].SkinnedMeshRenderer;
						// Only export the blendShapeWeights into the Node, when it's not the first SkinnedMeshRenderer with the same Mesh
						// Because the weights already exported into the GltfMesh
						if (smr && meshObj && _meshToBlendShapeAccessors.TryGetValue(meshObj, out var data) && smr != data.firstSkinnedMeshRenderer)
						{
							var blendShapeWeights = GetBlendShapeWeights(smr, meshObj);
							if (blendShapeWeights != null)
							{
								if (data.weights != null)
								{
									// Check if the blendShapeWeights has any differences to the weights already exported into the gltfMesh
									// When not, we don't need to set the same values to the Node Weights
									bool isSame = true;
									for (int i = 0; i < blendShapeWeights.Count; i++)
										isSame &= System.Math.Abs(blendShapeWeights[i] - data.weights[i]) < double.Epsilon;
									if (!isSame)
										node.Weights = blendShapeWeights;
								}
							}
						}
					}
				}
			}

			exportNodeMarker.End();

			// children that are not primitives get added as child nodes
			if (nonPrimitives.Length > 0)
			{
				var parentOfChilds = node;

				// when we're exporting a light or camera, we add an implicit node as first child of the camera/light node.
				// this ensures that child objects and animations etc. "just work".
				if (needsInvertedLookDirection)
				{
					var inbetween = new Node();

					if (ExportNames)
					{
						inbetween.Name = nodeTransform.name + "-flipped";
					}

					inbetween.Rotation = Quaternion.Inverse(SchemaExtensions.InvertDirection).ToGltfQuaternionConvert();

					var inbetweenId = new NodeId
					{
						Id = _root.Nodes.Count,
						Root = _root
					};

					_root.Nodes.Add(inbetween);

					node.Children = new List<NodeId>(1);
					node.Children.Add(inbetweenId);

					parentOfChilds = inbetween;
				}

				parentOfChilds.Children = new List<NodeId>(nonPrimitives.Length);
				foreach (var child in nonPrimitives)
				{
					if (!ShouldExportTransform(child.transform)) continue;
					var childNode = ExportNode(child.transform);
					if (childNode != null) parentOfChilds.Children.Add(childNode);
				}
			}

			// node export callback
			afterNodeExportMarker.Begin();
			foreach (var plugin in _plugins)
				plugin?.AfterNodeExport(this, _root, nodeTransform, node);
			afterNodeExportMarker.End();

			return id;
		}

		private static bool ContainsValidRenderer(GameObject gameObject, bool exportDisabledGameObjects)
		{
			if (!gameObject) return false;
			var meshRenderer = gameObject.GetComponent<MeshRenderer>();
			var meshFilter = gameObject.GetComponent<MeshFilter>();
			var skinnedMeshRender = gameObject.GetComponent<SkinnedMeshRenderer>();
			var materials = meshRenderer ? meshRenderer.sharedMaterials : skinnedMeshRender ? skinnedMeshRender.sharedMaterials : null;
			var anyMaterialIsNonNull = false;
			if (materials != null)
				for (int i = 0; i < materials.Length; i++)
					anyMaterialIsNonNull |= materials[i];
			return ((meshFilter && meshRenderer && (meshRenderer.enabled || exportDisabledGameObjects)) || (skinnedMeshRender && (skinnedMeshRender.enabled || exportDisabledGameObjects))) && anyMaterialIsNonNull;
		}

        private void FilterPrimitives(Transform transform, out GameObject[] primitives, out GameObject[] nonPrimitives)
		{
			var childCount = transform.childCount;
			var prims = new List<GameObject>(childCount + 1);
			var nonPrims = new List<GameObject>(childCount);

			// add another primitive if the root object also has a mesh
			if (ShouldExportTransform(transform))
			{
				if (ContainsValidRenderer(transform.gameObject, settings.ExportDisabledGameObjects))
				{
					prims.Add(transform.gameObject);
				}
			}
			
			for (var i = 0; i < childCount; i++)
			{
				var go = transform.GetChild(i).gameObject;

				// This seems to be a performance optimization but results in transforms that are detected as "primitives" not being animated
				// if (IsPrimitive(go))
				// 	 prims.Add(go);
				// else
				nonPrims.Add(go);
			}

			primitives = prims.ToArray();
			nonPrimitives = nonPrims.ToArray();
		}

        // This seems to be a performance optimization but results in transforms that are detected as "primitives" not being animated
		// private static bool IsPrimitive(GameObject gameObject)
		// {
		// 	/*
		// 	 * Primitives have the following properties:
		// 	 * - have no children
		// 	 * - have no non-default local transform properties
		// 	 * - have MeshFilter and MeshRenderer components OR has SkinnedMeshRenderer component
		// 	 */
		// 	return gameObject.transform.childCount == 0
		// 		&& gameObject.transform.localPosition == Vector3.zero
		// 		&& gameObject.transform.localRotation == Quaternion.identity
		// 		&& gameObject.transform.localScale == Vector3.one
		// 		&& ContainsValidRenderer(gameObject);
		// }

		public ExportFileResult ExportFile(string fileName, string mimeType, Stream stream) {
			if (_shouldUseInternalBufferForImages) {
				byte[] data = new byte[stream.Length];
				stream.Read(data, 0, (int)stream.Length);
				stream.Close();

				return new ExportFileResult {
					bufferView = this.ExportBufferView(data),
					mimeType = mimeType,
				};
			} else {
				var uniqueFileName = GetUniqueName(_fileNames, fileName);

				_fileNames.Add(uniqueFileName);

				_fileInfos.Add(
					new FileInfo {
						stream = stream,
						uniqueFileName = uniqueFileName,
					}
				);

				return new ExportFileResult {
					uri = uniqueFileName,
				};
			}
		}

		private void ExportFiles(string outputPath)
		{
			for (int i = 0; i < _fileInfos.Count; ++i)
			{
				var fileInfo = _fileInfos[i];

				var fileOutputPath = Path.Combine(outputPath, fileInfo.uniqueFileName);

				var dir = Path.GetDirectoryName(fileOutputPath);
				if (!Directory.Exists(dir) && dir != null)
					Directory.CreateDirectory(dir);

				var outputStream = File.Create(fileOutputPath);
				fileInfo.stream.Seek(0, SeekOrigin.Begin);
				fileInfo.stream.CopyTo(outputStream);
				outputStream.Close();
			}
		}

		private void ExportAnimation()
		{
			for (int i = 0; i < _animatedNodes.Count; ++i)
			{
				Transform t = _animatedNodes[i];
				ExportAnimationFromNode(ref t);
			}
		}

#region Public API
#if ANIMATION_SUPPORTED

		public int GetAnimationId(AnimationClip clip, Transform transform, float speed = 1)
		{
			if (_clipAndSpeedAndNodeToAnimation.TryGetValue((clip, speed, transform), out var id))
			{
				return _root.Animations.IndexOf(id);
			}
			return -1;
		}
#endif

		public MaterialId GetMaterialId(GLTFRoot root, Material materialObj)
		{
			var materialKey = 0;
			if (materialObj == DefaultMaterial)
				materialKey = 0;
			else if (materialObj)
				materialKey = materialObj.GetInstanceID();

			if (_exportedMaterials.TryGetValue(materialKey, out var id))
			{
				return new MaterialId
				{
					Id = id,
					Root = root
				};
			}

			return null;
		}

		public TextureId GetTextureId(GLTFRoot root, Texture textureObj)
		{
			for (var i = 0; i < _textures.Count; i++)
			{
				if (_textures[i].Texture == textureObj)
				{
					return new TextureId
					{
						Id = i,
						Root = root
					};
				}
			}
			return null;
		}

		public TextureId GetTextureId(GLTFRoot root, UniqueTexture textureObj)
		{
			for (var i = 0; i < _textures.Count; i++)
			{
				if (_textures[i].Equals(textureObj))
				{
					return new TextureId
					{
						Id = i,
						Root = root
					};
				}
			}
			return null;
		}
		
		public MeshId GetMeshId(Mesh meshObj)
		{
			foreach (var primOwner in _primOwner)
			{
				// Not sure if this is entirely accurate – we're returning the first instance here.
				if (primOwner.Key.Mesh == meshObj)
				{
					return primOwner.Value;
				}
			}

			return null;
		}

		public ImageId GetImageId(GLTFRoot root, Texture imageObj, TextureExportSettings textureMapType)
		{
			for (var i = 0; i < _imageInfos.Count; i++)
			{
				if (_imageInfos[i].texture == imageObj && _imageInfos[i].textureMapType == textureMapType)
				{
					return new ImageId
					{
						Id = i,
						Root = root
					};
				}
			}

			return null;
		}

		public SamplerId GetSamplerId(GLTFRoot root, Texture textureObj)
		{
			if (_textureSettingsToSamplerIndices.TryGetValue(new SamplerRelevantTextureData(textureObj), out var samplerId))
			{
				return new SamplerId
				{
					Id = samplerId,
					Root = root
				};
			}

			return null;
		}

		public Texture GetTexture(int id) => _textures[id].Texture;

		#endregion
	}
}
