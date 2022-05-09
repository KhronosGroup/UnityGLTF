#if UNITY_EDITOR
#define ANIMATION_EXPORT_SUPPORTED
#endif

#if ANIMATION_EXPORT_SUPPORTED && (UNITY_ANIMATION || !UNITY_2019_1_OR_NEWER)
#define ANIMATION_SUPPORTED
#endif

#define USE_FAST_BINARY_WRITER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using GLTF;
using GLTF.Schema;
using GLTF.Schema.KHR_lights_punctual;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Extensions;
using UnityGLTF.JsonPointer;
using CameraType = GLTF.Schema.CameraType;
using LightType = UnityEngine.LightType;
using Object = UnityEngine.Object;
using WrapMode = GLTF.Schema.WrapMode;

#if ANIMATION_EXPORT_SUPPORTED
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace UnityGLTF
{
	public class ExportOptions
	{
		public bool ExportInactivePrimitives = true;
		public bool TreatEmptyRootAsScene = false;
		public bool MergeClipsWithMatchingNames = false;
		public LayerMask ExportLayers = -1;
		public ILogger logger;

		public ExportOptions()
		{
			var settings = GLTFSettings.GetOrCreateSettings();
			if (settings.UseMainCameraVisibility)
				ExportLayers = Camera.main ? Camera.main.cullingMask : -1;
		}

		public GLTFSceneExporter.RetrieveTexturePathDelegate TexturePathRetriever = (texture) => texture.name;
		public GLTFSceneExporter.AfterSceneExportDelegate AfterSceneExport;
		public GLTFSceneExporter.BeforeSceneExportDelegate BeforeSceneExport;
		public GLTFSceneExporter.AfterNodeExportDelegate AfterNodeExport;
		public GLTFSceneExporter.BeforeMaterialExportDelegate BeforeMaterialExport;
		public GLTFSceneExporter.AfterMaterialExportDelegate AfterMaterialExport;
	}

	public class GLTFSceneExporter
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

		// Static callbacks
		public static event BeforeSceneExportDelegate BeforeSceneExport;
		public static event AfterSceneExportDelegate AfterSceneExport;
		public static event AfterNodeExportDelegate AfterNodeExport;
		/// <returns>True: material export is complete. False: continue regular export.</returns>
		public static event BeforeMaterialExportDelegate BeforeMaterialExport;
		public static event AfterMaterialExportDelegate AfterMaterialExport;

		private static ILogger Debug = UnityEngine.Debug.unityLogger;

		internal readonly List<IJsonPointerResolver> pointerResolvers = new List<IJsonPointerResolver>();
		public void RegisterResolver(IJsonPointerResolver resolver)
		{
			if(!this.pointerResolvers.Contains(resolver))
				this.pointerResolvers.Add(resolver);
		}

		private enum IMAGETYPE
		{
			RGB,
			RGBA,
			R,
			G,
			B,
			A,
			G_INVERT
		}

		public enum TextureMapType
		{
			Main,
			Bump,
			SpecGloss,
			Emission,
			MetallicGloss,
			Light,
			Occlusion,
			MetallicGloss_DontConvert,
			Custom_Unknown
		}

		private struct ImageInfo
		{
			public Texture2D texture;
			public TextureMapType textureMapType;
			public string outputPath;
			public bool canBeExportedFromDisk;
		}

		public IReadOnlyList<Transform> RootTransforms => _rootTransforms;

		private Transform[] _rootTransforms;
		private GLTFRoot _root;
		private BufferId _bufferId;
		private GLTFBuffer _buffer;
#if USE_FAST_BINARY_WRITER
		private BinaryWriterWithLessAllocations _bufferWriter;
#else
		private BinaryWriter _bufferWriter;
#endif
		private List<ImageInfo> _imageInfos;
		private List<Texture> _textures;
		private Dictionary<Material, int> _materials;
		private List<AnimationClip> _animationClips;
		private bool _shouldUseInternalBufferForImages;
		private Dictionary<int, int> _exportedTransforms;
		private List<Transform> _animatedNodes;
		private List<Transform> _skinnedNodes;
		private Dictionary<SkinnedMeshRenderer, UnityEngine.Mesh> _bakedMeshes;

		private int _exportLayerMask;
		private ExportOptions _exportOptions;

		private KHR_animation_pointer_Resolver animationPointerResolver = new KHR_animation_pointer_Resolver();

		private Material _metalGlossChannelSwapMaterial;
		private Material _normalChannelMaterial;

		private const uint MagicGLTF = 0x46546C67;
		private const uint Version = 2;
		private const uint MagicJson = 0x4E4F534A;
		private const uint MagicBin = 0x004E4942;
		private const int GLTFHeaderSize = 12;
		private const int SectionHeaderSize = 8;

		protected struct PrimKey
		{
			public bool Equals(PrimKey other)
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
				return obj is PrimKey other && Equals(other);
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
		}

		private struct MeshAccessors
		{
			public AccessorId aPosition, aNormal, aTangent, aTexcoord0, aTexcoord1, aColor0;
			public Dictionary<int, MeshPrimitive> subMeshPrimitives;
		}

		private struct BlendShapeAccessors
		{
			public List<Dictionary<string, AccessorId>> targets;
			public List<Double> weights;
			public List<string> targetNames;
		}

		private readonly Dictionary<PrimKey, MeshId> _primOwner = new Dictionary<PrimKey, MeshId>();
		private readonly Dictionary<Mesh, MeshAccessors> _meshToPrims = new Dictionary<Mesh, MeshAccessors>();
		private readonly Dictionary<Mesh, BlendShapeAccessors> _meshToBlendShapeAccessors = new Dictionary<Mesh, BlendShapeAccessors>();
#if ANIMATION_EXPORT_SUPPORTED
		private readonly Dictionary<(AnimationClip clip, float speed), GLTFAnimation> _clipToAnimation = new Dictionary<(AnimationClip, float), GLTFAnimation>();
#endif
#if ANIMATION_SUPPORTED
		private readonly Dictionary<(AnimationClip clip, float speed, string targetPath), Transform> _clipAndSpeedAndPathToExportedTransform = new Dictionary<(AnimationClip, float, string), Transform>();
#endif

		#region Settings

		// Settings
		static GLTFSettings settings => GLTFSettings.GetOrCreateSettings();

		public static bool ExportNames {
			get { return settings.ExportNames; }
			set { settings.ExportNames = value; }
		}
		public static bool ExportFullPath
		{
			get { return settings.ExportFullPath; }
			set { settings.ExportFullPath = value; }
		}
		public static bool RequireExtensions
		{
			get { return settings.RequireExtensions; }
			set { settings.RequireExtensions = value; }
		}
		public static bool TryExportTexturesFromDisk
		{
			get { return settings.TryExportTexturesFromDisk; }
			set { settings.TryExportTexturesFromDisk = value; }
		}
		public static bool ExportAnimations
		{
			get { return settings.ExportAnimations; }
			set { settings.ExportAnimations = value; }
		}
		public static bool UniqueAnimationNames
		{
			get { return settings.UniqueAnimationNames; }
			set { settings.UniqueAnimationNames = value; }
		}
		public static bool BakeSkinnedMeshes
		{
			get { return settings.BakeSkinnedMeshes; }
			set { settings.BakeSkinnedMeshes = value; }
		}
		public static bool ExportDisabledGameObjects
		{
			get { return settings.ExportDisabledGameObjects; }
			set { settings.ExportDisabledGameObjects = value; }
		}

#if UNITY_EDITOR
		public static string SaveFolderPath
		{
			get { return settings.SaveFolderPath; }
			set { settings.SaveFolderPath = value; }
		}
#endif

#if ANIMATION_SUPPORTED
		private static int AnimationBakingFramerate = 30; // FPS
		private static bool BakeAnimationData = true;
#endif

		#endregion

		private static ProfilerMarker exportGltfMarker = new ProfilerMarker("Export glTF");
		private static ProfilerMarker gltfSerializationMarker = new ProfilerMarker("Serialize exported data");
		private static ProfilerMarker exportMeshMarker = new ProfilerMarker("Export Mesh");
		private static ProfilerMarker exportPrimitiveMarker = new ProfilerMarker("Export Primitive");
		private static ProfilerMarker exportBlendShapeMarker = new ProfilerMarker("Export BlendShape");
		private static ProfilerMarker exportSkinFromNodeMarker = new ProfilerMarker("Export Skin");
		private static ProfilerMarker exportSparseAccessorMarker = new ProfilerMarker("Export Sparse Accessor");
		private static ProfilerMarker exportNodeMarker = new ProfilerMarker("Export Node");
		private static ProfilerMarker afterNodeExportMarker = new ProfilerMarker("After Node Export (Callback)");
		private static ProfilerMarker exportAnimationFromNodeMarker = new ProfilerMarker("Export Animation from Node");
		private static ProfilerMarker convertClipToGLTFAnimationMarker = new ProfilerMarker("Convert Clip to GLTF Animation");
		private static ProfilerMarker beforeSceneExportMarker = new ProfilerMarker("Before Scene Export (Callback)");
		private static ProfilerMarker exportSceneMarker = new ProfilerMarker("Export Scene");
		private static ProfilerMarker afterMaterialExportMarker = new ProfilerMarker("After Material Export (Callback)");
		private static ProfilerMarker exportMaterialMarker = new ProfilerMarker("Export Material");
		private static ProfilerMarker beforeMaterialExportMarker = new ProfilerMarker("Before Material Export (Callback)");
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

		/// <summary>
		/// Create a GLTFExporter that exports out a transform
		/// </summary>
		/// <param name="rootTransforms">Root transform of object to export</param>
		[Obsolete("Please switch to GLTFSceneExporter(Transform[] rootTransforms, ExportOptions options).  This constructor is deprecated and will be removed in a future release.")]
		public GLTFSceneExporter(Transform[] rootTransforms, RetrieveTexturePathDelegate texturePathRetriever)
			: this(rootTransforms, new ExportOptions { TexturePathRetriever = texturePathRetriever })
		{
		}

		/// <summary>
		/// Create a GLTFExporter that exports out a transform
		/// </summary>
		/// <param name="rootTransforms">Root transform of object to export</param>
		/// <param name="options">Export Settings</param>
		public GLTFSceneExporter(Transform[] rootTransforms, ExportOptions options)
		{
			_exportOptions = options;
			if (options.logger != null)
				Debug = options.logger;

			_exportLayerMask = _exportOptions.ExportLayers;

			var metalGlossChannelSwapShader = Resources.Load("MetalGlossChannelSwap", typeof(Shader)) as Shader;
			_metalGlossChannelSwapMaterial = new Material(metalGlossChannelSwapShader);

			var normalChannelShader = Resources.Load("NormalChannel", typeof(Shader)) as Shader;
			_normalChannelMaterial = new Material(normalChannelShader);

			_rootTransforms = rootTransforms;

			_exportedTransforms = new Dictionary<int, int>();
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
					Generator = "UnityGLTF"
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
			_materials = new Dictionary<Material, int>();
			_textures = new List<Texture>();
			_animationClips = new List<AnimationClip>();

			_buffer = new GLTFBuffer();
			_bufferId = new BufferId
			{
				Id = _root.Buffers.Count,
				Root = _root
			};
			_root.Buffers.Add(_buffer);
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

			TextWriter jsonWriter = new StreamWriter(jsonStream, Encoding.ASCII);
			exportGltfInitMarker.End();

			beforeSceneExportMarker.Begin();
			_exportOptions.BeforeSceneExport?.Invoke(this, _root);
			BeforeSceneExport?.Invoke(this, _root);
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
			if (_exportOptions.AfterSceneExport != null)
				_exportOptions.AfterSceneExport(this, _root);

			if (AfterSceneExport != null)
				AfterSceneExport.Invoke(this, _root);
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
		/// Convenience function to copy from a stream to a binary writer, for
		/// compatibility with pre-.NET 4.0.
		/// Note: Does not set position/seek in either stream. After executing,
		/// the input buffer's position should be the end of the stream.
		/// </summary>
		/// <param name="input">Stream to copy from</param>
		/// <param name="output">Stream to copy to.</param>
		private static void CopyStream(Stream input, BinaryWriter output)
		{
			byte[] buffer = new byte[8 * 1024];
			int length;
			while ((length = input.Read(buffer, 0, buffer.Length)) > 0)
			{
				output.Write(buffer, 0, length);
			}
		}

		/// <summary>
		/// Pads a stream with additional bytes.
		/// </summary>
		/// <param name="stream">The stream to be modified.</param>
		/// <param name="pad">The padding byte to append. Defaults to ASCII
		/// space (' ').</param>
		/// <param name="boundary">The boundary to align with, in bytes.
		/// </param>
		private static void AlignToBoundary(Stream stream, byte pad = (byte)' ', uint boundary = 4)
		{
			uint currentLength = (uint)stream.Length;
			uint newLength = CalculateAlignment(currentLength, boundary);
			for (int i = 0; i < newLength - currentLength; i++)
			{
				stream.WriteByte(pad);
			}
		}

		/// <summary>
		/// Calculates the number of bytes of padding required to align the
		/// size of a buffer with some multiple of byteAllignment.
		/// </summary>
		/// <param name="currentSize">The current size of the buffer.</param>
		/// <param name="byteAlignment">The number of bytes to align with.</param>
		/// <returns></returns>
		public static uint CalculateAlignment(uint currentSize, uint byteAlignment)
		{
			return (currentSize + byteAlignment - 1) / byteAlignment * byteAlignment;
		}


		/// <summary>
		/// Specifies the path and filename for the GLTF Json and binary
		/// </summary>
		/// <param name="path">File path for saving the GLTF and binary files</param>
		/// <param name="fileName">The name of the GLTF file</param>
		public void SaveGLTFandBin(string path, string fileName)
		{
			exportGltfMarker.Begin();

			exportGltfInitMarker.Begin();
			_shouldUseInternalBufferForImages = false;
			var fullPath = GetFileName(path, fileName, ".bin");
			var dirName = Path.GetDirectoryName(fullPath);
			if (dirName != null && !Directory.Exists(dirName))
				Directory.CreateDirectory(dirName);

			var binFile = File.Create(fullPath);
			_bufferWriter = new BinaryWriterWithLessAllocations(binFile);
			exportGltfInitMarker.End();

			_root.Scene = ExportScene(fileName, _rootTransforms);

			if (ExportAnimations)
			{
				ExportAnimation();
			}

			// Export skins
			for (int i = 0; i < _skinnedNodes.Count; ++i)
			{
				Transform t = _skinnedNodes[i];
				ExportSkinFromNode(t);

				// updateProgress(EXPORT_STEP.SKINNING, i, _skinnedNodes.Count);
			}

			afterSceneExportMarker.Begin();
			if (_exportOptions.AfterSceneExport != null)
				_exportOptions.AfterSceneExport(this, _root);

			if (AfterSceneExport != null)
				AfterSceneExport.Invoke(this, _root);
			afterSceneExportMarker.End();

			animationPointerResolver?.Resolve(this);

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			_buffer.Uri = fileName + ".bin";
			_buffer.ByteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Length, 4);

			var gltfFile = File.CreateText(Path.ChangeExtension(fullPath, ".gltf"));
			gltfSerializationMarker.Begin();
			_root.Serialize(gltfFile);
			gltfSerializationMarker.End();

			gltfWriteOutMarker.Begin();
#if WINDOWS_UWP
			gltfFile.Dispose();
			binFile.Dispose();
#else
			gltfFile.Close();
			binFile.Close();
#endif
			ExportImages(path);
			gltfWriteOutMarker.End();

			exportGltfMarker.End();
		}

		/// <summary>
		/// Strip illegal chars and reserved words from a candidate filename (should not include the directory path)
		/// </summary>
		/// <remarks>
		/// http://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name
		/// </remarks>
		private static string EnsureValidFileName(string filename)
		{
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

		/// <summary>
		/// Ensures a specific file extension from an absolute path that may or may not already have that extension.
		/// </summary>
		/// <param name="absolutePathThatMayHaveExtension">Absolute path that may or may not already have the required extension</param>
		/// <param name="requiredExtension">The extension to ensure, with leading dot</param>
		/// <returns>An absolute path that has the required extension</returns>
		private static string GetFileName(string directory, string fileNameThatMayHaveExtension, string requiredExtension)
		{
			var absolutePathThatMayHaveExtension = Path.Combine(directory, EnsureValidFileName(fileNameThatMayHaveExtension));

			if (!requiredExtension.StartsWith(".", StringComparison.Ordinal))
				requiredExtension = "." + requiredExtension;

			if (!Path.GetExtension(absolutePathThatMayHaveExtension).Equals(requiredExtension, StringComparison.OrdinalIgnoreCase))
				return absolutePathThatMayHaveExtension + requiredExtension;

			return absolutePathThatMayHaveExtension;
		}


		private void ExportImages(string outputPath)
		{
			for (int t = 0; t < _imageInfos.Count; ++t)
			{
				writeImageToDiskMarker.Begin();

				var image = _imageInfos[t].texture;
				var textureMapType = _imageInfos[t].textureMapType;
				var fileOutputPath = Path.Combine(outputPath, _imageInfos[t].outputPath);
				var canBeExportedFromDisk = _imageInfos[t].canBeExportedFromDisk;

				var dir = Path.GetDirectoryName(fileOutputPath);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				bool wasAbleToExportTexture = false;
				if (canBeExportedFromDisk)
				{
					File.WriteAllBytes(fileOutputPath, GetTextureDataFromDisk(image));
				}

				if(!wasAbleToExportTexture)
				{
					switch (textureMapType)
					{
						case TextureMapType.MetallicGloss:
							ExportMetallicGlossTexture(image, fileOutputPath, true);
							break;
						case TextureMapType.MetallicGloss_DontConvert:
							ExportMetallicGlossTexture(image, fileOutputPath, false);
							break;
						case TextureMapType.Bump:
							ExportNormalTexture(image, fileOutputPath);
							break;
						default:
							ExportTexture(image, fileOutputPath);
							break;
					}
				}

				writeImageToDiskMarker.End();
			}
		}

		/// <summary>
		/// This converts Unity's metallic-gloss texture representation into GLTF's metallic-roughness specifications.
		/// Unity's metallic-gloss A channel (glossiness) is inverted and goes into GLTF's metallic-roughness G channel (roughness).
		/// Unity's metallic-gloss R channel (metallic) goes into GLTF's metallic-roughess B channel.
		/// </summary>
		/// <param name="texture">Unity's metallic-gloss texture to be exported</param>
		/// <param name="outputPath">The location to export the texture</param>
		private void ExportMetallicGlossTexture(Texture2D texture, string outputPath, bool swapMetalGlossChannels)
		{
			var destRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			if (swapMetalGlossChannels)
				Graphics.Blit(texture, destRenderTexture, _metalGlossChannelSwapMaterial);
			else
				Graphics.Blit(texture, destRenderTexture);
			WriteRenderTextureToDiskAndRelease(destRenderTexture, outputPath, true);
		}

		/// <summary>
		/// This export's the normal texture. If a texture is marked as a normal map, the values are stored in the A and G channel.
		/// To output the correct normal texture, the A channel is put into the R channel.
		/// </summary>
		/// <param name="texture">Unity's normal texture to be exported</param>
		/// <param name="outputPath">The location to export the texture</param>
		private void ExportNormalTexture(Texture2D texture, string outputPath)
		{
			var destRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			Graphics.Blit(texture, destRenderTexture, _normalChannelMaterial);
			WriteRenderTextureToDiskAndRelease(destRenderTexture, outputPath, false);
		}

		private void ExportTexture(Texture2D texture, string outputPath)
		{
			var destRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
			Graphics.Blit(texture, destRenderTexture);
			WriteRenderTextureToDiskAndRelease(destRenderTexture, outputPath, false);
		}

		private void WriteRenderTextureToDiskAndRelease(RenderTexture destRenderTexture, string outputPath, bool linear)
		{
			RenderTexture.active = destRenderTexture;

			var exportTexture = new Texture2D(destRenderTexture.width, destRenderTexture.height, TextureFormat.ARGB32, false, false);
			exportTexture.ReadPixels(new Rect(0, 0, destRenderTexture.width, destRenderTexture.height), 0, 0);
			exportTexture.Apply();

			var binaryData = outputPath.EndsWith(".jpg") ? exportTexture.EncodeToJPG(settings.DefaultJpegQuality) : exportTexture.EncodeToPNG();
			File.WriteAllBytes(outputPath, binaryData);

			RenderTexture.ReleaseTemporary(destRenderTexture);
			if (Application.isEditor)
				GameObject.DestroyImmediate(exportTexture);
			else
				GameObject.Destroy(exportTexture);
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
			if (!settings.ExportDisabledGameObjects && !transform.gameObject.activeSelf) return false;
			if (settings.UseMainCameraVisibility && (_exportLayerMask >= 0 && _exportLayerMask != (_exportLayerMask | 1 << transform.gameObject.layer))) return false;
			if (transform.CompareTag("EditorOnly")) return false;
			return true;
		}

		private SceneId ExportScene(string name, Transform[] rootObjTransforms)
		{
			exportSceneMarker.Begin();

			var scene = new GLTFScene();

			if (ExportNames)
			{
				scene.Name = name;
			}

			if(_exportOptions.TreatEmptyRootAsScene)
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
				// if(!ShouldExportTransform(transform)) continue;
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
			exportNodeMarker.Begin();

			var node = new Node();

			if (ExportNames)
			{
				node.Name = nodeTransform.name;
			}

#if ANIMATION_SUPPORTED
			if (nodeTransform.GetComponent<UnityEngine.Animation>() || nodeTransform.GetComponent<UnityEngine.Animator>())
			{
				_animatedNodes.Add(nodeTransform);
			}
#endif
			if (nodeTransform.GetComponent<SkinnedMeshRenderer>() && ContainsValidRenderer(nodeTransform.gameObject))
			{
				_skinnedNodes.Add(nodeTransform);
			}

			// export camera attached to node
			Camera unityCamera = nodeTransform.GetComponent<Camera>();
			if (unityCamera != null && unityCamera.enabled)
			{
				node.Camera = ExportCamera(unityCamera);
			}

			// export lights
            Light unityLight = nodeTransform.GetComponent<Light>();
            if (unityLight != null && unityLight.enabled)
            {
                node.Light = ExportLight(unityLight);
                var prevRotation = nodeTransform.rotation;
                nodeTransform.rotation *= new Quaternion(0, -1, 0, 0);
                node.SetUnityTransform(nodeTransform);
                nodeTransform.rotation = prevRotation;
            }
            else
            {
                node.SetUnityTransform(nodeTransform);
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
			GameObject[] primitives, nonPrimitives;
			FilterPrimitives(nodeTransform, out primitives, out nonPrimitives);
			if (primitives.Length > 0)
			{
				node.Mesh = ExportMesh(nodeTransform.name, primitives);

				// associate unity meshes with gltf mesh id
				foreach (var prim in primitives)
				{
					var smr = prim.GetComponent<SkinnedMeshRenderer>();
					if (smr != null)
					{
						_primOwner[new PrimKey { Mesh = smr.sharedMesh, Materials = smr.sharedMaterials }] = node.Mesh;
					}
					else
					{
						var filter = prim.GetComponent<MeshFilter>();
						var renderer = prim.GetComponent<MeshRenderer>();
						_primOwner[new PrimKey { Mesh = filter.sharedMesh, Materials = renderer.sharedMaterials }] = node.Mesh;
					}
				}
			}

			exportNodeMarker.End();

			// children that are not primitives get added as child nodes
			if (nonPrimitives.Length > 0)
			{
				node.Children = new List<NodeId>(nonPrimitives.Length);
				foreach (var child in nonPrimitives)
				{
					if(!ShouldExportTransform(child.transform)) continue;
					node.Children.Add(ExportNode(child.transform));
				}
			}

			// node export callback
			afterNodeExportMarker.Begin();
			_exportOptions.AfterNodeExport?.Invoke(this, _root, nodeTransform, node);
			AfterNodeExport?.Invoke(this, _root, nodeTransform, node);
			afterNodeExportMarker.End();

			return id;
		}

		private CameraId ExportCamera(Camera unityCamera)
		{
			GLTFCamera camera = new GLTFCamera();
			//name
			camera.Name = unityCamera.name;

			//type
			bool isOrthographic = unityCamera.orthographic;
			camera.Type = isOrthographic ? CameraType.orthographic : CameraType.perspective;
			Matrix4x4 matrix = unityCamera.projectionMatrix;

			//matrix properties: compute the fields from the projection matrix
			if (isOrthographic)
			{
				CameraOrthographic ortho = new CameraOrthographic();

				ortho.XMag = 1 / matrix[0, 0];
				ortho.YMag = 1 / matrix[1, 1];

				float farClip = (matrix[2, 3] / matrix[2, 2]) - (1 / matrix[2, 2]);
				float nearClip = farClip + (2 / matrix[2, 2]);
				ortho.ZFar = farClip;
				ortho.ZNear = nearClip;

				camera.Orthographic = ortho;
			}
			else
			{
				CameraPerspective perspective = new CameraPerspective();
				float fov = 2 * Mathf.Atan(1 / matrix[1, 1]);
				float aspectRatio = matrix[1, 1] / matrix[0, 0];
				perspective.YFov = fov;
				perspective.AspectRatio = aspectRatio;

				if (matrix[2, 2] == -1)
				{
					//infinite projection matrix
					float nearClip = matrix[2, 3] * -0.5f;
					perspective.ZNear = nearClip;
				}
				else
				{
					//finite projection matrix
					float farClip = matrix[2, 3] / (matrix[2, 2] + 1);
					float nearClip = farClip * (matrix[2, 2] + 1) / (matrix[2, 2] - 1);
					perspective.ZFar = farClip;
					perspective.ZNear = nearClip;
				}
				camera.Perspective = perspective;
			}

			var id = new CameraId
			{
				Id = _root.Cameras.Count,
				Root = _root
			};

			_root.Cameras.Add(camera);

			return id;
		}

		private static bool ContainsValidRenderer(GameObject gameObject)
		{
			if(!gameObject) return false;
			var meshRenderer = gameObject.GetComponent<MeshRenderer>();
			var meshFilter = gameObject.GetComponent<MeshFilter>();
			var skinnedMeshRender = gameObject.GetComponent<SkinnedMeshRenderer>();
			var materials = meshRenderer ? meshRenderer.sharedMaterials : skinnedMeshRender ? skinnedMeshRender.sharedMaterials : null;
			var anyMaterialIsNonNull = false;
			if(materials != null)
				for (int i = 0; i < materials.Length; i++)
					anyMaterialIsNonNull |= materials[i];
			return (meshFilter && meshRenderer && meshRenderer.enabled) || (skinnedMeshRender && skinnedMeshRender.enabled) && anyMaterialIsNonNull;
		}

        private LightId ExportLight(Light unityLight)
        {
	        DeclareExtensionUsage(KHR_lights_punctualExtensionFactory.EXTENSION_NAME, false);

            GLTFLight light;

            if (unityLight.type == LightType.Spot)
            {
                light = new GLTFSpotLight() { innerConeAngle = unityLight.spotAngle / 2 * Mathf.Deg2Rad * 0.8f, outerConeAngle = unityLight.spotAngle / 2 * Mathf.Deg2Rad };
                //name
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
                light.range = unityLight.range;
                light.intensity = unityLight.intensity * Mathf.PI;
            }
            else if (unityLight.type == LightType.Directional)
            {
                light = new GLTFDirectionalLight();
                //name
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
                light.intensity = unityLight.intensity * Mathf.PI;
            }
            else if (unityLight.type == LightType.Point)
            {
                light = new GLTFPointLight();
                //name
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
                light.range = unityLight.range;
                light.intensity = unityLight.intensity * Mathf.PI;
            }
            else
            {
                light = new GLTFLight();
                //name
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
            }

            if (_root.Lights == null)
            {
                _root.Lights = new List<GLTFLight>();
            }

            var id = new LightId
            {
                Id = _root.Lights.Count,
                Root = _root
            };

            //list of lightids should be in extensions object
            _root.Lights.Add(light);

            return id;
        }

        private void FilterPrimitives(Transform transform, out GameObject[] primitives, out GameObject[] nonPrimitives)
		{
			var childCount = transform.childCount;
			var prims = new List<GameObject>(childCount + 1);
			var nonPrims = new List<GameObject>(childCount);

			// add another primitive if the root object also has a mesh
			if (transform.gameObject.activeSelf || ExportDisabledGameObjects)
			{
				if (ContainsValidRenderer(transform.gameObject))
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

		private static bool IsPrimitive(GameObject gameObject)
		{
			/*
			 * Primitives have the following properties:
			 * - have no children
			 * - have no non-default local transform properties
			 * - have MeshFilter and MeshRenderer components OR has SkinnedMeshRenderer component
			 */
			return gameObject.transform.childCount == 0
				&& gameObject.transform.localPosition == Vector3.zero
				&& gameObject.transform.localRotation == Quaternion.identity
				&& gameObject.transform.localScale == Vector3.one
				&& ContainsValidRenderer(gameObject);

		}
		private void ExportAnimation()
		{
			for (int i = 0; i < _animatedNodes.Count; ++i)
			{
				Transform t = _animatedNodes[i];
				ExportAnimationFromNode(ref t);
			}
		}

		private MeshId ExportMesh(string name, GameObject[] primitives)
		{
			exportMeshMarker.Begin();

			// check if this set of primitives is already a mesh
			MeshId existingMeshId = null;
			var key = new PrimKey();
			foreach (var prim in primitives)
			{
				var smr = prim.GetComponent<SkinnedMeshRenderer>();
				if (smr != null)
				{
					key.Mesh = smr.sharedMesh;
					key.Materials = smr.sharedMaterials;
				}
				else
				{
					var filter = prim.GetComponent<MeshFilter>();
					var renderer = prim.GetComponent<MeshRenderer>();
					key.Mesh = filter.sharedMesh;
					key.Materials = renderer.sharedMaterials;
				}

				MeshId tempMeshId;
				if (_primOwner.TryGetValue(key, out tempMeshId) && (existingMeshId == null || tempMeshId == existingMeshId))
				{
					existingMeshId = tempMeshId;
				}
				else
				{
					existingMeshId = null;
					break;
				}
			}

			// if so, return that mesh id
			if (existingMeshId != null)
			{
				return existingMeshId;
			}

			// if not, create new mesh and return its id
			var mesh = new GLTFMesh();

			if (ExportNames)
			{
				mesh.Name = name;
			}

			mesh.Primitives = new List<MeshPrimitive>(primitives.Length);
			foreach (var prim in primitives)
			{
				MeshPrimitive[] meshPrimitives = ExportPrimitive(prim, mesh);
				if (meshPrimitives != null)
				{
					mesh.Primitives.AddRange(meshPrimitives);
				}
			}

			var id = new MeshId
			{
				Id = _root.Meshes.Count,
				Root = _root
			};

			exportMeshMarker.End();

			if (mesh.Primitives.Count > 0)
			{
				_root.Meshes.Add(mesh);
				return id;
			}

			return null;
		}

#if UNITY_EDITOR
		private const string MakeMeshReadableDialogueDecisionKey = nameof(MakeMeshReadableDialogueDecisionKey);
		private static PropertyInfo canAccessProperty =
			typeof(Mesh).GetProperty("canAccess", BindingFlags.Instance | BindingFlags.Default | BindingFlags.NonPublic);
#endif

		private static bool MeshIsReadable(Mesh mesh)
		{
#if UNITY_EDITOR
			return mesh.isReadable || (bool) (canAccessProperty?.GetMethod?.Invoke(mesh, null) ?? true);
#else
			return mesh.isReadable;
#endif
		}

		// a mesh *might* decode to multiple prims if there are submeshes
		private MeshPrimitive[] ExportPrimitive(GameObject gameObject, GLTFMesh mesh)
		{
			exportPrimitiveMarker.Begin();

			Mesh meshObj = null;
			SkinnedMeshRenderer smr = null;
			var filter = gameObject.GetComponent<MeshFilter>();
			if (filter)
			{
				meshObj = filter.sharedMesh;
			}
			else
			{
				smr = gameObject.GetComponent<SkinnedMeshRenderer>();
				if (smr)
				{
					meshObj = smr.sharedMesh;
				}
			}
			if (!meshObj)
			{
				Debug.LogWarning($"MeshFilter.sharedMesh on GameObject:{gameObject.name} is missing, skipping", gameObject);
				exportPrimitiveMarker.End();
				return null;
			}

#if UNITY_EDITOR
			if (!MeshIsReadable(meshObj) && EditorUtility.IsPersistent(meshObj))
			{
#if UNITY_2019_3_OR_NEWER
				if(EditorUtility.DisplayDialog("Exporting mesh but mesh is not readable",
					   $"The mesh {meshObj.name} is not readable. Do you want to change its import settings and make it readable now?",
					   "Make it readable", "No, skip mesh",
					   DialogOptOutDecisionType.ForThisSession, MakeMeshReadableDialogueDecisionKey))
#endif
				{
					var path = AssetDatabase.GetAssetPath(meshObj);
					var importer = AssetImporter.GetAtPath(path) as ModelImporter;
					if (importer)
					{
						importer.isReadable = true;
						importer.SaveAndReimport();
					}
				}
#if UNITY_2019_3_OR_NEWER
				else
				{
					Debug.LogWarning($"The mesh {meshObj.name} is not readable. Skipping", null);
					exportPrimitiveMarker.End();
					return null;
				}
#endif
			}
#endif

			if (Application.isPlaying && !MeshIsReadable(meshObj))
			{
				Debug.LogWarning($"The mesh {meshObj.name} is not readable. Skipping", null);
				exportPrimitiveMarker.End();
				return null;
			}

			var renderer = gameObject.GetComponent<MeshRenderer>();
			if (!renderer) smr = gameObject.GetComponent<SkinnedMeshRenderer>();

			if(!renderer && !smr)
			{
				Debug.LogWarning("GameObject does have neither renderer nor SkinnedMeshRenderer! " + gameObject.name, gameObject);
				exportPrimitiveMarker.End();
				return null;
			}
			var materialsObj = renderer ? renderer.sharedMaterials : smr.sharedMaterials;

			var prims = new MeshPrimitive[meshObj.subMeshCount];
			List<MeshPrimitive> nonEmptyPrims = null;
			var vertices = meshObj.vertices;
			if (vertices.Length < 1)
			{
				Debug.LogWarning("MeshFilter does not contain any vertices, won't export: " + gameObject.name, gameObject);
				exportPrimitiveMarker.End();
				return null;
			}

			if (!_meshToPrims.ContainsKey(meshObj))
			{
				AccessorId aPosition = null, aNormal = null, aTangent = null, aTexcoord0 = null, aTexcoord1 = null, aColor0 = null;

				aPosition = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(meshObj.vertices, SchemaExtensions.CoordinateSpaceConversionScale));

				if (meshObj.normals.Length != 0)
					aNormal = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(meshObj.normals, SchemaExtensions.CoordinateSpaceConversionScale));

				if (meshObj.tangents.Length != 0)
					aTangent = ExportAccessor(SchemaExtensions.ConvertVector4CoordinateSpaceAndCopy(meshObj.tangents, SchemaExtensions.TangentSpaceConversionScale));

				if (meshObj.uv.Length != 0)
					aTexcoord0 = ExportAccessor(SchemaExtensions.FlipTexCoordArrayVAndCopy(meshObj.uv));

				if (meshObj.uv2.Length != 0)
					aTexcoord1 = ExportAccessor(SchemaExtensions.FlipTexCoordArrayVAndCopy(meshObj.uv2));

				if (settings.ExportVertexColors && meshObj.colors.Length != 0)
					aColor0 = ExportAccessor(QualitySettings.activeColorSpace == ColorSpace.Linear ? meshObj.colors : meshObj.colors.ToLinear());

				aPosition.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
				if (aNormal != null) aNormal.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
				if (aTangent != null) aTangent.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
				if (aTexcoord0 != null) aTexcoord0.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
				if (aTexcoord1 != null) aTexcoord1.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
				if (aColor0 != null) aColor0.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;

				_meshToPrims.Add(meshObj, new MeshAccessors()
				{
					aPosition = aPosition,
					aNormal = aNormal,
					aTangent = aTangent,
					aTexcoord0 = aTexcoord0,
					aTexcoord1 = aTexcoord1,
					aColor0 = aColor0,
					subMeshPrimitives = new Dictionary<int, MeshPrimitive>()
				});
			}

			var accessors = _meshToPrims[meshObj];

			// walk submeshes and export the ones with non-null meshes
			for (int submesh = 0; submesh < meshObj.subMeshCount; submesh++)
			{
				if (submesh >= materialsObj.Length) continue;
				if (!materialsObj[submesh]) continue;

				if (!accessors.subMeshPrimitives.ContainsKey(submesh))
				{
					var primitive = new MeshPrimitive();

					var topology = meshObj.GetTopology(submesh);
					var indices = meshObj.GetIndices(submesh);
					if (topology == MeshTopology.Triangles) SchemaExtensions.FlipTriangleFaces(indices);

					primitive.Mode = GetDrawMode(topology);
					primitive.Indices = ExportAccessor(indices, true);
					primitive.Indices.Value.BufferView.Value.Target = BufferViewTarget.ElementArrayBuffer;

					primitive.Attributes = new Dictionary<string, AccessorId>();
					primitive.Attributes.Add(SemanticProperties.POSITION, accessors.aPosition);

					if (accessors.aNormal != null)
						primitive.Attributes.Add(SemanticProperties.NORMAL, accessors.aNormal);
					if (accessors.aTangent != null)
						primitive.Attributes.Add(SemanticProperties.TANGENT, accessors.aTangent);
					if (accessors.aTexcoord0 != null)
						primitive.Attributes.Add(SemanticProperties.TEXCOORD_0, accessors.aTexcoord0);
					if (accessors.aTexcoord1 != null)
						primitive.Attributes.Add(SemanticProperties.TEXCOORD_1, accessors.aTexcoord1);
					if (accessors.aColor0 != null)
						primitive.Attributes.Add(SemanticProperties.COLOR_0, accessors.aColor0);

					primitive.Material = null;

					ExportBlendShapes(smr, meshObj, submesh, primitive, mesh);

					accessors.subMeshPrimitives.Add(submesh, primitive);
				}

				var submeshPrimitive = accessors.subMeshPrimitives[submesh];
				prims[submesh] = new MeshPrimitive(submeshPrimitive, _root)
				{
					Material = ExportMaterial(materialsObj[submesh]),
				};
			}

			//remove any prims that have empty triangles
            nonEmptyPrims = new List<MeshPrimitive>(prims);
            nonEmptyPrims.RemoveAll(EmptyPrimitive);
            prims = nonEmptyPrims.ToArray();

            exportPrimitiveMarker.End();

			return prims;
		}

        private static bool EmptyPrimitive(MeshPrimitive prim)
        {
            if (prim == null || prim.Attributes == null)
            {
                return true;
            }
            return false;
        }


        private MaterialId CreateAndAddMaterialId(Material materialObj, GLTFMaterial material)
        {
	        _materials.Add(materialObj, _materials.Count);

	        var id = new MaterialId
	        {
		        Id = _root.Materials.Count,
		        Root = _root
	        };
	        _root.Materials.Add(material);

	        // after material export
	        afterMaterialExportMarker.Begin();
	        _exportOptions.AfterMaterialExport?.Invoke(this, _root, materialObj, material);
	        AfterMaterialExport?.Invoke(this, _root, materialObj, material);
	        afterMaterialExportMarker.End();

	        return id;
        }

        static void DecomposeEmissionColor(Color input, out Color output, out float intensity)
        {
	        var emissiveAmount = input.linear;
	        var maxEmissiveAmount = Mathf.Max(emissiveAmount.r, emissiveAmount.g, emissiveAmount.b);
	        if (maxEmissiveAmount > 1)
	        {
		        emissiveAmount.r /= maxEmissiveAmount;
		        emissiveAmount.g /= maxEmissiveAmount;
		        emissiveAmount.b /= maxEmissiveAmount;
	        }
	        emissiveAmount.a = Mathf.Clamp01(emissiveAmount.a);

	        // this feels wrong but leads to the right results, probably the above calculations are in the wrong color space
	        maxEmissiveAmount = Mathf.LinearToGammaSpace(maxEmissiveAmount);

	        output = emissiveAmount;
	        intensity = maxEmissiveAmount;
        }

        static void DecomposeScaleOffset(Vector4 input, out Vector2 scale, out Vector2 offset)
        {
	        scale = new Vector2(input.x, input.y);
	        offset = new Vector2(input.z, 1 - input.w - input.y);
        }

        public MaterialId ExportMaterial(Material materialObj)
		{
            //TODO if material is null
			MaterialId id = GetMaterialId(_root, materialObj);
			if (id != null)
			{
				return id;
			}

			var material = new GLTFMaterial();

            if (!materialObj)
            {
                if (ExportNames)
                {
                    material.Name = "null";
                }
                material.PbrMetallicRoughness = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };
                return CreateAndAddMaterialId(materialObj, material);
            }

			if (ExportNames)
			{
				material.Name = materialObj.name;
			}

			// before material export: only continue with regular export if that didn't succeed.
			if (_exportOptions.BeforeMaterialExport != null)
			{
				beforeMaterialExportMarker.Begin();
				if (_exportOptions.BeforeMaterialExport.Invoke(this, _root, materialObj, material))
				{
					beforeMaterialExportMarker.End();
					return CreateAndAddMaterialId(materialObj, material);
				}
				else
				{
					beforeMaterialExportMarker.End();
				}
			}


			// static callback, run after options callback
			// we're iterating here because we want to stop calling any once we hit one that can export this material.
			if (BeforeMaterialExport != null)
			{
				var list = BeforeMaterialExport.GetInvocationList();
				foreach (var entry in list)
				{
					beforeMaterialExportMarker.Begin();
					var cb = (BeforeMaterialExportDelegate) entry;
					if (cb != null && cb.Invoke(this, _root, materialObj, material))
					{
						beforeMaterialExportMarker.End();
						return CreateAndAddMaterialId(materialObj, material);
					}
					else
					{
						beforeMaterialExportMarker.End();
					}
				}
			}

			exportMaterialMarker.Begin();

			switch (materialObj.GetTag("RenderType", false, ""))
			{
				case "TransparentCutout":
					if (materialObj.HasProperty("_Cutoff"))
					{
						material.AlphaCutoff = materialObj.GetFloat("_Cutoff");
					}
					material.AlphaMode = AlphaMode.MASK;
					break;
				case "Transparent":
				case "Fade":
					material.AlphaMode = AlphaMode.BLEND;
					break;
				default:
					if (materialObj.IsKeywordEnabled("_ALPHATEST_ON"))
					{
						if (materialObj.HasProperty("_Cutoff"))
						{
							material.AlphaCutoff = materialObj.GetFloat("_Cutoff");
						}
						material.AlphaMode = AlphaMode.MASK;
					}
					material.AlphaMode = AlphaMode.OPAQUE;
					break;
			}

			material.DoubleSided = (materialObj.HasProperty("_Cull") && materialObj.GetInt("_Cull") == (int)CullMode.Off) ||
			                       (materialObj.HasProperty("_CullMode") && materialObj.GetInt("_CullMode") == (int)CullMode.Off);

			if(materialObj.IsKeywordEnabled("_EMISSION") || materialObj.IsKeywordEnabled("EMISSION"))
			{
				if (materialObj.HasProperty("_EmissionColor"))
				{
					var c = materialObj.GetColor("_EmissionColor");
					DecomposeEmissionColor(c, out var emissiveAmount, out var maxEmissiveAmount);
					material.EmissiveFactor = emissiveAmount.ToNumericsColorRaw();

					if(maxEmissiveAmount > 1)
					{
						material.AddExtension(KHR_materials_emissive_strength_Factory.EXTENSION_NAME, new KHR_materials_emissive_strength() { emissiveStrength = maxEmissiveAmount });
						DeclareExtensionUsage(KHR_materials_emissive_strength_Factory.EXTENSION_NAME, false);
					}
				}

				if (materialObj.HasProperty("_EmissionMap"))
				{
					var emissionTex = materialObj.GetTexture("_EmissionMap");

					if (emissionTex != null)
					{
						if(emissionTex is Texture2D)
						{
							material.EmissiveTexture = ExportTextureInfo(emissionTex, TextureMapType.Emission);

							ExportTextureTransform(material.EmissiveTexture, materialObj, "_EmissionMap");
						}
						else
						{
							Debug.LogFormat(LogType.Error, "Can't export a {0} emissive texture in material {1}", emissionTex.GetType(), materialObj.name);
						}

					}
				}
			}

			// workaround for glTFast roundtrip: has a _BumpMap but no _NORMALMAP or _BUMPMAP keyword.
			var is_glTFastShader = materialObj.shader.name.IndexOf("glTF", StringComparison.Ordinal) > -1;
			if (materialObj.HasProperty("_BumpMap") && (materialObj.IsKeywordEnabled("_NORMALMAP") || materialObj.IsKeywordEnabled("_BUMPMAP") || is_glTFastShader))
			{
				var normalTex = materialObj.GetTexture("_BumpMap");

				if (normalTex != null)
				{
					if(normalTex is Texture2D)
					{
						material.NormalTexture = ExportNormalTextureInfo(normalTex, TextureMapType.Bump, materialObj);
						ExportTextureTransform(material.NormalTexture, materialObj, "_BumpMap");
					}
					else
					{
						Debug.LogFormat(LogType.Error, "Can't export a {0} normal texture in material {1}", normalTex.GetType(), materialObj.name);
					}
				}
			}
			if (materialObj.HasProperty("normalTexture"))
			{
				var normalTex = materialObj.GetTexture("normalTexture");

				if (normalTex != null)
				{
					if(normalTex is Texture2D)
					{
						material.NormalTexture = ExportNormalTextureInfo(normalTex, TextureMapType.Bump, materialObj);
						ExportTextureTransform(material.NormalTexture, materialObj, "_BumpMap");
					}
					else
					{
						Debug.LogFormat(LogType.Error, "Can't export a {0} normal texture in material {1}", normalTex.GetType(), materialObj.name);
					}
				}
			}

			if (materialObj.HasProperty("_OcclusionMap"))
			{
				var occTex = materialObj.GetTexture("_OcclusionMap");
				if (occTex != null)
				{
					if(occTex is Texture2D)
					{
						material.OcclusionTexture = ExportOcclusionTextureInfo(occTex, TextureMapType.Occlusion, materialObj);
						ExportTextureTransform(material.OcclusionTexture, materialObj, "_OcclusionMap");
					}
					else
					{
						Debug.LogFormat(LogType.Error, "Can't export a {0} occlusion texture in material {1}", occTex.GetType(), materialObj.name);
					}
				}
			}
			if( IsUnlit(materialObj)){

				ExportUnlit( material, materialObj );
			}
			else if (IsPBRMetallicRoughness(materialObj))
			{
				material.PbrMetallicRoughness = ExportPBRMetallicRoughness(materialObj);
			}
			else if (IsPBRSpecularGlossiness(materialObj))
			{
				ExportPBRSpecularGlossiness(material, materialObj);
			}
			else if (IsCommonConstant(materialObj))
			{
				material.CommonConstant = ExportCommonConstant(materialObj);
			}
			else if (materialObj.HasProperty("_BaseMap"))
			{
				var mainTex = materialObj.GetTexture("_BaseMap");
				material.PbrMetallicRoughness = new PbrMetallicRoughness()
				{
					BaseColorFactor = (materialObj.HasProperty("_BaseColor")
						? materialObj.GetColor("_BaseColor")
						: Color.white).ToNumericsColorLinear(),
					BaseColorTexture = mainTex ? ExportTextureInfo(mainTex, TextureMapType.Main) : null
				};
			}
			else if (materialObj.HasProperty("_ColorTexture"))
			{
				var mainTex = materialObj.GetTexture("_ColorTexture");
				material.PbrMetallicRoughness = new PbrMetallicRoughness()
				{
					BaseColorFactor = (materialObj.HasProperty("_BaseColor")
						? materialObj.GetColor("_BaseColor")
						: Color.white).ToNumericsColorLinear(),
					BaseColorTexture = mainTex ? ExportTextureInfo(mainTex, TextureMapType.Main) : null
				};
			}
            else if (materialObj.HasProperty("_MainTex")) //else export main texture
            {
                var mainTex = materialObj.GetTexture("_MainTex");

                if (mainTex != null)
                {
                    material.PbrMetallicRoughness = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };
                    material.PbrMetallicRoughness.BaseColorTexture = ExportTextureInfo(mainTex, TextureMapType.Main);
                    ExportTextureTransform(material.PbrMetallicRoughness.BaseColorTexture, materialObj, "_MainTex");
                }
                if (materialObj.HasProperty("_TintColor")) //particles use _TintColor instead of _Color
                {
                    if (material.PbrMetallicRoughness == null)
                        material.PbrMetallicRoughness = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };

                    material.PbrMetallicRoughness.BaseColorFactor = materialObj.GetColor("_TintColor").ToNumericsColorLinear();
                }
                material.DoubleSided = true;
            }

			exportMaterialMarker.End();

			return CreateAndAddMaterialId(materialObj, material);
		}

		// Blend Shapes / Morph Targets
		// Adopted from Gary Hsu (bghgary)
		// https://github.com/bghgary/glTF-Tools-for-Unity/blob/master/UnityProject/Assets/Gltf/Editor/Exporter.cs
		private void ExportBlendShapes(SkinnedMeshRenderer smr, Mesh meshObj, int submeshIndex, MeshPrimitive primitive, GLTFMesh mesh)
		{
			if (settings.BlendShapeExportProperties == GLTFSettings.BlendShapeExportPropertyFlags.None)
				return;

			if (_meshToBlendShapeAccessors.TryGetValue(meshObj, out var data))
			{
				mesh.Weights = data.weights;
				primitive.Targets = data.targets;
				primitive.TargetNames = data.targetNames;
				return;
			}

			if (smr != null && meshObj.blendShapeCount > 0)
			{
				List<Dictionary<string, AccessorId>> targets = new List<Dictionary<string, AccessorId>>(meshObj.blendShapeCount);
				List<Double> weights = new List<double>(meshObj.blendShapeCount);
				List<string> targetNames = new List<string>(meshObj.blendShapeCount);

#if UNITY_2019_3_OR_NEWER
				var meshHasNormals = meshObj.HasVertexAttribute(VertexAttribute.Normal);
				var meshHasTangents = meshObj.HasVertexAttribute(VertexAttribute.Tangent);
#else
				var meshHasNormals = meshObj.normals.Length > 0;
				var meshHasTangents = meshObj.tangents.Length > 0;
#endif

				for (int blendShapeIndex = 0; blendShapeIndex < meshObj.blendShapeCount; blendShapeIndex++)
				{
					exportBlendShapeMarker.Begin();

					targetNames.Add(meshObj.GetBlendShapeName(blendShapeIndex));
					// As described above, a blend shape can have multiple frames.  Given that glTF only supports a single frame
					// per blend shape, we'll always use the final frame (the one that would be for when 100% weight is applied).
					int frameIndex = meshObj.GetBlendShapeFrameCount(blendShapeIndex) - 1;

					var deltaVertices = new Vector3[meshObj.vertexCount];
					var deltaNormals = new Vector3[meshObj.vertexCount];
					var deltaTangents = new Vector3[meshObj.vertexCount];
					meshObj.GetBlendShapeFrameVertices(blendShapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);

					var exportTargets = new Dictionary<string, AccessorId>();

					if (!settings.BlendShapeExportSparseAccessors)
					{
						var positionAccessor = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(deltaVertices, SchemaExtensions.CoordinateSpaceConversionScale));
						positionAccessor.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
						exportTargets.Add(SemanticProperties.POSITION, positionAccessor);
					}
					else
					{
						// Debug.Log("Delta Vertices:\n"+string.Join("\n ", deltaVertices));
						// Debug.Log("Vertices:\n"+string.Join("\n ", meshObj.vertices));
						// Experimental: sparse accessor.
						// - get the accessor we want to base this upon
						// - this is how position is originally exported:
						//   ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(meshObj.vertices, SchemaExtensions.CoordinateSpaceConversionScale));
						var baseAccessor = _meshToPrims[meshObj].aPosition;
						var exportedAccessor = ExportSparseAccessor(null, null, SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(deltaVertices, SchemaExtensions.CoordinateSpaceConversionScale));
						if (exportedAccessor != null)
						{
							exportTargets.Add(SemanticProperties.POSITION, exportedAccessor);
						}
					}

					if (meshHasNormals && settings.BlendShapeExportProperties.HasFlag(GLTFSettings.BlendShapeExportPropertyFlags.Normal))
					{
						if (!settings.BlendShapeExportSparseAccessors)
						{
							var accessor = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(deltaNormals, SchemaExtensions.CoordinateSpaceConversionScale));
							accessor.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
							exportTargets.Add(SemanticProperties.NORMAL, accessor);
						}
						else
						{
							var baseAccessor = _meshToPrims[meshObj].aNormal;
							exportTargets.Add(SemanticProperties.NORMAL, ExportSparseAccessor(null, null, SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(deltaVertices, SchemaExtensions.CoordinateSpaceConversionScale)));
						}
					}
					if (meshHasTangents && settings.BlendShapeExportProperties.HasFlag(GLTFSettings.BlendShapeExportPropertyFlags.Tangent))
					{
						if (!settings.BlendShapeExportSparseAccessors)
						{
							var accessor = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(deltaTangents, SchemaExtensions.CoordinateSpaceConversionScale));
							accessor.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
							exportTargets.Add(SemanticProperties.TANGENT, accessor);
						}
						else
						{
							// 	var baseAccessor = _meshToPrims[meshObj].aTangent;
							// 	exportTargets.Add(SemanticProperties.TANGENT, ExportSparseAccessor(baseAccessor, SchemaExtensions.ConvertVector4CoordinateSpaceAndCopy(meshObj.tangents, SchemaExtensions.TangentSpaceConversionScale), SchemaExtensions.ConvertVector4CoordinateSpaceAndCopy(deltaVertices, SchemaExtensions.TangentSpaceConversionScale)));
							exportTargets.Add(SemanticProperties.TANGENT, ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(deltaTangents, SchemaExtensions.CoordinateSpaceConversionScale)));
							// Debug.LogWarning("Blend Shape Tangents for " + meshObj + " won't be exported with sparse accessors – sparse accessor for tangents isn't supported right now.");
						}
					}
					targets.Add(exportTargets);

					// We need to get the weight from the SkinnedMeshRenderer because this represents the currently
					// defined weight by the user to apply to this blend shape.  If we instead got the value from
					// the unityMesh, it would be a _per frame_ weight, and for a single-frame blend shape, that would
					// always be 100.  A blend shape might have more than one frame if a user wanted to more tightly
					// control how a blend shape will be animated during weight changes (e.g. maybe they want changes
					// between 0-50% to be really minor, but between 50-100 to be extreme, hence they'd have two frames
					// where the first frame would have a weight of 50 (meaning any weight between 0-50 should be relative
					// to the values in this frame) and then any weight between 50-100 would be relevant to the weights in
					// the second frame.  See Post 20 for more info:
					// https://forum.unity3d.com/threads/is-there-some-method-to-add-blendshape-in-editor.298002/#post-2015679
					if(exportTargets.Any())
						weights.Add(smr.GetBlendShapeWeight(blendShapeIndex) / 100);

					exportBlendShapeMarker.End();
				}

				if(weights.Any() && targets.Any())
				{
					mesh.Weights = weights;
					primitive.Targets = targets;
					primitive.TargetNames = targetNames;
				}
				else
				{
					mesh.Weights = null;
					primitive.Targets = null;
					primitive.TargetNames = null;
				}

				// cache the exported data; we can re-use it between all submeshes of a mesh.
				_meshToBlendShapeAccessors.Add(meshObj, new BlendShapeAccessors()
				{
					targets = targets,
					weights = weights,
					targetNames = targetNames
				});
			}
		}

		private bool IsPBRMetallicRoughness(Material material)
		{
			return material.HasProperty("_Metallic") && (material.HasProperty("_MetallicGlossMap") || material.HasProperty("_Glossiness") || material.HasProperty("metallicRoughnessTexture"));
		}

		private bool IsUnlit(Material material)
		{
			return material.shader.name.ToLowerInvariant().Contains("unlit");
		}

		private bool IsPBRSpecularGlossiness(Material material)
		{
			return material.HasProperty("_SpecColor") && material.HasProperty("_SpecGlossMap");
		}

		private bool IsCommonConstant(Material material)
		{
			return material.HasProperty("_AmbientFactor") &&
			material.HasProperty("_LightMap") &&
			material.HasProperty("_LightFactor");
		}

		private void ExportTextureTransform(TextureInfo def, Material mat, string texName)
		{
			Vector2 offset = mat.GetTextureOffset(texName);
			Vector2 scale = mat.GetTextureScale(texName);

			if (offset == Vector2.zero && scale == Vector2.one)
			{
				if(mat.HasProperty("_MainTex_ST"))
				{
					// difficult choice here: some shaders might support texture transform per-texture, others use the main transform.
					if(mat.HasProperty("_MainTex"))
					{
						offset = mat.mainTextureOffset;
						scale = mat.mainTextureScale;
					}
				}
				else
				{
					offset = Vector2.zero;
					scale = Vector2.one;
				}
			}

			if (_root.ExtensionsUsed == null)
			{
				_root.ExtensionsUsed = new List<string>(
					new[] { ExtTextureTransformExtensionFactory.EXTENSION_NAME }
				);
			}
			else if (!_root.ExtensionsUsed.Contains(ExtTextureTransformExtensionFactory.EXTENSION_NAME))
			{
				_root.ExtensionsUsed.Add(ExtTextureTransformExtensionFactory.EXTENSION_NAME);
			}

			if (RequireExtensions)
			{
				if (_root.ExtensionsRequired == null)
				{
					_root.ExtensionsRequired = new List<string>(
						new[] { ExtTextureTransformExtensionFactory.EXTENSION_NAME }
					);
				}
				else if (!_root.ExtensionsRequired.Contains(ExtTextureTransformExtensionFactory.EXTENSION_NAME))
				{
					_root.ExtensionsRequired.Add(ExtTextureTransformExtensionFactory.EXTENSION_NAME);
				}
			}

			if (def.Extensions == null)
				def.Extensions = new Dictionary<string, IExtension>();

			def.Extensions[ExtTextureTransformExtensionFactory.EXTENSION_NAME] = new ExtTextureTransformExtension(
				new GLTF.Math.Vector2(offset.x, 1 - offset.y - scale.y),
				0, // TODO: support rotation
				new GLTF.Math.Vector2(scale.x, scale.y),
				0 // TODO: support UV channels
			);
		}

		public NormalTextureInfo ExportNormalTextureInfo(
			Texture texture,
			TextureMapType textureMapType,
			Material material)
		{
			var info = new NormalTextureInfo();

			info.Index = ExportTexture(texture, textureMapType);

			if (material.HasProperty("_BumpScale"))
			{
				info.Scale = material.GetFloat("_BumpScale");
			}

			return info;
		}

		private OcclusionTextureInfo ExportOcclusionTextureInfo(
			Texture texture,
			TextureMapType textureMapType,
			Material material)
		{
			var info = new OcclusionTextureInfo();

			info.Index = ExportTexture(texture, textureMapType);

			if (material.HasProperty("_OcclusionStrength"))
			{
				info.Strength = material.GetFloat("_OcclusionStrength");
			}

			return info;
		}

		public PbrMetallicRoughness ExportPBRMetallicRoughness(Material material)
		{
			var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };
			var isGltfPbrMetallicRoughnessShader = material.shader.name.Equals("GLTF/PbrMetallicRoughness", StringComparison.Ordinal);
			var isGlTFastShader = material.shader.name.Equals("glTF/PbrMetallicRoughness", StringComparison.Ordinal);

			if (material.HasProperty("_Color"))
			{
				pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();
			}

			if (material.HasProperty("_BaseColor"))
			{
				pbr.BaseColorFactor = material.GetColor("_BaseColor").ToNumericsColorLinear();
			}

            if (material.HasProperty("_TintColor")) //particles use _TintColor instead of _Color
            {
                float white = 1;
                if (material.HasProperty("_Color"))
                {
                    var c = material.GetColor("_Color");
                    white = (c.r + c.g + c.b) / 3.0f; //multiply alpha by overall whiteness of TintColor
                }

                pbr.BaseColorFactor = (material.GetColor("_TintColor") * white).ToNumericsColorLinear() ;
            }

            if (material.HasProperty("_MainTex") || material.HasProperty("_BaseMap")) //TODO if additive particle, render black into alpha
			{
				// TODO use private Material.GetFirstPropertyNameIdByAttribute here, supported from 2020.1+
				var mainTexPropertyName = material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
				var mainTex = material.GetTexture(mainTexPropertyName);

				if (mainTex)
				{
					pbr.BaseColorTexture = ExportTextureInfo(mainTex, TextureMapType.Main);
					ExportTextureTransform(pbr.BaseColorTexture, material, mainTexPropertyName);
				}
			}

            var ignoreMetallicFactor = material.IsKeywordEnabled("_METALLICGLOSSMAP") && !isGltfPbrMetallicRoughnessShader && !isGlTFastShader;
			if (material.HasProperty("_Metallic") && !ignoreMetallicFactor)
			{
				pbr.MetallicFactor = material.GetFloat("_Metallic");
			}

			if (material.HasProperty("_Roughness"))
			{
				float roughness = material.GetFloat("_Roughness");
				pbr.RoughnessFactor = roughness;
			}
			else if (material.HasProperty("_Glossiness") || material.HasProperty("_Smoothness"))
			{
				var smoothnessPropertyName = material.HasProperty("_Smoothness") ? "_Smoothness" : "_Glossiness";
				var metallicGlossMap = material.HasProperty("_MetallicGlossMap") ? material.GetTexture("_MetallicGlossMap") : null;
				float smoothness = material.GetFloat(smoothnessPropertyName);
				// legacy workaround: the UnityGLTF shaders misuse "_Glossiness" as roughness but don't have a keyword for it.
				if (isGltfPbrMetallicRoughnessShader)
					smoothness = 1 - smoothness;
				pbr.RoughnessFactor = (metallicGlossMap && material.HasProperty("_GlossMapScale")) ? (1 - material.GetFloat("_GlossMapScale")) : (1.0 - smoothness);
			}

			if (material.HasProperty("_MetallicGlossMap"))
			{
				var mrTex = material.GetTexture("_MetallicGlossMap");

				if (mrTex)
				{
					pbr.MetallicRoughnessTexture = ExportTextureInfo(mrTex, (isGltfPbrMetallicRoughnessShader || isGlTFastShader) ? TextureMapType.MetallicGloss_DontConvert : TextureMapType.MetallicGloss);
					// in the Standard shader, _METALLICGLOSSMAP replaces _Metallic and so we need to set the multiplier to 1;
					// that's not true for the gltf shaders though, so we keep the value there.
					if (ignoreMetallicFactor)
						pbr.MetallicFactor = 1.0f;
					ExportTextureTransform(pbr.MetallicRoughnessTexture, material, "_MetallicGlossMap");
				}
			}
			else if (material.HasProperty("metallicRoughnessTexture"))
			{
				var mrTex = material.GetTexture("metallicRoughnessTexture");
				if (mrTex)
				{
					pbr.MetallicRoughnessTexture = ExportTextureInfo(mrTex, TextureMapType.MetallicGloss_DontConvert);
				}
			}

			return pbr;
		}

		public void ExportUnlit(GLTFMaterial def, Material material){

			const string extname = KHR_MaterialsUnlitExtensionFactory.EXTENSION_NAME;
			DeclareExtensionUsage( extname, true );
			def.AddExtension( extname, new KHR_MaterialsUnlitExtension());

			var pbr = new PbrMetallicRoughness();

			if (material.HasProperty("_Color"))
			{
				pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();
			}

			if (material.HasProperty("_BaseColor"))
			{
				pbr.BaseColorFactor = material.GetColor("_BaseColor").ToNumericsColorLinear();
			}

			if (material.HasProperty("_MainTex"))
			{
				var mainTex = material.GetTexture("_MainTex");
				if (mainTex)
				{
					pbr.BaseColorTexture = ExportTextureInfo(mainTex, TextureMapType.Main);
					ExportTextureTransform(pbr.BaseColorTexture, material, "_MainTex");
				}
			}

			if (material.HasProperty("_BaseMap"))
			{
				var mainTex = material.GetTexture("_BaseMap");
				if (mainTex)
				{
					pbr.BaseColorTexture = ExportTextureInfo(mainTex, TextureMapType.Main);
					ExportTextureTransform(pbr.BaseColorTexture, material, "_BaseMap");
				}
			}

			def.PbrMetallicRoughness = pbr;

		}

		private void ExportPBRSpecularGlossiness(GLTFMaterial material, Material materialObj)
		{
			if (_root.ExtensionsUsed == null)
			{
				_root.ExtensionsUsed = new List<string>(new[] { "KHR_materials_pbrSpecularGlossiness" });
			}
			else if (!_root.ExtensionsUsed.Contains("KHR_materials_pbrSpecularGlossiness"))
			{
				_root.ExtensionsUsed.Add("KHR_materials_pbrSpecularGlossiness");
			}

			if (RequireExtensions)
			{
				if (_root.ExtensionsRequired == null)
				{
					_root.ExtensionsRequired = new List<string>(new[] { "KHR_materials_pbrSpecularGlossiness" });
				}
				else if (!_root.ExtensionsRequired.Contains("KHR_materials_pbrSpecularGlossiness"))
				{
					_root.ExtensionsRequired.Add("KHR_materials_pbrSpecularGlossiness");
				}
			}

			if (material.Extensions == null)
			{
				material.Extensions = new Dictionary<string, IExtension>();
			}

			GLTF.Math.Color diffuseFactor = KHR_materials_pbrSpecularGlossinessExtension.DIFFUSE_FACTOR_DEFAULT;
			TextureInfo diffuseTexture = KHR_materials_pbrSpecularGlossinessExtension.DIFFUSE_TEXTURE_DEFAULT;
			GLTF.Math.Vector3 specularFactor = KHR_materials_pbrSpecularGlossinessExtension.SPEC_FACTOR_DEFAULT;
			double glossinessFactor = KHR_materials_pbrSpecularGlossinessExtension.GLOSS_FACTOR_DEFAULT;
			TextureInfo specularGlossinessTexture = KHR_materials_pbrSpecularGlossinessExtension.SPECULAR_GLOSSINESS_TEXTURE_DEFAULT;

			if (materialObj.HasProperty("_Color"))
			{
				diffuseFactor = materialObj.GetColor("_Color").ToNumericsColorLinear();
			}

			if (materialObj.HasProperty("_BaseColor"))
			{
				diffuseFactor = materialObj.GetColor("_BaseColor").ToNumericsColorLinear();
			}

			if (materialObj.HasProperty("_MainTex"))
			{
				var mainTex = materialObj.GetTexture("_MainTex");

				if (mainTex != null)
				{
					diffuseTexture = ExportTextureInfo(mainTex, TextureMapType.Main);
					ExportTextureTransform(diffuseTexture, materialObj, "_MainTex");
				}
			}

			if (materialObj.HasProperty("_BaseMap"))
			{
				var mainTex = materialObj.GetTexture("_BaseMap");

				if (mainTex != null)
				{
					diffuseTexture = ExportTextureInfo(mainTex, TextureMapType.Main);
					ExportTextureTransform(diffuseTexture, materialObj, "_BaseMap");
				}
			}

			if (materialObj.HasProperty("_SpecColor"))
			{
				var specGlossMap = materialObj.GetTexture("_SpecGlossMap");
				if (specGlossMap == null)
				{
					var specColor = materialObj.GetColor("_SpecColor").ToNumericsColorLinear();
					specularFactor = new GLTF.Math.Vector3(specColor.R, specColor.G, specColor.B);
				}
			}

			if (materialObj.HasProperty("_Glossiness"))
			{
				var specGlossMap = materialObj.GetTexture("_SpecGlossMap");
				if (specGlossMap == null)
				{
					glossinessFactor = materialObj.GetFloat("_Glossiness");
				}
			}

			if (materialObj.HasProperty("_SpecGlossMap"))
			{
				var mgTex = materialObj.GetTexture("_SpecGlossMap");

				if (mgTex)
				{
					specularGlossinessTexture = ExportTextureInfo(mgTex, TextureMapType.SpecGloss);
					ExportTextureTransform(specularGlossinessTexture, materialObj, "_SpecGlossMap");
				}
			}

			material.Extensions[KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME] = new KHR_materials_pbrSpecularGlossinessExtension(
				diffuseFactor,
				diffuseTexture,
				specularFactor,
				glossinessFactor,
				specularGlossinessTexture
			);
		}

		private MaterialCommonConstant ExportCommonConstant(Material materialObj)
		{
			if (_root.ExtensionsUsed == null)
			{
				_root.ExtensionsUsed = new List<string>(new[] { "KHR_materials_common" });
			}
			else if (!_root.ExtensionsUsed.Contains("KHR_materials_common"))
			{
				_root.ExtensionsUsed.Add("KHR_materials_common");
			}

			if (RequireExtensions)
			{
				if (_root.ExtensionsRequired == null)
				{
					_root.ExtensionsRequired = new List<string>(new[] { "KHR_materials_common" });
				}
				else if (!_root.ExtensionsRequired.Contains("KHR_materials_common"))
				{
					_root.ExtensionsRequired.Add("KHR_materials_common");
				}
			}

			var constant = new MaterialCommonConstant();

			if (materialObj.HasProperty("_AmbientFactor"))
			{
				constant.AmbientFactor = materialObj.GetColor("_AmbientFactor").ToNumericsColorRaw();
			}

			if (materialObj.HasProperty("_LightMap"))
			{
				var lmTex = materialObj.GetTexture("_LightMap");

				if (lmTex)
				{
					constant.LightmapTexture = ExportTextureInfo(lmTex, TextureMapType.Light);
					ExportTextureTransform(constant.LightmapTexture, materialObj, "_LightMap");
				}

			}

			if (materialObj.HasProperty("_LightFactor"))
			{
				constant.LightmapFactor = materialObj.GetColor("_LightFactor").ToNumericsColorRaw();
			}

			return constant;
		}

		public TextureInfo ExportTextureInfo(Texture texture, TextureMapType textureMapType)
		{
			var info = new TextureInfo();

			info.Index = ExportTexture(texture, textureMapType);

			return info;
		}

		public TextureId ExportTexture(Texture textureObj, TextureMapType textureMapType)
		{
			TextureId id = GetTextureId(_root, textureObj);
			if (id != null)
			{
				return id;
			}

			var texture = new GLTFTexture();

			//If texture name not set give it a unique name using count
			if (textureObj.name == "")
			{
				textureObj.name = (_root.Textures.Count + 1).ToString();
			}

			if (ExportNames)
			{
				texture.Name = textureObj.name;
			}

			if (_shouldUseInternalBufferForImages)
		    {
				texture.Source = ExportImageInternalBuffer(textureObj, textureMapType);
		    }
		    else
		    {
				texture.Source = ExportImage(textureObj, textureMapType);
		    }
			texture.Sampler = ExportSampler(textureObj);

			_textures.Add(textureObj);

			id = new TextureId
			{
				Id = _root.Textures.Count,
				Root = _root
			};

			_root.Textures.Add(texture);

			return id;
		}

		/// <summary>
		/// Used for determining the resulting file output path for external images (not internal buffer).
		/// Depends on various factors such as alpha channel, texture map type / conversion needs, and existing data on disk.
		/// Logic needs to match the one in ExportImageInternalBuffer.
		/// The actual export happens in ExportImages.
		/// </summary>
		/// <returns>The relative texture output path on disk, including extension</returns>
		private string GetImageOutputPath(Texture texture, TextureMapType textureMapType, out bool ableToExportFromDisk)
		{
			var imagePath = _exportOptions.TexturePathRetriever(texture);
			if (string.IsNullOrEmpty(imagePath))
			{
				imagePath = texture.name;
			}

			ableToExportFromDisk = false;
			bool textureHasAlpha = true;

			if (TryExportTexturesFromDisk && CanGetTextureDataFromDisk(textureMapType, texture, out string path))
			{
				if (IsPng(path) || IsJpeg(path))
				{
					imagePath = path;
					ableToExportFromDisk = true;
				}
			}

			switch (textureMapType)
			{
				case TextureMapType.MetallicGloss:
					textureHasAlpha = false;
					break;
				case TextureMapType.MetallicGloss_DontConvert:
				case TextureMapType.Light:
				case TextureMapType.Occlusion:
					textureHasAlpha = false;
					break;
				case TextureMapType.Bump:
					textureHasAlpha = false;
					break;
				default:
					textureHasAlpha = TextureHasAlphaChannel(texture);
					break;
			}

			var canExportAsJpeg = !textureHasAlpha && settings.UseTextureFileTypeHeuristic;
			var desiredExtension = canExportAsJpeg ? ".jpg" : ".png";

			if (!ExportFullPath)
			{
				imagePath = Path.GetFileName(imagePath);
			}

			if (!ableToExportFromDisk)
			{
				imagePath = Path.ChangeExtension(imagePath, desiredExtension);
			}

			return imagePath;
		}

		private ImageId ExportImage(Texture texture, TextureMapType textureMapType)
		{
			ImageId id = GetImageId(_root, texture);
			if (id != null)
			{
				return id;
			}

			var image = new GLTFImage();

			if (ExportNames)
			{
				image.Name = texture.name;
			}

            if (texture.GetType() == typeof(RenderTexture))
            {
                Texture2D tempTexture = new Texture2D(texture.width, texture.height);
                tempTexture.name = texture.name;

                RenderTexture.active = texture as RenderTexture;
                tempTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                tempTexture.Apply();
                texture = tempTexture;
            }

#if UNITY_2017_1_OR_NEWER
            if (texture.GetType() == typeof(CustomRenderTexture))
            {
                Texture2D tempTexture = new Texture2D(texture.width, texture.height);
                tempTexture.name = texture.name;

                RenderTexture.active = texture as CustomRenderTexture;
                tempTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                tempTexture.Apply();
                texture = tempTexture;
            }
#endif

			var filenamePath = GetImageOutputPath(texture, textureMapType, out var canBeExportedFromDisk);

			// some characters such as # are allowed as part of an URI and are thus not escaped
			// by EscapeUriString. They need to be escaped if they're part of the filename though.
			image.Uri = Path.Combine(
				Uri.EscapeUriString(Path.GetDirectoryName(filenamePath).Replace("\\","/")),
				Uri.EscapeDataString(Path.GetFileName(filenamePath))
			).Replace("\\","/");

            _imageInfos.Add(new ImageInfo
			{
				texture = texture as Texture2D,
				textureMapType = textureMapType,
				outputPath = filenamePath,
				canBeExportedFromDisk = canBeExportedFromDisk,
			});

            id = new ImageId
			{
				Id = _root.Images.Count,
				Root = _root
			};

			_root.Images.Add(image);

			return id;
		}

		bool CanGetTextureDataFromDisk(TextureMapType textureMapType, Texture texture, out string path)
		{
			path = null;

#if UNITY_EDITOR
			if (Application.isEditor && UnityEditor.AssetDatabase.Contains(texture))
			{
				path = UnityEditor.AssetDatabase.GetAssetPath(texture);
				var importer = AssetImporter.GetAtPath(path) as TextureImporter;
				if (importer?.textureShape != TextureImporterShape.Texture2D)
					return false;

				switch (textureMapType)
				{
					// if this is a normal map generated from greyscale, we shouldn't attempt to export from disk
					case TextureMapType.Bump:
						if (importer && importer.textureType == TextureImporterType.NormalMap && importer.convertToNormalmap)
							return false;
						break;
					// check if the texture contains an alpha channel; if yes, we shouldn't attempt to export from disk but instead convert.
					case TextureMapType.MetallicGloss:
					case TextureMapType.SpecGloss:
						if (importer && importer.DoesSourceTextureHaveAlpha())
							return false;
						break;
				}

				if (File.Exists(path))
				{
					if(AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(Texture2D))
					{
						var ext = Path.GetExtension(path);
						path = path.Replace(ext, "-" + texture.name + ext);
					}
					return true;
				}
			}
#endif
			return false;
		}

		private byte[] GetTextureDataFromDisk(Texture texture)
		{
#if UNITY_EDITOR
			var path = UnityEditor.AssetDatabase.GetAssetPath(texture);

			if (File.Exists(path))
				return File.ReadAllBytes(path);
#endif
			return null;
		}

		bool TextureHasAlphaChannel(Texture sourceTexture)
		{
			var hasAlpha = false;

#if UNITY_EDITOR
			if (AssetDatabase.Contains(sourceTexture) && sourceTexture is Texture2D)
			{
				var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sourceTexture)) as TextureImporter;
				if (importer)
				{
					switch (importer.alphaSource)
					{
						case TextureImporterAlphaSource.FromInput:
							hasAlpha = importer.DoesSourceTextureHaveAlpha();
							break;
						case TextureImporterAlphaSource.FromGrayScale:
							hasAlpha = true;
							break;
						case TextureImporterAlphaSource.None:
							hasAlpha = false;
							break;
					}
				}
			}
