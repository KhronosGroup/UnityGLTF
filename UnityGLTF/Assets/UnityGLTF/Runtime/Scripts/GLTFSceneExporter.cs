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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using GLTF.Schema;
using GLTF.Schema.KHR_lights_punctual;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Extensions;
using CameraType = GLTF.Schema.CameraType;
using LightType = UnityEngine.LightType;
using WrapMode = GLTF.Schema.WrapMode;

#if UNITY_EDITOR
using UnityEditor;
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

	public partial class GLTFSceneExporter
	{
		// Available export callbacks.
		// Callbacks can be either set statically (for exporters that register themselves)
		// or added in the ExportOptions.
		public delegate string RetrieveTexturePathDelegate(Texture texture);
		public delegate void BeforeSceneExportDelegate(GLTFSceneExporter exporter, GLTFRoot gltfRoot);
		public delegate void AfterSceneExportDelegate(GLTFSceneExporter exporter, GLTFRoot gltfRoot);
		public delegate void AfterNodeExportDelegate(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node);

		private static ILogger Debug = UnityEngine.Debug.unityLogger;

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
		private List<ImageInfo> _imageInfos;
		private List<Texture> _textures;
		private Dictionary<Material, int> _materials;
		private List<(Transform tr, AnimationClip clip)> _animationClips;
		private bool _shouldUseInternalBufferForImages;
		private Dictionary<int, int> _exportedTransforms;
		private Dictionary<int, int> _exportedCameras;
		private Dictionary<int, int> _exportedLights;
		private List<Transform> _animatedNodes;
		private List<Transform> _skinnedNodes;
		private Dictionary<SkinnedMeshRenderer, UnityEngine.Mesh> _bakedMeshes;

		private int _exportLayerMask;
		private ExportOptions _exportOptions;

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
			else
				Debug = UnityEngine.Debug.unityLogger;

			_exportLayerMask = _exportOptions.ExportLayers;

			var metalGlossChannelSwapShader = Resources.Load("MetalGlossChannelSwap", typeof(Shader)) as Shader;
			_metalGlossChannelSwapMaterial = new Material(metalGlossChannelSwapShader);

			var normalChannelShader = Resources.Load("NormalChannel", typeof(Shader)) as Shader;
			_normalChannelMaterial = new Material(normalChannelShader);

			_rootTransforms = rootTransforms;

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
			_animationClips = new List<(Transform, AnimationClip)>();

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
		/// Specifies the path and filename for the GLTF Json and binary
		/// </summary>
		/// <param name="path">File path for saving the GLTF and binary files</param>
		/// <param name="fileName">The name of the GLTF file</param>
		public void SaveGLTFandBin(string path, string fileName)
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
		public static string GetFileName(string directory, string fileNameThatMayHaveExtension, string requiredExtension)
		{
			var absolutePathThatMayHaveExtension = Path.Combine(directory, EnsureValidFileName(fileNameThatMayHaveExtension));

			if (!requiredExtension.StartsWith(".", StringComparison.Ordinal))
				requiredExtension = "." + requiredExtension;

			if (!Path.GetExtension(absolutePathThatMayHaveExtension).Equals(requiredExtension, StringComparison.OrdinalIgnoreCase))
				return absolutePathThatMayHaveExtension + requiredExtension;

			return absolutePathThatMayHaveExtension;
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

			Light unityLight = nodeTransform.GetComponent<Light>();
			if (unityLight != null && unityLight.enabled)
			{
				node.Light = ExportLight(unityLight);
			}

            if (unityLight != null || unityCamera != null)
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

			// Register nodes for animation parsing (could be disabled if animation is disabled)
			_exportedCameras.Add(unityCamera.GetInstanceID(), _root.Cameras.Count);
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
	            // TODO URP/HDRP can distinguish here, no need to guess innerConeAngle there
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

            // Register nodes for animation parsing (could be disabled if animation is disabled)
            _exportedLights.Add(unityLight.GetInstanceID(), _root.Lights.Count);

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

#region Public API

		public int GetAnimationId(AnimationClip clip, Transform transform)
		{
			for (var i = 0; i < _animationClips.Count; i++)
			{
				var entry = _animationClips[i];
				if (entry.tr == transform && entry.clip == clip) return i;
			}
			return -1;
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

		public Texture GetTexture(int id) => _textures[id];

#endregion

		private static DrawMode GetDrawMode(MeshTopology topology)
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
	}
}