#endif

			UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
#if !UNITY_2019_1_OR_NEWER
			if (sourceTexture is Texture2D tex2D)
				graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetGraphicsFormat(tex2D.format, true);
#else
			graphicsFormat = sourceTexture.graphicsFormat;
#endif
#if UNITY_2018_2_OR_NEWER
			if(graphicsFormat != UnityEngine.Experimental.Rendering.GraphicsFormat.None)
				hasAlpha |= UnityEngine.Experimental.Rendering.GraphicsFormatUtility.HasAlphaChannel(graphicsFormat);
#else
			hasAlpha = true;
#endif
			return hasAlpha;
		}

		private bool IsPng(string filename)
		{
			return Path.GetExtension(filename).EndsWith("png", StringComparison.InvariantCultureIgnoreCase);
		}

		private bool IsJpeg(string filename)
		{
			return Path.GetExtension(filename).EndsWith("jpg", StringComparison.InvariantCultureIgnoreCase) || Path.GetExtension(filename).EndsWith("jpeg", StringComparison.InvariantCultureIgnoreCase);
		}

#if UNITY_EDITOR
		private bool TryGetImporter<T>(Object obj, out T importer) where T : AssetImporter
		{
			if (EditorUtility.IsPersistent(obj))
			{
				var texturePath = AssetDatabase.GetAssetPath(obj);
				importer = AssetImporter.GetAtPath(texturePath) as T;
				return importer;
			}
			importer = null;
			return false;
		}
#endif

		private ImageId ExportImageInternalBuffer(UnityEngine.Texture texture, TextureMapType textureMapType)
		{
			const string PNGMimeType = "image/png";
			const string JPEGMimeType = "image/jpeg";

			if (texture == null)
		    {
				throw new NullReferenceException("texture can not be NULL.");
		    }

		    var image = new GLTFImage();
		    image.MimeType = PNGMimeType;

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			bool wasAbleToExportFromDisk = false;
			bool textureHasAlpha = true;

			if (TryExportTexturesFromDisk && CanGetTextureDataFromDisk(textureMapType, texture, out string path))
			{
				if (IsPng(path))
				{
					image.MimeType = PNGMimeType;
					var imageBytes = GetTextureDataFromDisk(texture);
					_bufferWriter.Write(imageBytes);
					wasAbleToExportFromDisk = true;
				}
				else if (IsJpeg(path))
				{
					image.MimeType = JPEGMimeType;
					var imageBytes = GetTextureDataFromDisk(texture);
					_bufferWriter.Write(imageBytes);
					wasAbleToExportFromDisk = true;
				}
				else
				{
					Debug.Log("Texture can't be exported from disk: " + path + ". Only PNG & JPEG are supported. The texture will be re-encoded as PNG.", texture);
				}
			}

			if (!wasAbleToExportFromDisk)
		    {
				var sRGB = true;

#if UNITY_EDITOR
				if (textureMapType == TextureMapType.Custom_Unknown)
				{
#if UNITY_EDITOR
					if (TryGetImporter<TextureImporter>(texture, out var importer))
					{
						if (!importer.sRGBTexture)
							sRGB = false;
						else
							sRGB = true;
					}
#endif
				}
#endif

				var format = sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;

				// TODO we could make sure texture size is power-of-two here
				var destRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 24, RenderTextureFormat.ARGB32, format);
				GL.sRGBWrite = sRGB;

				switch (textureMapType)
				{
					case TextureMapType.MetallicGloss:
						Graphics.Blit(texture, destRenderTexture, _metalGlossChannelSwapMaterial);
						textureHasAlpha = false;
						break;
					case TextureMapType.MetallicGloss_DontConvert:
					case TextureMapType.Light:
					case TextureMapType.Occlusion:
						GL.sRGBWrite = false; // seems we need to convert here, otherwise color space is wrong
						Graphics.Blit(texture, destRenderTexture);
						textureHasAlpha = false;
						break;
					case TextureMapType.Bump:
						Graphics.Blit(texture, destRenderTexture, _normalChannelMaterial);
						textureHasAlpha = false;
						break;
					default:
						Graphics.Blit(texture, destRenderTexture);
						textureHasAlpha = TextureHasAlphaChannel(texture);
						break;
				}

				var exportTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
				exportTexture.ReadPixels(new Rect(0, 0, destRenderTexture.width, destRenderTexture.height), 0, 0);
				exportTexture.Apply();

				var canExportAsJpeg = !textureHasAlpha && settings.UseTextureFileTypeHeuristic;
				var imageData = canExportAsJpeg ? exportTexture.EncodeToJPG(settings.DefaultJpegQuality) : exportTexture.EncodeToPNG();
				image.MimeType = canExportAsJpeg ? JPEGMimeType : PNGMimeType;
				_bufferWriter.Write(imageData);

				RenderTexture.ReleaseTemporary(destRenderTexture);

				GL.sRGBWrite = false;
				if (Application.isEditor)
				{
					UnityEngine.Object.DestroyImmediate(exportTexture);
				}
				else
				{
					UnityEngine.Object.Destroy(exportTexture);
				}
		    }

			// // Check for potential warnings in GLTF validation
			// if (!Mathf.IsPowerOfTwo(texture.width) || !Mathf.IsPowerOfTwo(texture.height))
			// {
			// 	Debug.LogWarning("Validation Warning: " + "Image has non-power-of-two dimensions: " + texture.width + "x" + texture.height + ".", texture);
			// }

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);
			image.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

		    var id = new ImageId
		    {
				Id = _root.Images.Count,
				Root = _root
		    };
		    _root.Images.Add(image);

		    return id;
		}

		private SamplerId ExportSampler(Texture texture)
		{
			var samplerId = GetSamplerId(_root, texture);
			if (samplerId != null)
				return samplerId;

			var sampler = new Sampler();

			switch (texture.wrapMode)
			{
				case TextureWrapMode.Clamp:
					sampler.WrapS = WrapMode.ClampToEdge;
					sampler.WrapT = WrapMode.ClampToEdge;
					break;
				case TextureWrapMode.Repeat:
					sampler.WrapS = WrapMode.Repeat;
					sampler.WrapT = WrapMode.Repeat;
					break;
				case TextureWrapMode.Mirror:
					sampler.WrapS = WrapMode.MirroredRepeat;
					sampler.WrapT = WrapMode.MirroredRepeat;
					break;
				default:
					Debug.LogWarning("Unsupported Texture.wrapMode: " + texture.wrapMode, texture);
					sampler.WrapS = WrapMode.Repeat;
					sampler.WrapT = WrapMode.Repeat;
					break;
			}

			var mipmapCount = 1;
#if UNITY_2019_2_OR_NEWER
			mipmapCount = texture.mipmapCount;
#else
			if (texture is Texture2D tex2D) mipmapCount = tex2D.mipmapCount;
#endif
			if(mipmapCount > 1)
			{
				switch (texture.filterMode)
				{
					case FilterMode.Point:
						sampler.MinFilter = MinFilterMode.NearestMipmapNearest;
						sampler.MagFilter = MagFilterMode.Nearest;
						break;
					case FilterMode.Bilinear:
						sampler.MinFilter = MinFilterMode.LinearMipmapNearest;
						sampler.MagFilter = MagFilterMode.Linear;
						break;
					case FilterMode.Trilinear:
						sampler.MinFilter = MinFilterMode.LinearMipmapLinear;
						sampler.MagFilter = MagFilterMode.Linear;
						break;
					default:
						Debug.LogWarning("Unsupported Texture.filterMode: " + texture.filterMode, texture);
						sampler.MinFilter = MinFilterMode.LinearMipmapLinear;
						sampler.MagFilter = MagFilterMode.Linear;
						break;
				}
			}
			else
			{
				sampler.MinFilter = MinFilterMode.None;
				sampler.MagFilter = MagFilterMode.None;
			}

			samplerId = new SamplerId
			{
				Id = _root.Samplers.Count,
				Root = _root
			};

			_root.Samplers.Add(sampler);

			return samplerId;
		}

#region Accessors Export

		private AccessorId ExportAccessor(byte[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorByteArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.SCALAR;

			int min = arr[0];
			int max = arr[0];

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur < min)
				{
					min = cur;
				}
				if (cur > max)
				{
					max = cur;
				}
			}

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			accessor.ComponentType = GLTFComponentType.UnsignedByte;

			foreach (var v in arr)
			{
				_bufferWriter.Write((byte)v);
			}

			accessor.Min = new List<double> { min };
			accessor.Max = new List<double> { max };

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorByteArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(float[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorFloatArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.SCALAR;

			exportAccessorMinMaxMarker.Begin();
			var min = arr[0];
			var max = arr[0];

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur < min)
				{
					min = cur;
				}
				if (cur > max)
				{
					max = cur;
				}
			}
			exportAccessorMinMaxMarker.End();

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, sizeof(float));

			exportAccessorBufferWriteMarker.Begin();
			accessor.ComponentType = GLTFComponentType.Float;

#if USE_FAST_BINARY_WRITER
			_bufferWriter.Write(arr);
#else
			foreach (var v in arr)
			{
				_bufferWriter.Write((float)v);
			}
#endif
			exportAccessorBufferWriteMarker.End();

			accessor.Min = new List<double> { min };
			accessor.Max = new List<double> { max };

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorFloatArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(int[] arr, bool isIndices = false)
		{
			exportAccessorMarker.Begin();
			if (isIndices) exportAccessorIntArrayIndicesMarker.Begin(); else exportAccessorIntArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.SCALAR;

			int min = arr[0];
			int max = arr[0];

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur < min)
				{
					min = cur;
				}
				if (cur > max)
				{
					max = cur;
				}
			}

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			// From the spec:
			// Values of the index accessor must not include the maximum value for the given component type,
			// which triggers primitive restart in several graphics APIs and would require client implementations to rebuild the index buffer.
			// Primitive restart values are disallowed and all index values must refer to actual vertices.
			int maxAllowedValue = isIndices ? max + 1 : max;

			if (maxAllowedValue <= byte.MaxValue && min >= byte.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedByte;

				foreach (var v in arr)
				{
					_bufferWriter.Write((byte)v);
				}
			}
			else if (maxAllowedValue <= sbyte.MaxValue && min >= sbyte.MinValue && !isIndices)
			{
				accessor.ComponentType = GLTFComponentType.Byte;

				foreach (var v in arr)
				{
					_bufferWriter.Write((sbyte)v);
				}
			}
			else if (maxAllowedValue <= short.MaxValue && min >= short.MinValue && !isIndices)
			{
				accessor.ComponentType = GLTFComponentType.Short;

				foreach (var v in arr)
				{
					_bufferWriter.Write((short)v);
				}
			}
			else if (maxAllowedValue <= ushort.MaxValue && min >= ushort.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedShort;

				foreach (var v in arr)
				{
					_bufferWriter.Write((ushort)v);
				}
			}
			else if (maxAllowedValue >= uint.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedInt;

				foreach (var v in arr)
				{
					_bufferWriter.Write((uint)v);
				}
			}
			else
			{
				accessor.ComponentType = GLTFComponentType.Float;

				foreach (var v in arr)
				{
					_bufferWriter.Write((float)v);
				}
			}

			accessor.Min = new List<double> { min };
			accessor.Max = new List<double> { max };

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			if (isIndices) exportAccessorIntArrayIndicesMarker.End(); else exportAccessorIntArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(Vector2[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorVector2ArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC2;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float maxX = arr[0].x;
			float maxY = arr[0].y;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
			}

			accessor.Min = new List<double> { minX, minY };
			accessor.Max = new List<double> { maxX, maxY };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			foreach (var vec in arr)
			{
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
			}

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorVector2ArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(Vector3[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorVector3ArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC3;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float minZ = arr[0].z;
			float maxX = arr[0].x;
			float maxY = arr[0].y;
			float maxZ = arr[0].z;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ };
			accessor.Max = new List<double> { maxX, maxY, maxZ };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
#if USE_FAST_BINARY_WRITER
			_bufferWriter.Write(arr);
#else
			foreach (var vec in arr)
			{
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
			}
#endif
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength, sizeof(float) * 3);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorVector3ArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="baseAccessor"></param>
		/// <param name="baseData">The data is treated as "additive" (e.g. blendshapes) when baseData != null</param>
		/// <param name="arr"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private AccessorId ExportSparseAccessor(AccessorId baseAccessor, Vector3[] baseData, Vector3[] arr)
		{
			exportSparseAccessorMarker.Begin();

			uint dataCount = (uint) arr.Length;
			if (dataCount == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			// TODO need to assert that these types match to the base accessor
			// TODO we might need to build a data <> accessor dict as well to avoid having to pass in the base data again

			// need to assert data and baseData have the same length etc.
			if (baseData != null && baseData.Length != arr.Length)
			{
				throw new Exception("Sparse Accessor Base Data must either be null or the same length as the data array, current: " + baseData.Length + " != " + arr.Length);
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = dataCount;
			accessor.Type = GLTFAccessorAttributeType.VEC3;

			if(baseAccessor != null)
			{
				accessor.BufferView = baseAccessor.Value.BufferView;
				accessor.ByteOffset = baseAccessor.Value.ByteOffset;
			}

			accessor.Sparse = new AccessorSparse();
			var sparse = accessor.Sparse;

			var indices = new List<int>();

			// Debug.Log("Values for sparse data array:\n " + string.Join("\n ", arr));
			for (int i = 0; i < arr.Length; i++)
			{
				var comparer = (baseAccessor == null || baseData == null) ? Vector3.zero : baseData[i];
				if (comparer != arr[i])
				{
					indices.Add(i);
				}
			}

			// HACK since GLTF doesn't allow 0 buffer length, but that can well happen when a morph target exactly matches the base mesh
			// NOT doing this results in GLTF validation errors about buffers having length 0
			if (indices.Count < 1)
			{
				indices = new List<int>() {0};
			}

			// we need to calculate the min/max of the entire new data array
			uint count = (uint) arr.Length;

			var firstElement = baseData != null ? (baseData[0] + arr[0]) : arr[0];
			float minX = firstElement.x;
			float minY = firstElement.y;
			float minZ = firstElement.z;
			float maxX = firstElement.x;
			float maxY = firstElement.y;
			float maxZ = firstElement.z;

			for (var i = 1; i < count; i++)
			{
				var cur = baseData != null ? (baseData[i] + arr[i]) : arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ };
			accessor.Max = new List<double> { maxX, maxY, maxZ };

			// write indices
			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffsetIndices = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			// Debug.Log("Storing " + indices.Count + " sparse indices + values");

			foreach (var index in indices)
			{
				_bufferWriter.Write(index);
			}

			uint byteLengthIndices = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffsetIndices, 4);

			sparse.Indices = new AccessorSparseIndices();
			// TODO should be properly using the smallest possible component type
			sparse.Indices.ComponentType = GLTFComponentType.UnsignedInt;
			sparse.Indices.BufferView = ExportBufferView(byteOffsetIndices, byteLengthIndices);

			// write values
			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
			foreach (var i in indices)
			{
				var vec = arr[i];
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
			}
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			sparse.Values = new AccessorSparseValues();
			sparse.Values.BufferView = ExportBufferView(byteOffset, byteLength);

			sparse.Count = indices.Count;

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportSparseAccessorMarker.End();

			return id;
		}


		private AccessorId ExportAccessor(Vector4[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorVector4ArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float minZ = arr[0].z;
			float minW = arr[0].w;
			float maxX = arr[0].x;
			float maxY = arr[0].y;
			float maxZ = arr[0].z;
			float maxW = arr[0].w;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.w < minW)
				{
					minW = cur.w;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
				if (cur.w > maxW)
				{
					maxW = cur.w;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ, minW };
			accessor.Max = new List<double> { maxX, maxY, maxZ, maxW };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
#if USE_FAST_BINARY_WRITER
			_bufferWriter.Write(arr);
#else
			foreach (var vec in arr)
			{
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
				_bufferWriter.Write(vec.w);
			}
#endif
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorVector4ArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(Color[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorColorArrayMarker.Begin();

			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			float minR = arr[0].r;
			float minG = arr[0].g;
			float minB = arr[0].b;
			float minA = arr[0].a;
			float maxR = arr[0].r;
			float maxG = arr[0].g;
			float maxB = arr[0].b;
			float maxA = arr[0].a;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.r < minR)
				{
					minR = cur.r;
				}
				if (cur.g < minG)
				{
					minG = cur.g;
				}
				if (cur.b < minB)
				{
					minB = cur.b;
				}
				if (cur.a < minA)
				{
					minA = cur.a;
				}
				if (cur.r > maxR)
				{
					maxR = cur.r;
				}
				if (cur.g > maxG)
				{
					maxG = cur.g;
				}
				if (cur.b > maxB)
				{
					maxB = cur.b;
				}
				if (cur.a > maxA)
				{
					maxA = cur.a;
				}
			}

			accessor.Min = new List<double> { minR, minG, minB, minA };
			accessor.Max = new List<double> { maxR, maxG, maxB, maxA };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			foreach (var color in arr)
			{
				_bufferWriter.Write(color.r);
				_bufferWriter.Write(color.g);
				_bufferWriter.Write(color.b);
				_bufferWriter.Write(color.a);
			}

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorColorArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private BufferViewId ExportBufferView(uint byteOffset, uint byteLength, uint byteStride = 0)
		{
			var bufferView = new BufferView
			{
				Buffer = _bufferId,
				ByteOffset = byteOffset,
				ByteLength = byteLength,
				ByteStride = byteStride
			};

			var id = new BufferViewId
			{
				Id = _root.BufferViews.Count,
				Root = _root
			};

			_root.BufferViews.Add(bufferView);

			return id;
		}

#endregion


		public int GetAnimationId(AnimationClip clip)
		{
			return _animationClips.IndexOf(clip);
		}

		public MaterialId GetMaterialId(GLTFRoot root, Material materialObj)
		{
			if (_materials.TryGetValue(materialObj, out var id))
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
				if (_textures[i] == textureObj)
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

		public Texture GetTexture(int id) => _textures[id];

		public ImageId GetImageId(GLTFRoot root, Texture imageObj)
		{
			for (var i = 0; i < _imageInfos.Count; i++)
			{
				if (_imageInfos[i].texture == imageObj)
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
			for (var i = 0; i < root.Samplers.Count; i++)
			{
				bool filterIsNearest = root.Samplers[i].MinFilter == MinFilterMode.Nearest
					|| root.Samplers[i].MinFilter == MinFilterMode.NearestMipmapNearest
					|| root.Samplers[i].MinFilter == MinFilterMode.LinearMipmapNearest;

				bool filterIsLinear = root.Samplers[i].MinFilter == MinFilterMode.Linear
					|| root.Samplers[i].MinFilter == MinFilterMode.NearestMipmapLinear;

				bool filterMatched = textureObj.filterMode == FilterMode.Point && filterIsNearest
					|| textureObj.filterMode == FilterMode.Bilinear && filterIsLinear
					|| textureObj.filterMode == FilterMode.Trilinear && root.Samplers[i].MinFilter == MinFilterMode.LinearMipmapLinear;

				bool wrapMatched = textureObj.wrapMode == TextureWrapMode.Clamp && root.Samplers[i].WrapS == WrapMode.ClampToEdge
					|| textureObj.wrapMode == TextureWrapMode.Repeat && root.Samplers[i].WrapS != WrapMode.ClampToEdge;

				if (filterMatched && wrapMatched)
				{
					return new SamplerId
					{
						Id = i,
						Root = root
					};
				}
			}

			return null;
		}

		protected static DrawMode GetDrawMode(MeshTopology topology)
		{
			switch (topology)
			{
				case MeshTopology.Points: return DrawMode.Points;
				case MeshTopology.Lines: return DrawMode.Lines;
				case MeshTopology.LineStrip: return DrawMode.LineStrip;
				case MeshTopology.Triangles: return DrawMode.Triangles;
			}

			throw new Exception("glTF does not support Unity mesh topology: " + topology);
		}

		// Parses Animation/Animator component and generate a glTF animation for the active clip
		// This may need additional work to fully support animatorControllers
		public void ExportAnimationFromNode(ref Transform transform)
		{
			exportAnimationFromNodeMarker.Begin();

#if ANIMATION_SUPPORTED
			Animator animator = transform.GetComponent<Animator>();
			if (animator)
			{
#if ANIMATION_EXPORT_SUPPORTED
                AnimationClip[] clips = AnimationUtility.GetAnimationClips(transform.gameObject);
                var animatorController = animator.runtimeAnimatorController as AnimatorController;
				// Debug.Log("animator: " + animator + "=> " + animatorController);
                ExportAnimationClips(transform, clips, animator, animatorController);
#endif
			}

			UnityEngine.Animation animation = transform.GetComponent<UnityEngine.Animation>();
			if (animation)
			{
#if ANIMATION_EXPORT_SUPPORTED
                AnimationClip[] clips = UnityEditor.AnimationUtility.GetAnimationClips(transform.gameObject);
                ExportAnimationClips(transform, clips);
#endif
			}
#endif
			exportAnimationFromNodeMarker.End();
		}

#if ANIMATION_EXPORT_SUPPORTED
		private IEnumerable<AnimatorState> GetAnimatorStateParametersForClip(AnimationClip clip, AnimatorController animatorController)
		{
			if (!clip)
				yield break;

			if (!animatorController)
				yield return new AnimatorState() { name = clip.name, speed = 1f };

			foreach (var layer in animatorController.layers)
			{
				foreach (var state in layer.stateMachine.states)
				{
					// find a matching clip in the animator
					if (state.state.motion is AnimationClip c && c == clip)
					{
						yield return state.state;
					}
				}
			}
		}

		private GLTFAnimation GetOrCreateAnimation(AnimationClip clip, string searchForDuplicateName, float speed)
		{
			var existingAnim = default(GLTFAnimation);
			if (_exportOptions.MergeClipsWithMatchingNames)
			{
				// Check if we already exported an animation with exactly that name. If yes, we want to append to the previous one instead of making a new one.
				// This allows to merge multiple animations into one if required (e.g. a character and an instrument that should play at the same time but have individual clips).
				existingAnim = _root.Animations?.FirstOrDefault(x => x.Name == searchForDuplicateName);
			}

			// TODO when multiple AnimationClips are exported, we're currently not properly merging those;
			// we should only export the GLTFAnimation once but then apply that to all nodes that require it (duplicating the animation but not the accessors)
			// instead of naively writing over the GLTFAnimation with the same data.
			var animationClipAndSpeed = (clip, speed);
			if (existingAnim == null)
			{
				if(_clipToAnimation.TryGetValue(animationClipAndSpeed, out existingAnim))
				{
					// we duplicate the clip it was exported before so we can retarget to another transform.
					existingAnim = new GLTFAnimation(existingAnim, _root);
				}
			}

			GLTFAnimation anim = existingAnim != null ? existingAnim : new GLTFAnimation();

			// add to set of already exported clip-state pairs
			if (!_clipToAnimation.ContainsKey(animationClipAndSpeed))
				_clipToAnimation.Add(animationClipAndSpeed, anim);

			return anim;
		}

		// Creates GLTFAnimation for each clip and adds it to the _root
		public void ExportAnimationClips(Transform nodeTransform, IList<AnimationClip> clips, Animator animator = null, AnimatorController animatorController = null)
		{
			// Debug.Log("exporting clips from " + nodeTransform + " with " + animatorController);
			if (animatorController)
			{
				if (!animator) throw new ArgumentNullException("Missing " + nameof(animator));
				for (int i = 0; i < clips.Count; i++)
				{
					if (!clips[i]) continue;

					// special case: there could be multiple states with the same animation clip.
					// if we want to handle this here, we need to find all states that match this clip
					foreach(var state in GetAnimatorStateParametersForClip(clips[i], animatorController))
					{
						var speed = state.speed * (state.speedParameterActive ? animator.GetFloat(state.speedParameter) : 1f);
						var name = clips[i].name;
						ExportAnimationClip(clips[i], name, nodeTransform, speed);
					}
				}
			}
			else
			{
				for (int i = 0; i < clips.Count; i++)
				{
					if (!clips[i]) continue;
					var speed = 1f;
					ExportAnimationClip(clips[i], clips[i].name, nodeTransform, speed);
				}
			}
		}

		public GLTFAnimation ExportAnimationClip(AnimationClip clip, string name, Transform node, float speed)
		{
			if (!clip) return null;
			GLTFAnimation anim = GetOrCreateAnimation(clip, name, speed);

			anim.Name = name;
			if(settings.UniqueAnimationNames)
				anim.Name = ObjectNames.GetUniqueName(_root.Animations.Select(x => x.Name).ToArray(), anim.Name);

			ConvertClipToGLTFAnimation(clip, node, anim, speed);

			if (anim.Channels.Count > 0 && anim.Samplers.Count > 0 && !_root.Animations.Contains(anim))
			{
				_root.Animations.Add(anim);
				_animationClips.Add(clip);
			}
			return anim;
		}
#endif

#if ANIMATION_SUPPORTED
		public enum AnimationKeyRotationType
		{
			Unknown,
			Quaternion,
			Euler
		}

		public class PropertyCurve
		{
			public string propertyName;
			public Type propertyType;
			public List<AnimationCurve> curve;
			public Object target;

			public PropertyCurve(Object target, EditorCurveBinding binding)
			{
				this.target = target;
				this.propertyName = binding.propertyName;
				curve = new List<AnimationCurve>();
			}

			public float Evaluate(float time, int index)
			{
				return curve[index].Evaluate(time);
			}
		}

		private struct TargetCurveSet
		{
			#pragma warning disable 0649
			public AnimationCurve[] translationCurves;
			public AnimationCurve[] rotationCurves;
			public AnimationCurve[] scaleCurves;
			public AnimationKeyRotationType rotationType;
			public Dictionary<string, AnimationCurve> weightCurves;
			public PropertyCurve propertyCurve;
			#pragma warning restore

			public Dictionary<string, PropertyCurve> propertyCurves;

			// for KHR_animation_pointer
			public void AddPropertyCurves(Object animatedObject, AnimationCurve curve, EditorCurveBinding binding)
			{
				if (propertyCurves == null) propertyCurves = new Dictionary<string, PropertyCurve>();
				if (!binding.propertyName.Contains("."))
				{
					var prop = new PropertyCurve(animatedObject, binding);
					prop.curve.Add(curve);
					propertyCurves.Add(binding.propertyName, prop);
				}
				else
				{
					var memberName = binding.propertyName;

					// Color is animated as a Color/Vector4
					if (memberName.EndsWith(".r", StringComparison.Ordinal) || memberName.EndsWith(".g", StringComparison.Ordinal) ||
					    memberName.EndsWith(".b", StringComparison.Ordinal) || memberName.EndsWith(".a", StringComparison.Ordinal) ||
					    memberName.EndsWith(".x", StringComparison.Ordinal) || memberName.EndsWith(".y", StringComparison.Ordinal) ||
					    memberName.EndsWith(".z", StringComparison.Ordinal) || memberName.EndsWith(".w", StringComparison.Ordinal))
						memberName = binding.propertyName.Substring(0, binding.propertyName.LastIndexOf(".", StringComparison.Ordinal));

					if (propertyCurves.TryGetValue(memberName, out var existing))
					{
						existing.curve.Add(curve);
					}
					else
					{
						var prop = new PropertyCurve(animatedObject, binding);
						prop.propertyName = memberName;
						prop.curve.Add(curve);
						propertyCurves.Add(memberName, prop);
						if (memberName.StartsWith("material.") && animatedObject is Renderer rend)
						{
							var mat = rend.sharedMaterial;
							if (!mat)
							{
								Debug.LogWarning("Animated missing material?", animatedObject);
							}
							memberName = memberName.Substring("material.".Length);
							prop.propertyName = memberName;
							prop.target = mat;
							if (memberName.EndsWith("_ST", StringComparison.Ordinal))
							{
								prop.propertyType = typeof(Vector4);
							}
							else
							{
								var found = false;
								for (var i = 0; i < ShaderUtil.GetPropertyCount(mat.shader); i++)
								{
									if (found) break;
									var name = ShaderUtil.GetPropertyName(mat.shader, i);
									if (!memberName.EndsWith(name)) continue;
									found = true;
									var materialProperty = ShaderUtil.GetPropertyType(mat.shader, i);
									switch (materialProperty)
									{
										case ShaderUtil.ShaderPropertyType.Color:
											prop.propertyType = typeof(Color);
											break;
										case ShaderUtil.ShaderPropertyType.Vector:
											prop.propertyType = typeof(Vector4);
											break;
										case ShaderUtil.ShaderPropertyType.Float:
											prop.propertyType = typeof(float);
											prop.propertyType = typeof(float);
											break;
										case ShaderUtil.ShaderPropertyType.TexEnv:
											prop.propertyType = typeof(Texture);
											break;
									}
								}
							}
						}
						else if (animatedObject is Light)
						{
							switch (memberName)
							{
								case "m_Color":
									prop.propertyType = typeof(Color);
									break;
								case "m_Intensity":
									prop.propertyType = typeof(float);
									break;
							}
						}
						else
						{
							var member = binding.type
								.GetMember(prop.propertyName, BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
								.FirstOrDefault();
							if (member is FieldInfo field) prop.propertyType = field.FieldType;
							else if (member is PropertyInfo p) prop.propertyType = p.PropertyType;
						}
					}
				}
			}

			public void Init()
			{
				translationCurves = new AnimationCurve[3];
				rotationCurves = new AnimationCurve[4];
				scaleCurves = new AnimationCurve[3];
				weightCurves = new Dictionary<string, AnimationCurve>();
			}
		}

		private void ConvertClipToGLTFAnimation(AnimationClip clip, Transform transform, GLTFAnimation animation, float speed)
		{
			convertClipToGLTFAnimationMarker.Begin();

			// Generate GLTF.Schema.AnimationChannel and GLTF.Schema.AnimationSampler
			// 1 channel per node T/R/S, one sampler per node T/R/S
			// Need to keep a list of nodes to convert to indexes

			// 1. browse clip, collect all curves and create a TargetCurveSet for each target
			Dictionary<string, TargetCurveSet> targetCurvesBinding = new Dictionary<string, TargetCurveSet>();
			CollectClipCurves(transform.gameObject, clip, targetCurvesBinding);

			// Baking needs all properties, fill missing curves with transform data in 2 keyframes (start, endTime)
			// where endTime is clip duration
			// Note: we should avoid creating curves for a property if none of it's components is animated

			GenerateMissingCurves(clip.length, transform, ref targetCurvesBinding);

			if (BakeAnimationData)
			{
				// Bake animation for all animated nodes
				foreach (string target in targetCurvesBinding.Keys)
				{
					var hadAlreadyExportedThisBindingBefore = _clipAndSpeedAndPathToExportedTransform.TryGetValue((clip, speed, target), out var alreadyExportedTransform);
					Transform targetTr = target.Length > 0 ? transform.Find(target) : transform;
					int newTargetId = targetTr ? GetAnimationTargetIdFromTransform(targetTr) : -1;

					if (hadAlreadyExportedThisBindingBefore && newTargetId < 0)
					{
						// warn: the transform for this binding exists, but its Node isn't exported. It's probably disabled and "Export Disabled" is off.
						if (targetTr)
						{
							Debug.LogWarning("An animated transform is not part of _exportedTransforms, is the object disabled? " + targetTr.name + " (InstanceID: " + targetTr.GetInstanceID() + ")", targetTr);
						}

						// we need to remove the channels and samplers from the existing animation that was passed in if they exist
						int alreadyExportedChannelTargetId = GetAnimationTargetIdFromTransform(alreadyExportedTransform);
						animation.Channels.RemoveAll(x => x.Target.Node.Id == alreadyExportedChannelTargetId);

						// TODO remove all samplers from this animation that were targeting the channels that we just removed
						// var remainingSamplers = new HashSet<int>();
						// foreach (var c in animation.Channels)
						// {
						// 	remainingSamplers.Add(c.Sampler.Id);
						// }
						//
						// for (int i = animation.Samplers.Count - 1; i >= 0; i--)
						// {
						// 	if (!remainingSamplers.Contains(i))
						// 	{
						// 		animation.Samplers.RemoveAt(i);
						// 		// TODO: this doesn't work because we're punching holes in the sampler order; all channel sampler IDs would need to be adjusted as well.
						// 	}
						// }

						continue;
					}

					if (!targetTr)
						continue;

					if (hadAlreadyExportedThisBindingBefore && targetTr)
					{
						int alreadyExportedChannelTargetId = GetAnimationTargetIdFromTransform(alreadyExportedTransform);

						for (int i = 0; i < animation.Channels.Count; i++)
						{
							var existingTarget = animation.Channels[i].Target;
							if (existingTarget.Node.Id != alreadyExportedChannelTargetId) continue;

							existingTarget.Node = new NodeId()
							{
								Id = newTargetId,
								Root = _root
							};
						}
						continue;
					}

					// add to cache: this is the first time we're exporting that particular binding.
					_clipAndSpeedAndPathToExportedTransform.Add((clip, speed, target), targetTr);
					var curve = targetCurvesBinding[target];
					var speedMultiplier = Mathf.Clamp(speed, 0.01f, Mathf.Infinity);

					// Initialize data
					// Bake and populate animation data
					float[] times = null;
					Vector3[] positions = null;
					Vector4[] rotations = null;
					Vector3[] scales = null;
					float[] weights = null;

					// arbitrary properties require the KHR_animation_pointer extension
					bool sampledAnimationData = false;
					if (settings.UseAnimationPointer && curve.propertyCurves != null && curve.propertyCurves.Count > 0)
					{
						var curves = curve.propertyCurves;
						foreach (KeyValuePair<string, PropertyCurve> c in curves)
						{
							var propertyName = c.Key;
							var prop = c.Value;
							if (BakePropertyAnimation(prop, clip.length, AnimationBakingFramerate, speedMultiplier, out times, out var values))
							{
								AddAnimationData(prop.target, prop.propertyName, animation, times, values);
								sampledAnimationData = true;
							}
						}
					}

					if (BakeCurveSet(curve, clip.length, AnimationBakingFramerate, speedMultiplier, ref times, ref positions, ref rotations, ref scales, ref weights))
					{
						bool haveAnimation = positions != null || rotations != null || scales != null || weights != null;
						if(haveAnimation)
						{
							AddAnimationData(targetTr, animation, times, positions, rotations, scales, weights);
							sampledAnimationData = true;
						}
						continue;
					}

					if(!sampledAnimationData)
						Debug.LogWarning("Warning: empty animation curves for " + target + " in " + clip + " from " + transform, transform);
				}
			}
			else
			{
				Debug.LogError("Only baked animation is supported for now. Skipping animation", null);
			}

			convertClipToGLTFAnimationMarker.End();
		}

		private void CollectClipCurves(GameObject root, AnimationClip clip, Dictionary<string, TargetCurveSet> targetCurves)
		{
#if UNITY_EDITOR

			foreach (var binding in UnityEditor.AnimationUtility.GetCurveBindings(clip))
			{
				AnimationCurve curve = UnityEditor.AnimationUtility.GetEditorCurve(clip, binding);

				var containsPosition = binding.propertyName.Contains("m_LocalPosition");
				var containsScale = binding.propertyName.Contains("m_LocalScale");
				var containsRotation = binding.propertyName.ToLowerInvariant().Contains("localrotation");
				var containsEuler = binding.propertyName.ToLowerInvariant().Contains("localeuler");
				var containsBlendShapeWeight = binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal);
				var containsCompatibleData = containsPosition || containsScale || containsRotation || containsEuler || containsBlendShapeWeight;

				if (!containsCompatibleData && !settings.UseAnimationPointer) continue;

				if (!targetCurves.ContainsKey(binding.path))
				{
					TargetCurveSet curveSet = new TargetCurveSet();
					curveSet.Init();
					targetCurves.Add(binding.path, curveSet);
				}

				TargetCurveSet current = targetCurves[binding.path];

				if (containsPosition)
				{
					if (binding.propertyName.Contains(".x"))
						current.translationCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.translationCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.translationCurves[2] = curve;
				}
				else if (containsScale)
				{
					if (binding.propertyName.Contains(".x"))
						current.scaleCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.scaleCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.scaleCurves[2] = curve;
				}
				else if (containsRotation)
				{
					current.rotationType = AnimationKeyRotationType.Quaternion;
					if (binding.propertyName.Contains(".x"))
						current.rotationCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.rotationCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.rotationCurves[2] = curve;
					else if (binding.propertyName.Contains(".w"))
						current.rotationCurves[3] = curve;
				}
				// Takes into account 'localEuler', 'localEulerAnglesBaked' and 'localEulerAnglesRaw'
				else if (containsEuler)
				{
					current.rotationType = AnimationKeyRotationType.Euler;
					if (binding.propertyName.Contains(".x"))
						current.rotationCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.rotationCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.rotationCurves[2] = curve;
				}
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				else if (containsBlendShapeWeight)
				{
					var weightName = binding.propertyName.Substring("blendShape.".Length);
					current.weightCurves.Add(weightName, curve);
				}
				else if (settings.UseAnimationPointer)
				{
					var obj = AnimationUtility.GetAnimatedObject(root, binding);
					current.AddPropertyCurves(obj, curve, binding);
					targetCurves[binding.path] = current;
					continue;
				}

				targetCurves[binding.path] = current;
			}
#endif
		}

		private void GenerateMissingCurves(float endTime, Transform tr, ref Dictionary<string, TargetCurveSet> targetCurvesBinding)
		{
			var keyList = targetCurvesBinding.Keys.ToList();
			foreach (string target in keyList)
			{
				Transform targetTr = target.Length > 0 ? tr.Find(target) : tr;
				if (targetTr == null)
					continue;

				TargetCurveSet current = targetCurvesBinding[target];

				if (current.weightCurves.Count > 0)
				{
					// need to sort and generate the other matching curves as constant curves for all blend shapes
					var renderer = targetTr.GetComponent<SkinnedMeshRenderer>();
					var mesh = renderer.sharedMesh;
					var shapeCount = mesh.blendShapeCount;

					// need to reorder weights: Unity stores the weights alphabetically in the AnimationClip,
					// not in the order of the weights.
					var newWeights = new Dictionary<string, AnimationCurve>();
					for (int i = 0; i < shapeCount; i++)
					{
						var shapeName = mesh.GetBlendShapeName(i);
						var shapeCurve = current.weightCurves.ContainsKey(shapeName) ? current.weightCurves[shapeName] : CreateConstantCurve(renderer.GetBlendShapeWeight(i), endTime);
						newWeights.Add(shapeName, shapeCurve);
					}

					current.weightCurves = newWeights;
				}

				targetCurvesBinding[target] = current;
			}
		}

		private AnimationCurve CreateConstantCurve(float value, float endTime)
		{
			// No translation curves, adding them
			AnimationCurve curve = new AnimationCurve();
			curve.AddKey(0, value);
			curve.AddKey(endTime, value);
			return curve;
		}

		private bool BakePropertyAnimation(PropertyCurve prop, float length, float bakingFramerate, float speedMultiplier, out float[] times, out object[] values)
		{
			times = null;
			values = null;

			var nbSamples = Mathf.Max(1, Mathf.CeilToInt(length * bakingFramerate));
			var deltaTime = length / nbSamples;

			times = new float[nbSamples];
			values = new object[nbSamples];

			// Assuming all the curves exist now
			for (int i = 0; i < nbSamples; ++i)
			{
				float t = i * deltaTime;
				times[i] = t / speedMultiplier;
				if (prop.curve.Count == 1)
				{
					values[i] = prop.curve[0].Evaluate(t);
				}
				else
				{
					var type = prop.propertyType;

					if (typeof(Vector2) == type)
					{
						values[i] = new Vector2(prop.Evaluate(t, 0), prop.Evaluate(t, 1));
					}
					else if (typeof(Vector3) == type)
					{
						values[i] = new Vector3(prop.Evaluate(t, 0), prop.Evaluate(t, 1), prop.Evaluate(t, 2));
					}
					else if (typeof(Vector4) == type)
					{
						values[i] = new Vector4(prop.Evaluate(t, 0), prop.Evaluate(t, 1), prop.Evaluate(t, 2), prop.Evaluate(t, 3));
					}
					else if (typeof(Color) == type)
					{
						values[i] = new Color(prop.Evaluate(t, 0), prop.Evaluate(t, 1), prop.Evaluate(t, 2), prop.Evaluate(t, 3));
					}
					else
					{
						Debug.LogError("Property is not handled: " + prop.propertyName + ", " + prop.propertyType, null);
						return false;
					}
				}
			}

			return true;
		}

		private bool BakeCurveSet(TargetCurveSet curveSet, float length, float bakingFramerate, float speedMultiplier, ref float[] times, ref Vector3[] positions, ref Vector4[] rotations, ref Vector3[] scales, ref float[] weights)
		{
			int nbSamples = Mathf.Max(1, Mathf.CeilToInt(length * bakingFramerate));
			float deltaTime = length / nbSamples;
			var weightCount = curveSet.weightCurves?.Count ?? 0;

			bool haveTranslationKeys = curveSet.translationCurves != null && curveSet.translationCurves.Length > 0 && curveSet.translationCurves[0] != null;
			bool haveRotationKeys = curveSet.rotationCurves != null && curveSet.rotationCurves.Length > 0 && curveSet.rotationCurves[0] != null;
			bool haveScaleKeys = curveSet.scaleCurves != null && curveSet.scaleCurves.Length > 0 && curveSet.scaleCurves[0] != null;
			bool haveWeightKeys = curveSet.weightCurves != null && curveSet.weightCurves.Count > 0;

			if(haveScaleKeys)
			{
				if(curveSet.scaleCurves.Length < 3)
				{
					Debug.LogError("Have Scale Animation, but not all properties are animated. Ignoring for now", null);
					return false;
				}
				bool anyIsNull = false;
				foreach (var sc in curveSet.scaleCurves)
					anyIsNull |= sc == null;

				if (anyIsNull)
				{
					Debug.LogWarning("A scale curve has at least one null property curve! Ignoring", null);
					haveScaleKeys = false;
				}
			}

			if(haveRotationKeys)
			{
				bool anyIsNull = false;
				int checkRotationKeyCount = curveSet.rotationType == AnimationKeyRotationType.Euler ? 3 : 4;
				for (int i = 0; i < checkRotationKeyCount; i++)
				{
					anyIsNull |= curveSet.rotationCurves.Length - 1 < i || curveSet.rotationCurves[i] == null;
				}

				if (anyIsNull)
				{
					Debug.LogWarning("A rotation curve has at least one null property curve! Ignoring", null);
					haveRotationKeys = false;
				}
			}

			if(!haveTranslationKeys && !haveRotationKeys && !haveScaleKeys && !haveWeightKeys)
			{
				return false;
			}

			// Initialize Arrays
			times = new float[nbSamples];
			if(haveTranslationKeys)
				positions = new Vector3[nbSamples];
			if(haveScaleKeys)
				scales = new Vector3[nbSamples];
			if(haveRotationKeys)
				rotations = new Vector4[nbSamples];
			if (haveWeightKeys)
				weights = new float[nbSamples * weightCount];

			// Assuming all the curves exist now
			for (int i = 0; i < nbSamples; ++i)
			{
				float currentTime = i * deltaTime;
				times[i] = currentTime / speedMultiplier;

				if(haveTranslationKeys)
					positions[i] = new Vector3(curveSet.translationCurves[0].Evaluate(currentTime), curveSet.translationCurves[1].Evaluate(currentTime), curveSet.translationCurves[2].Evaluate(currentTime));

				if(haveScaleKeys)
					scales[i] = new Vector3(curveSet.scaleCurves[0].Evaluate(currentTime), curveSet.scaleCurves[1].Evaluate(currentTime), curveSet.scaleCurves[2].Evaluate(currentTime));

				if(haveRotationKeys)
				{
					if (curveSet.rotationType == AnimationKeyRotationType.Euler)
					{
						Quaternion eulerToQuat = Quaternion.Euler(curveSet.rotationCurves[0].Evaluate(currentTime), curveSet.rotationCurves[1].Evaluate(currentTime), curveSet.rotationCurves[2].Evaluate(currentTime));
						rotations[i] = new Vector4(eulerToQuat.x, eulerToQuat.y, eulerToQuat.z, eulerToQuat.w);
					}
					else
					{
						rotations[i] = new Vector4(curveSet.rotationCurves[0].Evaluate(currentTime), curveSet.rotationCurves[1].Evaluate(currentTime), curveSet.rotationCurves[2].Evaluate(currentTime), curveSet.rotationCurves[3].Evaluate(currentTime));
					}
				}

				if (haveWeightKeys)
				{
					var curveArray = curveSet.weightCurves.Values.ToArray();
					for(int j = 0; j < weightCount; j++)
					{
						weights[i * weightCount + j] = curveArray[j].Evaluate(times[i]);
					}
				}
			}

			RemoveUnneededKeyframes(ref times, ref positions, ref rotations, ref scales, ref weights, ref weightCount);

			return true;
		}

#endif

		public int GetNodeIdFromTransform(Transform transform)
		{
			return GetAnimationTargetIdFromTransform(transform);
		}

		public int GetAnimationTargetIdFromTransform(Transform transform)
		{
			if (_exportedTransforms.ContainsKey(transform.GetInstanceID()))
			{
				return _exportedTransforms[transform.GetInstanceID()];
			}
			return -1;
		}

		public int GetAnimationTargetIdFromMaterial(Material mat)
		{
			if (_materials.TryGetValue(mat, out var index)) return index;
			return -1;
		}

		public void AddAnimationData(Object animatedObject, string propertyName, GLTFAnimation animation, float[] times, object[] values)
		{
			if (!settings.UseAnimationPointer)
			{
				Debug.LogWarning("Trying to export arbitrary animation (" + propertyName + ") - this requires KHR_animation_pointer", animatedObject);
				return;
			}
			if (values.Length <= 0) return;

			// var channelTargetId = GetAnimationTargetIdFromTransform(target);
			// if (channelTargetId < 0)
			// {
			// 	Debug.LogWarning("An animated transform is not part of _exportedTransforms, is the object disabled? " + target.name + " (InstanceID: " + target.GetInstanceID() + ")", target);
			// 	return;
			// }
			// var nodePath = "/nodes/" + channelTargetId + "/" + path;

			bool flipValueRange = false;
			bool isTextureTransform = false;
			string secondPropertyName = null;

			switch (animatedObject)
			{
				case Material material:
					// mapping from known Unity property names to glTF property names
					switch (propertyName)
					{
						case "_Color":
						case "_BaseColor":
							propertyName = "baseColorFactor";
							break;
						case "_MainTex_ST":
						case "_BaseMap_ST":
							if (!(material.HasProperty("_MainTex") && material.GetTexture("_MainTex")) &&
							    !(material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap"))) return;
							propertyName = $"baseColorTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}";
							secondPropertyName = $"baseColorTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}";
							isTextureTransform = true;
							break;
						case "_EmissionColor":
							propertyName = "emissiveFactor";
							secondPropertyName = $"extensions/{KHR_materials_emissive_strength_Factory.EXTENSION_NAME}/{nameof(KHR_materials_emissive_strength.emissiveStrength)}";
							break;
						case "_EmissionMap_ST":
							if (!(material.HasProperty("_EmissionMap") && material.GetTexture("_EmissionMap"))) return;
							propertyName = $"emissiveTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}";
							secondPropertyName = $"emissiveTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}";
							isTextureTransform = true;
							break;
						case "_Smoothness":
							propertyName = "roughnessFactor";
							flipValueRange = true;
							break;
						case "_Roughness":
							propertyName = "roughnessFactor";
							break;
						case "_Metalllic":
							propertyName = "metallicFactor";
							break;
					}
					break;
				case Light light:
					Debug.Log("light prop: " + propertyName);
					switch (propertyName)
					{
						case "m_Color":
							propertyName = $"color";
							break;
						case "m_Intensity":
							propertyName = $"intensity";
							break;
					}
					break;
			}

			AccessorId timeAccessor = ExportAccessor(times);

			AnimationChannel Tchannel = new AnimationChannel();
			AnimationChannelTarget TchannelTarget = new AnimationChannelTarget();
			Tchannel.Target = TchannelTarget;

			AnimationSampler Tsampler = new AnimationSampler();
			Tsampler.Input = timeAccessor;

			// for cases where one property needs to be split up into multiple tracks
			// example: emissiveFactor * emissiveStrength
			// TODO not needed when secondPropertyName==null
			AnimationChannel Tchannel2 = new AnimationChannel();
			AnimationChannelTarget TchannelTarget2 = new AnimationChannelTarget();
			Tchannel2.Target = TchannelTarget2;
			AnimationSampler Tsampler2 = new AnimationSampler();
			Tsampler2.Input = timeAccessor;

			var val = values[0];
			switch (val)
			{
				case float _:
					if (flipValueRange)
						Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => 1.0f - (float)e));
					else
						Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => (float)e));
					break;
				case Vector2 _:
					Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => (Vector2)e));
					break;
				case Vector3 _:
					Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => (Vector3)e));
					break;
				case Vector4 _:
					if (!isTextureTransform)
					{
						Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => (Vector4)e));
					}
					else
					{
						var scales = new Vector2[values.Length];
						var offsets = new Vector2[values.Length];
						for (int i = 0; i < values.Length; i++)
						{
							DecomposeScaleOffset((Vector4) values[i], out var scale, out var offset);
							scales[i] = scale;
							offsets[i] = offset;
						}
						Tsampler.Output = ExportAccessor(scales);
						Tsampler2.Output = ExportAccessor(offsets);
					}
					break;
				case Color _:
					if (propertyName == "emissiveFactor" && secondPropertyName != null)
					{
						var colors = new Color[values.Length];
						var strengths = new float[values.Length];
						for (int i = 0; i < values.Length; i++)
						{
							DecomposeEmissionColor((Color) values[i], out var color, out var intensity);
							colors[i] = color;
							strengths[i] = intensity;
						}
						Tsampler.Output = ExportAccessor(colors);
						Tsampler2.Output = ExportAccessor(strengths);
					}
					else
					{
						Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => (Color)e));
					}
					break;
			}

			Tchannel.Sampler = new AnimationSamplerId
			{
				Id = animation.Samplers.Count,
				GLTFAnimation = animation,
				Root = _root
			};
			animation.Samplers.Add(Tsampler);
			animation.Channels.Add(Tchannel);

			ConvertToAnimationPointer(animatedObject, propertyName, TchannelTarget);

			if(secondPropertyName != null)
			{
				Tchannel2.Sampler = new AnimationSamplerId
				{
					Id = animation.Samplers.Count,
					GLTFAnimation = animation,
					Root = _root
				};
				animation.Samplers.Add(Tsampler2);
				animation.Channels.Add(Tchannel2);

				ConvertToAnimationPointer(animatedObject, secondPropertyName, TchannelTarget2);
			}
		}

		void ConvertToAnimationPointer(object animatedObject, string propertyName, AnimationChannelTarget target)
		{
			var ext = new KHR_animation_pointer();
			ext.propertyBinding = propertyName;
			ext.animatedObject = animatedObject;
			ext.channel = target;
			animationPointerResolver.Add(ext);

			target.Path = "pointer";
			target.AddExtension(KHR_animation_pointer.EXTENSION_NAME, ext);
		}

		public void AddAnimationData(
			Transform target,
			GLTF.Schema.GLTFAnimation animation,
			float[] times = null,
			Vector3[] positions = null,
			Vector4[] rotations = null,
			Vector3[] scales = null,
			float[] weights = null)
		{
			addAnimationDataMarker.Begin();

			int channelTargetId = GetAnimationTargetIdFromTransform(target);
			if (channelTargetId < 0)
			{
				Debug.LogWarning($"An animated transform seems to be {(settings.ExportDisabledGameObjects ? "missing" : "missing or disabled")}: {target.name} (InstanceID: {target.GetInstanceID()})", target);
				addAnimationDataMarker.End();
				return;
			}

			AccessorId timeAccessor = ExportAccessor(times);
			timeAccessor.Value.BufferView.Value.ByteStride = 0;

			// Translation
			if(positions != null && positions.Length > 0)
			{
				exportPositionAnimationDataMarker.Begin();

				AnimationChannel Tchannel = new AnimationChannel();
				AnimationChannelTarget TchannelTarget = new AnimationChannelTarget();
				TchannelTarget.Path = GLTFAnimationChannelPath.translation.ToString();
				TchannelTarget.Node = new NodeId
				{
					Id = channelTargetId,
					Root = _root
				};

				Tchannel.Target = TchannelTarget;

				AnimationSampler Tsampler = new AnimationSampler();
				Tsampler.Input = timeAccessor;
				Tsampler.Output = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(positions, SchemaExtensions.CoordinateSpaceConversionScale));
				Tsampler.Output.Value.BufferView.Value.ByteStride = 0;
				Tchannel.Sampler = new AnimationSamplerId
				{
					Id = animation.Samplers.Count,
					GLTFAnimation = animation,
					Root = _root
				};

				animation.Samplers.Add(Tsampler);
				animation.Channels.Add(Tchannel);

				if (settings.UseAnimationPointer)
					ConvertToAnimationPointer(target, "translation", TchannelTarget);

				exportPositionAnimationDataMarker.End();
			}

			// Rotation
			if(rotations != null && rotations.Length > 0)
			{
				exportRotationAnimationDataMarker.Begin();

				AnimationChannel Rchannel = new AnimationChannel();
				AnimationChannelTarget RchannelTarget = new AnimationChannelTarget();
				RchannelTarget.Path = GLTFAnimationChannelPath.rotation.ToString();
				RchannelTarget.Node = new NodeId
				{
					Id = channelTargetId,
					Root = _root
				};

				Rchannel.Target = RchannelTarget;

				AnimationSampler Rsampler = new AnimationSampler();
				Rsampler.Input = timeAccessor; // Float, for time
				Rsampler.Output = ExportAccessorSwitchHandedness(rotations); // Vec4 for rotations
				Rsampler.Output.Value.BufferView.Value.ByteStride = 0;
				Rchannel.Sampler = new AnimationSamplerId
				{
					Id = animation.Samplers.Count,
					GLTFAnimation = animation,
					Root = _root
				};

				animation.Samplers.Add(Rsampler);
				animation.Channels.Add(Rchannel);

				if (settings.UseAnimationPointer)
					ConvertToAnimationPointer(target, "rotation", RchannelTarget);

				exportRotationAnimationDataMarker.End();
			}

			// Scale
			if(scales != null && scales.Length > 0)
			{
				exportScaleAnimationDataMarker.Begin();

				AnimationChannel Schannel = new AnimationChannel();
				AnimationChannelTarget SchannelTarget = new AnimationChannelTarget();
				SchannelTarget.Path = GLTFAnimationChannelPath.scale.ToString();
				SchannelTarget.Node = new NodeId
				{
					Id = channelTargetId,
					Root = _root
				};

				Schannel.Target = SchannelTarget;

				AnimationSampler Ssampler = new AnimationSampler();
				Ssampler.Input = timeAccessor; // Float, for time
				Ssampler.Output = ExportAccessor(scales); // Vec3 for scale
				Ssampler.Output.Value.BufferView.Value.ByteStride = 0;
				Schannel.Sampler = new AnimationSamplerId
				{
					Id = animation.Samplers.Count,
					GLTFAnimation = animation,
					Root = _root
				};

				animation.Samplers.Add(Ssampler);
				animation.Channels.Add(Schannel);

				if (settings.UseAnimationPointer)
					ConvertToAnimationPointer(target, "scale", SchannelTarget);

				exportScaleAnimationDataMarker.End();
			}

			if (weights != null && weights.Length > 0)
			{
				exportWeightsAnimationDataMarker.Begin();

				// scale weights correctly if there are any
				var skinnedMesh = target.GetComponent<SkinnedMeshRenderer>();
				if (skinnedMesh)
				{
					// this code is adapted from SkinnedMeshRendererEditor (which calculates the right range for sliders to show)
					// instead of calculating per blend shape, we're assuming all blendshapes have the same min/max here though.
					var minBlendShapeFrameWeight = 0.0f;
					var maxBlendShapeFrameWeight = 0.0f;

					var sharedMesh = skinnedMesh.sharedMesh;
					var shapeCount = sharedMesh.blendShapeCount;
					for (int index = 0; index < shapeCount; ++index)
					{
						var blendShapeFrameCount = sharedMesh.GetBlendShapeFrameCount(index);
						for (var frameIndex = 0; frameIndex < blendShapeFrameCount; ++frameIndex)
						{
							var shapeFrameWeight = sharedMesh.GetBlendShapeFrameWeight(index, frameIndex);
							minBlendShapeFrameWeight = Mathf.Min(shapeFrameWeight, minBlendShapeFrameWeight);
							maxBlendShapeFrameWeight = Mathf.Max(shapeFrameWeight, maxBlendShapeFrameWeight);
						}
					}

					// Debug.Log($"min: {minBlendShapeFrameWeight}, max: {maxBlendShapeFrameWeight}");
					// glTF weights 0..1 match to Unity weights 0..100, but Unity weights can be in arbitrary ranges
					if (maxBlendShapeFrameWeight > 0)
					{
						for (int i = 0; i < weights.Length; i++)
							weights[i] *= 1 / maxBlendShapeFrameWeight;
					}
				}

				AnimationChannel Wchannel = new AnimationChannel();
				AnimationChannelTarget WchannelTarget = new AnimationChannelTarget();
				WchannelTarget.Path = GLTFAnimationChannelPath.weights.ToString();
				WchannelTarget.Node = new NodeId()
				{
					Id = channelTargetId,
					Root = _root
				};

				Wchannel.Target = WchannelTarget;

				AnimationSampler Wsampler = new AnimationSampler();
				Wsampler.Input = timeAccessor;
				Wsampler.Output = ExportAccessor(weights);
				Wsampler.Output.Value.BufferView.Value.ByteStride = 0;
				Wchannel.Sampler = new AnimationSamplerId()
				{
					Id = animation.Samplers.Count,
					GLTFAnimation = animation,
					Root = _root
				};

				animation.Samplers.Add(Wsampler);
				animation.Channels.Add(Wchannel);

				if (settings.UseAnimationPointer)
					ConvertToAnimationPointer(target, "weights", WchannelTarget);

				exportWeightsAnimationDataMarker.End();
			}

			addAnimationDataMarker.End();
		}

		private bool ArrayRangeEquals(float[] array, int sectionLength, int prevSectionStart, int sectionStart, int nextSectionStart)
		{
			var equals = true;
			for (int i = 0; i < sectionLength; i++)
			{
				equals &= array[prevSectionStart + i] == array[sectionStart + i] && array[sectionStart + i] == array[nextSectionStart + i];
				if (!equals) return false;
			}

			return true;
		}

		public void RemoveUnneededKeyframes(ref float[] times, ref Vector3[] positions, ref Vector4[] rotations, ref Vector3[] scales, ref float[] weights, ref int weightCount)
		{
			removeAnimationUnneededKeyframesMarker.Begin();
			removeAnimationUnneededKeyframesInitMarker.Begin();

			var haveTranslationKeys = positions?.Any() ?? false;
			var haveRotationKeys = rotations?.Any() ?? false;
			var haveScaleKeys = scales?.Any() ?? false;
			var haveWeightKeys = weights?.Any() ?? false;

			// remove keys again where prev/next keyframe are identical
			List<float> t2 = new List<float>(times.Length);
			List<Vector3> p2 = new List<Vector3>(times.Length);
			List<Vector3> s2 = new List<Vector3>(times.Length);
			List<Vector4> r2 = new List<Vector4>(times.Length);
			List<float> w2 = new List<float>(times.Length);
			var singleFrameWeights = new float[weightCount];

			t2.Add(times[0]);
			if (haveTranslationKeys) p2.Add(positions[0]);
			if (haveRotationKeys) r2.Add(rotations[0]);
			if (haveScaleKeys) s2.Add(scales[0]);
			if (haveWeightKeys)
			{
				Array.Copy(weights, 0, singleFrameWeights, 0, weightCount);
				w2.AddRange(singleFrameWeights);
			}

			removeAnimationUnneededKeyframesInitMarker.End();

			for (int i = 1; i < times.Length - 1; i++)
			{
				removeAnimationUnneededKeyframesCheckIdenticalMarker.Begin();
				// check identical
				bool isIdentical = true;
				if (haveTranslationKeys)
					isIdentical &= positions[i - 1] == positions[i] && positions[i] == positions[i + 1];
				if (isIdentical && haveRotationKeys)
					isIdentical &= rotations[i - 1] == rotations[i] && rotations[i] == rotations[i + 1];
				if (isIdentical && haveScaleKeys)
					isIdentical &= scales[i - 1] == scales[i] && scales[i] == scales[i + 1];
				exportWeightsAnimationDataMarker.Begin();
				if (isIdentical && haveWeightKeys)
					isIdentical &= ArrayRangeEquals(weights, weightCount, (i - 1) * weightCount, i * weightCount, (i+1) * weightCount);
				exportWeightsAnimationDataMarker.End();

				if (!isIdentical)
				{
					removeAnimationUnneededKeyframesCheckIdenticalKeepMarker.Begin();
					t2.Add(times[i]);
					if (haveTranslationKeys) p2.Add(positions[i]);
					if (haveRotationKeys) r2.Add(rotations[i]);
					if (haveScaleKeys) s2.Add(scales[i]);
					exportWeightsAnimationDataMarker.Begin();
					if (haveWeightKeys)
					{
						Array.Copy(weights, (i - 1) * weightCount, singleFrameWeights, 0, weightCount);
						w2.AddRange(singleFrameWeights);
					}
					exportWeightsAnimationDataMarker.End();
					removeAnimationUnneededKeyframesCheckIdenticalKeepMarker.End();
				}
				removeAnimationUnneededKeyframesCheckIdenticalMarker.End();
			}

			removeAnimationUnneededKeyframesFinalizeMarker.Begin();

			var max = times.Length - 1;

			t2.Add(times[max]);
			if (haveTranslationKeys) p2.Add(positions[max]);
			if (haveRotationKeys) r2.Add(rotations[max]);
			if (haveScaleKeys) s2.Add(scales[max]);
			if (haveWeightKeys) w2.AddRange(weights.Skip((max - 1) * weightCount).Take(weightCount));

			// Debug.Log("Keyframes before compression: " + times.Length + "; " + "Keyframes after compression: " + t2.Count);

			times = t2.ToArray();
			if (haveTranslationKeys) positions = p2.ToArray();
			if (haveRotationKeys) rotations = r2.ToArray();
			if (haveScaleKeys) scales = s2.ToArray();
			if (haveWeightKeys) weights = w2.ToArray();

			removeAnimationUnneededKeyframesFinalizeMarker.End();

			removeAnimationUnneededKeyframesMarker.End();
		}

		private UnityEngine.Mesh GetMeshFromGameObject(GameObject gameObject)
		{
			if (gameObject.GetComponent<MeshFilter>())
			{
				return gameObject.GetComponent<MeshFilter>().sharedMesh;
			}

			SkinnedMeshRenderer skinMesh = gameObject.GetComponent<SkinnedMeshRenderer>();
			if (skinMesh)
			{
				if (!ExportAnimations && BakeSkinnedMeshes)
				{
					if (!_bakedMeshes.ContainsKey(skinMesh))
					{
						UnityEngine.Mesh bakedMesh = new UnityEngine.Mesh();
						skinMesh.BakeMesh(bakedMesh);
						_bakedMeshes.Add(skinMesh, bakedMesh);
					}

					return _bakedMeshes[skinMesh];
				}

				return gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
			}

			return null;
		}

		private UnityEngine.Material[] GetMaterialsFromGameObject(GameObject gameObject)
		{
			if (gameObject.GetComponent<MeshRenderer>())
			{
				return gameObject.GetComponent<MeshRenderer>().sharedMaterials;
			}

			if (gameObject.GetComponent<SkinnedMeshRenderer>())
			{
				return gameObject.GetComponent<SkinnedMeshRenderer>().sharedMaterials;
			}

			return null;
		}

		private void ExportSkinFromNode(Transform transform)
		{
			exportSkinFromNodeMarker.Begin();

			PrimKey key = new PrimKey();
			UnityEngine.Mesh mesh = GetMeshFromGameObject(transform.gameObject);
			key.Mesh = mesh;
			key.Materials = GetMaterialsFromGameObject(transform.gameObject);
			MeshId val;
			if (!_primOwner.TryGetValue(key, out val))
			{
				Debug.Log("No mesh found for skin on " + transform, transform);
				exportSkinFromNodeMarker.End();
				return;
			}
			SkinnedMeshRenderer skin = transform.GetComponent<SkinnedMeshRenderer>();
			GLTF.Schema.Skin gltfSkin = new Skin();

			// early out of this SkinnedMeshRenderer has no bones assigned (could be BlendShapes-only)
			if (skin.bones == null || skin.bones.Length == 0)
			{
				exportSkinFromNodeMarker.End();
				return;
			}

			bool allBoneTransformNodesHaveBeenExported = true;
			for (int i = 0; i < skin.bones.Length; ++i)
			{
				if (!skin.bones[i])
				{
					Debug.LogWarning("Skin has null bone at index " + i + ": " + skin, skin);
					continue;
				}
				var nodeId = skin.bones[i].GetInstanceID();
				if (!_exportedTransforms.ContainsKey(nodeId))
				{
					allBoneTransformNodesHaveBeenExported = false;
					break;
				}
			}

			if (!allBoneTransformNodesHaveBeenExported)
			{
				Debug.LogWarning("Not all bones for SkinnedMeshRenderer " + transform + " were exported. Skin information will be skipped. Make sure the bones are active and enabled if you want to export them.", transform);
				exportSkinFromNodeMarker.End();
				return;
			}

			for (int i = 0; i < skin.bones.Length; ++i)
			{
				if (!skin.bones[i])
				{
					continue;
				}

				var nodeId = skin.bones[i].GetInstanceID();

				gltfSkin.Joints.Add(
					new NodeId
					{
						Id = _exportedTransforms[nodeId],
						Root = _root
					});
			}

			gltfSkin.InverseBindMatrices = ExportAccessor(mesh.bindposes);

			Vector4[] bones = boneWeightToBoneVec4(mesh.boneWeights);
			Vector4[] weights = boneWeightToWeightVec4(mesh.boneWeights);

			if(val != null)
			{
				GLTF.Schema.GLTFMesh gltfMesh = _root.Meshes[val.Id];
				if(gltfMesh != null)
				{
					foreach (MeshPrimitive prim in gltfMesh.Primitives)
					{
						if (!prim.Attributes.ContainsKey("JOINTS_0"))
						{
							var jointsAccessor = ExportAccessorUint(bones);
							jointsAccessor.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
							prim.Attributes.Add("JOINTS_0", jointsAccessor);
						}

						if (!prim.Attributes.ContainsKey("WEIGHTS_0"))
						{
							var weightsAccessor = ExportAccessor(weights);
							weightsAccessor.Value.BufferView.Value.Target = BufferViewTarget.ArrayBuffer;
							prim.Attributes.Add("WEIGHTS_0", weightsAccessor);
						}
					}
				}
			}

			_root.Nodes[_exportedTransforms[transform.GetInstanceID()]].Skin = new SkinId() { Id = _root.Skins.Count, Root = _root };
			_root.Skins.Add(gltfSkin);

			exportSkinFromNodeMarker.End();
		}

		private Vector4[] boneWeightToBoneVec4(BoneWeight[] bw)
		{
			Vector4[] bones = new Vector4[bw.Length];
			for (int i = 0; i < bw.Length; ++i)
			{
				bones[i] = new Vector4(bw[i].boneIndex0, bw[i].boneIndex1, bw[i].boneIndex2, bw[i].boneIndex3);
			}

			return bones;
		}

		private Vector4[] boneWeightToWeightVec4(BoneWeight[] bw)
		{
			Vector4[] weights = new Vector4[bw.Length];
			for (int i = 0; i < bw.Length; ++i)
			{
				weights[i] = new Vector4(bw[i].weight0, bw[i].weight1, bw[i].weight2, bw[i].weight3);
			}

			return weights;
		}

		private AccessorId ExportAccessorUint(Vector4[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorUintArrayMarker.Begin();

			var count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.UnsignedShort;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float minZ = arr[0].z;
			float minW = arr[0].w;
			float maxX = arr[0].x;
			float maxY = arr[0].y;
			float maxZ = arr[0].z;
			float maxW = arr[0].w;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.w < minW)
				{
					minW = cur.w;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
				if (cur.w > maxW)
				{
					maxW = cur.w;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ, minW };
			accessor.Max = new List<double> { maxX, maxY, maxZ, maxW };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
			foreach (var vec in arr)
			{
				_bufferWriter.Write((ushort)vec.x);
				_bufferWriter.Write((ushort)vec.y);
				_bufferWriter.Write((ushort)vec.z);
				_bufferWriter.Write((ushort)vec.w);
			}
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorUintArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		// This is used for Quaternions / Rotations
		private AccessorId ExportAccessorSwitchHandedness(Vector4[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorVector4ArrayMarker.Begin();

			var count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			var a0 = arr[0];
			a0 = a0.SwitchHandedness();
			a0.Normalize();
			float minX = a0.x;
			float minY = a0.y;
			float minZ = a0.z;
			float minW = a0.w;
			float maxX = a0.x;
			float maxY = a0.y;
			float maxZ = a0.z;
			float maxW = a0.w;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];
				cur = cur.SwitchHandedness();
				cur.Normalize();

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.w < minW)
				{
					minW = cur.w;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
				if (cur.w > maxW)
				{
					maxW = cur.w;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ, minW };
			accessor.Max = new List<double> { maxX, maxY, maxZ, maxW };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
#if USE_FAST_BINARY_WRITER
			_bufferWriter.WriteNormalizedSwitchHandedness(arr);
#else
			foreach (var vec in arr)
			{
				Vector4 vect = vec.SwitchHandedness();
				vect.Normalize();
				_bufferWriter.Write(vect.x);
				_bufferWriter.Write(vect.y);
				_bufferWriter.Write(vect.z);
				_bufferWriter.Write(vect.w);
			}
#endif
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorVector4ArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}

		private AccessorId ExportAccessor(Matrix4x4[] arr)
		{
			exportAccessorMarker.Begin();
			exportAccessorMatrix4x4ArrayMarker.Begin();

			var count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.MAT4;

			// Dont serialize min/max for matrices

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			exportAccessorBufferWriteMarker.Begin();
			foreach (var mat in arr)
			{
				var m = SchemaExtensions.ToGltfMatrix4x4Convert(mat);
				for (uint i = 0; i < 4; ++i)
				{
					var col = m.GetColumn(i);
					_bufferWriter.Write(col.X);
					_bufferWriter.Write(col.Y);
					_bufferWriter.Write(col.Z);
					_bufferWriter.Write(col.W);
				}
			}
			exportAccessorBufferWriteMarker.End();

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			exportAccessorMatrix4x4ArrayMarker.End();
			exportAccessorMarker.End();

			return id;
		}
	}
}
