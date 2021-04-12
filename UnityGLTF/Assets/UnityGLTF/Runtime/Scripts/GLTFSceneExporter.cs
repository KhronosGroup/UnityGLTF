using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Extensions;
using CameraType = GLTF.Schema.CameraType;
using WrapMode = GLTF.Schema.WrapMode;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

namespace UnityGLTF
{
	public class ExportOptions
	{
		public GLTFSceneExporter.RetrieveTexturePathDelegate TexturePathRetriever = (texture) => texture.name;
		public bool ExportInactivePrimitives = true;
	}

	public class GLTFSceneExporter
	{
		public delegate string RetrieveTexturePathDelegate(Texture texture);

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

		private enum TextureMapType
		{
			Main,
			Bump,
			SpecGloss,
			Emission,
			MetallicGloss,
			Light,
			Occlusion
		}

		private struct ImageInfo
		{
			public Texture2D texture;
			public TextureMapType textureMapType;
		}

		private Transform[] _rootTransforms;
		private GLTFRoot _root;
		private BufferId _bufferId;
		private GLTFBuffer _buffer;
		private BinaryWriter _bufferWriter;
		private List<ImageInfo> _imageInfos;
		private List<Texture> _textures;
		private List<Material> _materials;
		private bool _shouldUseInternalBufferForImages;
		private Dictionary<int, int> _exportedTransforms;
		private List<Transform> _animatedNodes;
		private List<AnimationClip> _animationClips;
		private List<Transform> _skinnedNodes;
		private Dictionary<SkinnedMeshRenderer, UnityEngine.Mesh> _bakedMeshes;

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
			public Mesh Mesh;
			public Material Material;
		}
		private readonly Dictionary<PrimKey, MeshId> _primOwner = new Dictionary<PrimKey, MeshId>();
		private readonly Dictionary<Mesh, MeshPrimitive[]> _meshToPrims = new Dictionary<Mesh, MeshPrimitive[]>();

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
		public static bool BakeSkinnedMeshes
		{
			get { return settings.BakeSkinnedMeshes; }
			set { settings.BakeSkinnedMeshes = value; }
		}
		public static string SaveFolderPath
		{
			get { return settings.SaveFolderPath; }
			set { settings.SaveFolderPath = value; }
		}

		private static int AnimationBakingFramerate = 30; // FPS
		private static bool BakeAnimationData = true;

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
		public GLTFSceneExporter(Transform[] rootTransforms, ExportOptions options)
		{
			_exportOptions = options;

			var metalGlossChannelSwapShader = Resources.Load("MetalGlossChannelSwap", typeof(Shader)) as Shader;
			_metalGlossChannelSwapMaterial = new Material(metalGlossChannelSwapShader);

			var normalChannelShader = Resources.Load("NormalChannel", typeof(Shader)) as Shader;
			_normalChannelMaterial = new Material(normalChannelShader);

			_rootTransforms = rootTransforms;

			_exportedTransforms = new Dictionary<int, int>();
			_animatedNodes = new List<Transform>();
			_animationClips = new List<AnimationClip>();
			_skinnedNodes = new List<Transform>();
			_bakedMeshes = new Dictionary<SkinnedMeshRenderer, UnityEngine.Mesh>();

			_root = new GLTFRoot
			{
				Accessors = new List<Accessor>(),
				Animations = new List<GLTFAnimation>(),
				Asset = new Asset
				{
					Version = "2.0"
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
			_materials = new List<Material>();
			_textures = new List<Texture>();

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
			var fullPath = GetFileName(Path.Combine(path, fileName), "glb");
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
			Stream binStream = new MemoryStream();
			Stream jsonStream = new MemoryStream();

			_bufferWriter = new BinaryWriter(binStream);

			TextWriter jsonWriter = new StreamWriter(jsonStream, Encoding.ASCII);

			// rotate 180°
			if(_rootTransforms.Length > 1)
				Debug.LogWarning("Exporting multiple selected objects will most likely fail with the current rotation flip to match USDZ behaviour. Make sure to select a single root transform before export.");
			// foreach(var t in _rootTransforms)
			// 	t.rotation = Quaternion.Euler(0,180,0) * t.rotation;

			_root.Scene = ExportScene(sceneName, _rootTransforms);
			if (ExportAnimations)
			{
				ExportAnimation();
				// Export skins
				for (int i = 0; i < _skinnedNodes.Count; ++i)
				{
					Transform t = _skinnedNodes[i];
					ExportSkinFromNode(t);
				}
			}

			_buffer.ByteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Length, 4);

			_root.Serialize(jsonWriter, true);

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

			// write JSON chunk header.
			writer.Write((int)jsonStream.Length);
			writer.Write(MagicJson);

			jsonStream.Position = 0;
			CopyStream(jsonStream, writer);

			writer.Write((int)binStream.Length);
			writer.Write(MagicBin);

			binStream.Position = 0;
			CopyStream(binStream, writer);

			writer.Flush();

			// foreach(var t in _rootTransforms)
			// 	t.rotation = Quaternion.Euler(0,-180,0) * t.rotation;
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
			_shouldUseInternalBufferForImages = false;
			var fullPath = GetFileName(Path.Combine(path, fileName), "bin");
			var binFile = File.Create(fullPath);
			_bufferWriter = new BinaryWriter(binFile);

			// rotate 180°
			if(_rootTransforms.Length > 1)
				Debug.LogWarning("Exporting multiple selected objects will most likely fail with the current rotation flip to match USDZ behaviour. Make sure to select a single root transform before export.");
			// foreach(var t in _rootTransforms)
			// 	t.rotation *= Quaternion.Euler(0,180,0);

			_root.Scene = ExportScene(fileName, _rootTransforms);
			if (ExportAnimations)
			{
				ExportAnimation();
				// Export skins
				for (int i = 0; i < _skinnedNodes.Count; ++i)
				{
					Transform t = _skinnedNodes[i];
					ExportSkinFromNode(t);

					// updateProgress(EXPORT_STEP.SKINNING, i, _skinnedNodes.Count);
				}
			}
			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			_buffer.Uri = fileName + ".bin";
			_buffer.ByteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Length, 4);

			var gltfFile = File.CreateText(Path.ChangeExtension(fullPath, "gltf"));
			_root.Serialize(gltfFile);

#if WINDOWS_UWP
			gltfFile.Dispose();
			binFile.Dispose();
#else
			gltfFile.Close();
			binFile.Close();
#endif
			ExportImages(path);

			// foreach(var t in _rootTransforms)
			// 	t.rotation *= Quaternion.Euler(0,-180,0);
		}

		/// <summary>
		/// Ensures a specific file extension from an absolute path that may or may not already have that extension.
		/// </summary>
		/// <param name="absolutePathThatMayHaveExtension">Absolute path that may or may not already have the required extension</param>
		/// <param name="requiredExtension">The extension to ensure, with leading dot</param>
		/// <returns>An absolute path that has the required extension</returns>
		private string GetFileName(string absolutePathThatMayHaveExtension, string requiredExtension)
		{
			if (!Path.GetExtension(absolutePathThatMayHaveExtension).Equals(requiredExtension, StringComparison.OrdinalIgnoreCase))
			{
				return absolutePathThatMayHaveExtension + "." + requiredExtension;
			}

			return absolutePathThatMayHaveExtension;
		}
		
		private void ExportImages(string outputPath)
		{
			for (int t = 0; t < _imageInfos.Count; ++t)
			{
				var image = _imageInfos[t].texture;

				bool wasAbleToExportTexture = false;
				if (TryExportTexturesFromDisk && TryGetTextureDataFromDisk(image, out string path, out byte[] imageBytes))
				{
					var finalFilenamePath = ConstructImageFilenamePath(image, outputPath, Path.GetExtension(path));
					//Debug.Log(finalFilenamePath + ", " + image.name, image);
					if(IsPng(finalFilenamePath) || IsJpeg(finalFilenamePath)) {
						wasAbleToExportTexture = true;
						finalFilenamePath = Path.ChangeExtension(finalFilenamePath, Path.GetExtension(path));
						File.WriteAllBytes(finalFilenamePath, imageBytes);
					}
				}

				if(!wasAbleToExportTexture) {
					switch (_imageInfos[t].textureMapType)
					{
						case TextureMapType.MetallicGloss:
							ExportMetallicGlossTexture(image, outputPath);
							break;
						case TextureMapType.Bump:
							ExportNormalTexture(image, outputPath);
							break;
						default:
							ExportTexture(image, outputPath);
							break;
					}
				}
			}
		}

		/// <summary>
		/// This converts Unity's metallic-gloss texture representation into GLTF's metallic-roughness specifications.
		/// Unity's metallic-gloss A channel (glossiness) is inverted and goes into GLTF's metallic-roughness G channel (roughness).
		/// Unity's metallic-gloss R channel (metallic) goes into GLTF's metallic-roughess B channel.
		/// </summary>
		/// <param name="texture">Unity's metallic-gloss texture to be exported</param>
		/// <param name="outputPath">The location to export the texture</param>
		private void ExportMetallicGlossTexture(Texture2D texture, string outputPath)
		{
			var destRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

			Graphics.Blit(texture, destRenderTexture, _metalGlossChannelSwapMaterial);

			var exportTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false, true);
			exportTexture.ReadPixels(new Rect(0, 0, destRenderTexture.width, destRenderTexture.height), 0, 0);
			exportTexture.Apply();

			var finalFilenamePath = ConstructImageFilenamePath(texture, outputPath, "png");
			File.WriteAllBytes(finalFilenamePath, exportTexture.EncodeToPNG());

			RenderTexture.ReleaseTemporary(destRenderTexture);
			if (Application.isEditor)
			{
				GameObject.DestroyImmediate(exportTexture);
			}
			else
			{
				GameObject.Destroy(exportTexture);
			}
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

			var exportTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
			exportTexture.ReadPixels(new Rect(0, 0, destRenderTexture.width, destRenderTexture.height), 0, 0);
			exportTexture.Apply();

			var finalFilenamePath = ConstructImageFilenamePath(texture, outputPath, "png");
			File.WriteAllBytes(finalFilenamePath, exportTexture.EncodeToPNG());

			RenderTexture.ReleaseTemporary(destRenderTexture);
			if (Application.isEditor)
			{
				GameObject.DestroyImmediate(exportTexture);
			}
			else
			{
				GameObject.Destroy(exportTexture);
			}
		}

		private void ExportTexture(Texture2D texture, string outputPath)
		{
			var destRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

			Graphics.Blit(texture, destRenderTexture);

			var exportTexture = new Texture2D(texture.width, texture.height);
			exportTexture.ReadPixels(new Rect(0, 0, destRenderTexture.width, destRenderTexture.height), 0, 0);
			exportTexture.Apply();

			var finalFilenamePath = ConstructImageFilenamePath(texture, outputPath, "png");
			File.WriteAllBytes(finalFilenamePath, exportTexture.EncodeToPNG());

			RenderTexture.ReleaseTemporary(destRenderTexture);
			if (Application.isEditor)
			{
				GameObject.DestroyImmediate(exportTexture);
			}
			else
			{
				GameObject.Destroy(exportTexture);
			}
		}

		private string ConstructImageFilenamePath(Texture2D texture, string outputPath, string enforceExtension = null)
		{
			var imagePath = _exportOptions.TexturePathRetriever(texture);
			if (string.IsNullOrEmpty(imagePath))
			{
				imagePath = Path.Combine(outputPath, texture.name);
			}

			var filenamePath = Path.Combine(outputPath, imagePath);
			if (!ExportFullPath)
			{
				filenamePath = outputPath + "/" + texture.name;
			}
			var file = new FileInfo(filenamePath);
			file.Directory.Create();
			if(!string.IsNullOrEmpty(enforceExtension)) {
				Path.ChangeExtension(filenamePath, enforceExtension);
				if (enforceExtension.StartsWith(".") && !filenamePath.EndsWith(enforceExtension))
					filenamePath += enforceExtension;
				else if (!enforceExtension.StartsWith(".") && !filenamePath.EndsWith(enforceExtension))
					filenamePath += "." + enforceExtension;
			}
			return filenamePath;
		}

		private void DeclareExtensionUsage(string extension, bool isRequired=false)
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

		private SceneId ExportScene(string name, Transform[] rootObjTransforms)
		{
			var scene = new GLTFScene();

			if (ExportNames)
			{
				scene.Name = name;
			}

			scene.Nodes = new List<NodeId>(rootObjTransforms.Length);
			foreach (var transform in rootObjTransforms)
			{
				scene.Nodes.Add(ExportNode(transform));
			}

			_root.Scenes.Add(scene);

			return new SceneId
			{
				Id = _root.Scenes.Count - 1,
				Root = _root
			};
		}

		private NodeId ExportNode(Transform nodeTransform)
		{
			var node = new Node();

			if (ExportNames)
			{
				node.Name = nodeTransform.name;
			}


			if (nodeTransform.GetComponent<UnityEngine.Animation>() || nodeTransform.GetComponent<UnityEngine.Animator>())
			{
				_animatedNodes.Add(nodeTransform);
			}
			if (nodeTransform.GetComponent<SkinnedMeshRenderer>())
			{
				_skinnedNodes.Add(nodeTransform);
			}

			//export camera attached to node
			Camera unityCamera = nodeTransform.GetComponent<Camera>();
			if (unityCamera != null && unityCamera.enabled)
			{
				node.Camera = ExportCamera(unityCamera);
			}

            Light unityLight = nodeTransform.GetComponent<Light>();
            if (unityLight != null && unityLight.enabled)
            {
                node.Light = ExportLight(unityLight);

                nodeTransform.rotation *= new Quaternion(0, -1, 0, 0);

                node.SetUnityTransform(nodeTransform);

                nodeTransform.rotation *= new Quaternion(0, 1, 0, 0);

                //node.SetUnityTransformForce(nodeTransform,false); //forward is flipped
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


			// Register nodes for animation parsing (could be disabled is animation is disables)
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
						_primOwner[new PrimKey { Mesh = smr.sharedMesh, Material = smr.sharedMaterial }] = node.Mesh;
					}
					else
					{
						var filter = prim.GetComponent<MeshFilter>();
						var renderer = prim.GetComponent<MeshRenderer>();
						_primOwner[new PrimKey { Mesh = filter.sharedMesh, Material = renderer.sharedMaterial }] = node.Mesh;
					}
				}
			}

			// children that are not primitives get added as child nodes
			if (nonPrimitives.Length > 0)
			{
				node.Children = new List<NodeId>(nonPrimitives.Length);
				foreach (var child in nonPrimitives)
				{
					node.Children.Add(ExportNode(child.transform));
				}
			}

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
			return (meshFilter && meshRenderer && meshRenderer.enabled) || (skinnedMeshRender && skinnedMeshRender.enabled);
		}
        private LightId ExportLight(Light unityLight)
        {
	        if (_root.ExtensionsUsed == null)
	        {
		        _root.ExtensionsUsed = new List<string>(new[] { "KHR_lights_punctual" });
	        }
	        else if (!_root.ExtensionsUsed.Contains("KHR_lights_punctual"))
	        {
		        _root.ExtensionsUsed.Add("KHR_lights_punctual");
	        }

            GLTFLight light;

            if (unityLight.type == LightType.Spot)
            {
                light = new GLTFSpotLight() { innerConeAngle = unityLight.spotAngle / 2 * Mathf.Deg2Rad*0.8f, outerConeAngle = unityLight.spotAngle / 2 * Mathf.Deg2Rad };
                //name
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
                light.range = unityLight.range;
                light.intensity = unityLight.intensity;
            }
            else if (unityLight.type == LightType.Directional)
            {
                light = new GLTFDirectionalLight();
                //name
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
                light.intensity = unityLight.intensity;
            }
            else if (unityLight.type == LightType.Point)
            {
                light = new GLTFPointLight();
                //name
                light.Name = unityLight.name;

                light.type = unityLight.type.ToString().ToLower();
                light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
                light.range = unityLight.range;
                light.intensity = unityLight.intensity;
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
			if (transform.gameObject.activeSelf)
			{
				if (ContainsValidRenderer(transform.gameObject))
				{
					prims.Add(transform.gameObject);
				}
			}
			for (var i = 0; i < childCount; i++)
			{
				var go = transform.GetChild(i).gameObject;

				// This seems to be a performance optimization but results in transforms that are detected as "primtives" not being animated
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
			// check if this set of primitives is already a mesh
			MeshId existingMeshId = null;
			var key = new PrimKey();
			foreach (var prim in primitives)
			{
				var smr = prim.GetComponent<SkinnedMeshRenderer>();
				if (smr != null)
				{
					key.Mesh = smr.sharedMesh;
					key.Material = smr.sharedMaterial;
				}
				else
				{
					var filter = prim.GetComponent<MeshFilter>();
					var renderer = prim.GetComponent<MeshRenderer>();
					key.Mesh = filter.sharedMesh;
					key.Material = renderer.sharedMaterial;
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
			if (mesh.Primitives.Count > 0)
			{
				_root.Meshes.Add(mesh);
				return id;
			}

			return null;
		}

		// a mesh *might* decode to multiple prims if there are submeshes
		private MeshPrimitive[] ExportPrimitive(GameObject gameObject, GLTFMesh mesh)
		{
			Mesh meshObj = null;
			SkinnedMeshRenderer smr = null;
			var filter = gameObject.GetComponent<MeshFilter>();
			if (filter != null)
			{
				meshObj = filter.sharedMesh;
			}
			else
			{
				smr = gameObject.GetComponent<SkinnedMeshRenderer>();
				meshObj = smr.sharedMesh;
			}
			if (meshObj == null)
			{
				Debug.LogError(string.Format("MeshFilter.sharedMesh on gameobject:{0} is missing , skipping", gameObject.name));
				return null;
			}

			var renderer = gameObject.GetComponent<MeshRenderer>();
			if (!renderer) smr = gameObject.GetComponent<SkinnedMeshRenderer>();

			if(!renderer && !smr)
			{
				Debug.LogWarning("GameObject does have neither renderer nor SkinnedMeshRenderer! " + gameObject.name, gameObject);
				return null;
			}
			var materialsObj = renderer ? renderer.sharedMaterials : smr.sharedMaterials;

			var prims = new MeshPrimitive[meshObj.subMeshCount];
			var vertices = meshObj.vertices;
			if (vertices.Length < 1)
			{
				Debug.LogWarning("MeshFilter does not contain any vertices, won't export: " + gameObject.name, gameObject);
				return null;
			}

			// don't export any more accessors if this mesh is already exported
			MeshPrimitive[] primVariations;
			if (_meshToPrims.TryGetValue(meshObj, out primVariations)
				&& meshObj.subMeshCount == primVariations.Length)
			{
				for (var i = 0; i < primVariations.Length; i++)
				{
					prims[i] = new MeshPrimitive(primVariations[i], _root)
					{
						Material = ExportMaterial(materialsObj[i])
					};
				}

				return prims;
			}

			AccessorId aPosition = null, aNormal = null, aTangent = null,
				aTexcoord0 = null, aTexcoord1 = null, aColor0 = null;

			aPosition = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(meshObj.vertices, SchemaExtensions.CoordinateSpaceConversionScale));

			if (meshObj.normals.Length != 0)
				aNormal = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(meshObj.normals, SchemaExtensions.CoordinateSpaceConversionScale));

			if (meshObj.tangents.Length != 0)
				aTangent = ExportAccessor(SchemaExtensions.ConvertVector4CoordinateSpaceAndCopy(meshObj.tangents, SchemaExtensions.TangentSpaceConversionScale));

			if (meshObj.uv.Length != 0)
				aTexcoord0 = ExportAccessor(SchemaExtensions.FlipTexCoordArrayVAndCopy(meshObj.uv));

			if (meshObj.uv2.Length != 0)
				aTexcoord1 = ExportAccessor(SchemaExtensions.FlipTexCoordArrayVAndCopy(meshObj.uv2));

			if (meshObj.colors.Length != 0)
				aColor0 = ExportAccessor(QualitySettings.activeColorSpace == ColorSpace.Linear ? meshObj.colors : meshObj.colors.ToLinear());

			MaterialId lastMaterialId = null;

			for (var submesh = 0; submesh < meshObj.subMeshCount; submesh++)
			{
				var primitive = new MeshPrimitive();

				var topology = meshObj.GetTopology(submesh);
				var indices = meshObj.GetIndices(submesh);
				if (topology == MeshTopology.Triangles) SchemaExtensions.FlipTriangleFaces(indices);

				primitive.Mode = GetDrawMode(topology);
				primitive.Indices = ExportAccessor(indices, true);

				primitive.Attributes = new Dictionary<string, AccessorId>();
				primitive.Attributes.Add(SemanticProperties.POSITION, aPosition);

				if (aNormal != null)
					primitive.Attributes.Add(SemanticProperties.NORMAL, aNormal);
				if (aTangent != null)
					primitive.Attributes.Add(SemanticProperties.TANGENT, aTangent);
				if (aTexcoord0 != null)
					primitive.Attributes.Add(SemanticProperties.TEXCOORD_0, aTexcoord0);
				if (aTexcoord1 != null)
					primitive.Attributes.Add(SemanticProperties.TEXCOORD_1, aTexcoord1);
				if (aColor0 != null)
					primitive.Attributes.Add(SemanticProperties.COLOR_0, aColor0);

				if (submesh < materialsObj.Length)
				{
					primitive.Material = ExportMaterial(materialsObj[submesh]);
					lastMaterialId = primitive.Material;
				}
				else
				{
					primitive.Material = lastMaterialId;
				}

				ExportBlendShapes(smr, meshObj, primitive, mesh);

				prims[submesh] = primitive;
			}
            //remove any prims that have empty triangles
            List<MeshPrimitive> listPrims = new List<MeshPrimitive>(prims);
            listPrims.RemoveAll(EmptyPrimitive);
            prims = listPrims.ToArray();

			_meshToPrims[meshObj] = prims;

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

        private MaterialId ExportMaterial(Material materialObj)
		{
            //TODO if material is null
			MaterialId id = GetMaterialId(_root, materialObj);
			if (id != null)
			{
				return id;
			}

			var material = new GLTFMaterial();

            if (materialObj == null)
            {
                if (ExportNames)
                {
                    material.Name = "null";
                }
                _materials.Add(materialObj);
                material.PbrMetallicRoughness = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };

                id = new MaterialId
                {
                    Id = _root.Materials.Count,
                    Root = _root
                };
                _root.Materials.Add(material);
                return id;
            }

			if (ExportNames)
			{
				material.Name = materialObj.name;
			}

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
					material.AlphaMode = AlphaMode.BLEND;
					break;
				default:
					material.AlphaMode = AlphaMode.OPAQUE;
					break;
			}

			material.DoubleSided = materialObj.HasProperty("_Cull") &&
				materialObj.GetInt("_Cull") == (float)CullMode.Off;

			if(materialObj.IsKeywordEnabled("_EMISSION"))
			{
				if (materialObj.HasProperty("_EmissionColor"))
				{
					var c = materialObj.GetColor("_EmissionColor");
					material.EmissiveFactor = c.ToNumericsColorLinear();
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
							Debug.LogErrorFormat("Can't export a {0} emissive texture in material {1}", emissionTex.GetType(), materialObj.name);
						}

					}
				}
			}
			if (materialObj.HasProperty("_BumpMap") && (materialObj.IsKeywordEnabled("_NORMALMAP") || materialObj.IsKeywordEnabled("_BUMPMAP")))
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
						Debug.LogErrorFormat("Can't export a {0} normal texture in material {1}", normalTex.GetType(), materialObj.name);
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
						Debug.LogErrorFormat("Can't export a {0} occlusion texture in material {1}", occTex.GetType(), materialObj.name);
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

			_materials.Add(materialObj);

			id = new MaterialId
			{
				Id = _root.Materials.Count,
				Root = _root
			};
			_root.Materials.Add(material);

			return id;
		}

		// Blend Shapes / Morph Targets
		// Adopted from Gary Hsu (bghgary)
		// https://github.com/bghgary/glTF-Tools-for-Unity/blob/master/UnityProject/Assets/Gltf/Editor/Exporter.cs
		private void ExportBlendShapes(SkinnedMeshRenderer smr, Mesh meshObj, MeshPrimitive primitive, GLTFMesh mesh)
		{
			if (smr != null && meshObj.blendShapeCount > 0)
			{
				List<Dictionary<string, AccessorId>> targets = new List<Dictionary<string, AccessorId>>(meshObj.blendShapeCount);
				List<Double> weights = new List<double>(meshObj.blendShapeCount);
				List<string> targetNames = new List<string>(meshObj.blendShapeCount);

				for (int blendShapeIndex = 0; blendShapeIndex < meshObj.blendShapeCount; blendShapeIndex++)
				{

					targetNames.Add(meshObj.GetBlendShapeName(blendShapeIndex));
					// As described above, a blend shape can have multiple frames.  Given that glTF only supports a single frame
					// per blend shape, we'll always use the final frame (the one that would be for when 100% weight is applied).
					int frameIndex = meshObj.GetBlendShapeFrameCount(blendShapeIndex) - 1;

					var deltaVertices = new Vector3[meshObj.vertexCount];
					var deltaNormals = new Vector3[meshObj.vertexCount];
					var deltaTangents = new Vector3[meshObj.vertexCount];
					meshObj.GetBlendShapeFrameVertices(blendShapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);

					targets.Add(new Dictionary<string, AccessorId>
						{
							{ SemanticProperties.POSITION, ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy( deltaVertices, SchemaExtensions.CoordinateSpaceConversionScale)) },
							{ SemanticProperties.NORMAL, ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(deltaNormals,SchemaExtensions.CoordinateSpaceConversionScale))},
							{ SemanticProperties.TANGENT, ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(deltaTangents, SchemaExtensions.CoordinateSpaceConversionScale)) },
						});

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
					weights.Add(smr.GetBlendShapeWeight(blendShapeIndex) / 100);
				}

				mesh.Weights = weights;
				primitive.Targets = targets;
				primitive.TargetNames = targetNames;
			}
		}

		private bool IsPBRMetallicRoughness(Material material)
		{
			return material.HasProperty("_Metallic") && (material.HasProperty("_MetallicGlossMap") || material.HasProperty("_Glossiness"));
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
				// difficult choice here: some shaders might support texture transform per-texture, others use the main transform.
				offset = mat.mainTextureOffset;
				scale = mat.mainTextureScale;
				// offset.x = 1 - offset.x;
				// return;
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
				new GLTF.Math.Vector2(offset.x, -offset.y),
				0, // TODO: support rotation
				new GLTF.Math.Vector2(scale.x, scale.y),
				0 // TODO: support UV channels
			);
		}

		private NormalTextureInfo ExportNormalTextureInfo(
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

		private PbrMetallicRoughness ExportPBRMetallicRoughness(Material material)
		{
			var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };

			if (material.HasProperty("_Color"))
			{
				pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();
			}
			else if (material.HasProperty("_BaseColor"))
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
				var mainTex = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : material.GetTexture("_BaseMap");

				if (mainTex)
				{
					if(mainTex is Texture2D)
					{
						pbr.BaseColorTexture = ExportTextureInfo(mainTex, TextureMapType.Main);
						ExportTextureTransform(pbr.BaseColorTexture, material, "_MainTex");
					}
					else
					{
						Debug.LogErrorFormat("Can't export a {0} base texture in material {1}", mainTex.GetType(), material.name);
					}
				}
			}

			if (material.HasProperty("_Metallic"))
			{
				var metallicGlossMap = material.GetTexture("_MetallicGlossMap");
				pbr.MetallicFactor = material.GetFloat("_Metallic");
			}

			if (material.HasProperty("_Glossiness"))
			{
				var metallicGlossMap = material.GetTexture("_MetallicGlossMap");
				float gms = material.GetFloat("_GlossMapScale");
				float gloss = material.GetFloat("_Glossiness");
				pbr.RoughnessFactor = (metallicGlossMap && material.HasProperty("_GlossMapScale")) ? material.GetFloat("_GlossMapScale") : 1.0 - material.GetFloat("_Glossiness");
			}

			if (material.HasProperty("_MetallicGlossMap"))
			{
				var mrTex = material.GetTexture("_MetallicGlossMap");

				if (mrTex != null)
				{
					if(mrTex is Texture2D)
					{
						pbr.MetallicRoughnessTexture = ExportTextureInfo(mrTex, TextureMapType.MetallicGloss);
						ExportTextureTransform(pbr.MetallicRoughnessTexture, material, "_MetallicGlossMap");
					}
					else
					{
						Debug.LogErrorFormat("Can't export a {0} metallic smoothness texture in material {1}", mrTex.GetType(), material.name);
					}
				}
			}

			return pbr;
		}

		private void ExportUnlit(GLTFMaterial def, Material material){

			const string extname = KHR_MaterialsUnlitExtensionFactory.EXTENSION_NAME;
			DeclareExtensionUsage( extname, true );
			def.AddExtension( extname, new KHR_MaterialsUnlitExtension());

			var pbr = new PbrMetallicRoughness();

			if (material.HasProperty("_Color"))
			{
				pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();
			}

			if (material.HasProperty("_MainTex"))
			{
				var mainTex = material.GetTexture("_MainTex");
				if (mainTex != null)
				{
					if(mainTex is Texture2D)
					{
						pbr.BaseColorTexture = ExportTextureInfo(mainTex, TextureMapType.Main);
						ExportTextureTransform(pbr.BaseColorTexture, material, "_MainTex");
					}
					else
					{
						Debug.LogErrorFormat("Can't export a {0} base texture in material {1}", mainTex.GetType(), material.name);
					}
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

			if (materialObj.HasProperty("_MainTex"))
			{
				var mainTex = materialObj.GetTexture("_MainTex");

				if (mainTex != null)
				{
					if (mainTex is Texture2D)
					{
						diffuseTexture = ExportTextureInfo(mainTex, TextureMapType.Main);
						ExportTextureTransform(diffuseTexture, materialObj, "_MainTex");
					}
					else
					{
						Debug.LogErrorFormat("Can't export a {0} diffuse texture in material {1}", mainTex.GetType(), materialObj.name);
					}
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

				if (mgTex != null)
				{
					if (mgTex is Texture2D)
					{
						specularGlossinessTexture = ExportTextureInfo(mgTex, TextureMapType.SpecGloss);
						ExportTextureTransform(specularGlossinessTexture, materialObj, "_SpecGlossMap");
					}
					else
					{
						Debug.LogErrorFormat("Can't export a {0} specular glossiness texture in material {1}", mgTex.GetType(), materialObj.name);
					}
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

				if (lmTex != null)
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

		private TextureInfo ExportTextureInfo(Texture texture, TextureMapType textureMapType)
		{
			var info = new TextureInfo();

			info.Index = ExportTexture(texture, textureMapType);

			return info;
		}

		private TextureId ExportTexture(Texture textureObj, TextureMapType textureMapType)
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

		private string GetImageOutputPath(Texture texture)
		{
			var imagePath = _exportOptions.TexturePathRetriever(texture);
			if (string.IsNullOrEmpty(imagePath))
			{
				imagePath = texture.name;
			}

			var filenamePath = imagePath;
			var isGltfCompatible = IsPng(imagePath) || IsJpeg(imagePath);

			if (ExportFullPath)
			{
				if (!isGltfCompatible)
				{
					filenamePath = Path.ChangeExtension(imagePath, ".png");
				}
			}
			else
			{
				filenamePath = Path.GetFileName(filenamePath);
				if (!isGltfCompatible)
				{
					filenamePath = Path.ChangeExtension(texture.name, ".png");
				}
			}

			return filenamePath;
		}

		private ImageId ExportImage(Texture texture, TextureMapType texturMapType)
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

            _imageInfos.Add(new ImageInfo
			{
				texture = texture as Texture2D,
				textureMapType = texturMapType
			});

			var filenamePath = GetImageOutputPath(texture);

			image.Uri = Uri.EscapeUriString(filenamePath);

			id = new ImageId
			{
				Id = _root.Images.Count,
				Root = _root
			};

			_root.Images.Add(image);

			return id;
		}

		bool TryGetTextureDataFromDisk(Texture texture, out string path, out byte[] data)
		{
#if UNITY_EDITOR
			if (Application.isEditor && UnityEditor.AssetDatabase.Contains(texture))
			{
				path = UnityEditor.AssetDatabase.GetAssetPath(texture);
				if (File.Exists(path))
				{
					data = File.ReadAllBytes(path);
					return true;
				}
			}
#endif
			path = null;
			data = null;
			return false;
		}

		bool IsPng(string filename)
		{
			return Path.GetExtension(filename).EndsWith("png", StringComparison.InvariantCultureIgnoreCase);
		}

		bool IsJpeg(string filename)
		{
			return Path.GetExtension(filename).EndsWith("jpg", StringComparison.InvariantCultureIgnoreCase) || Path.GetExtension(filename).EndsWith("jpeg", StringComparison.InvariantCultureIgnoreCase);
		}

		private ImageId ExportImageInternalBuffer(UnityEngine.Texture texture, TextureMapType texturMapType)
		{
		    if (texture == null)
		    {
				throw new Exception("texture can not be NULL.");
		    }

		    var image = new GLTFImage();
			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			bool wasAbleToExportFromDisk = false;

			if(TryExportTexturesFromDisk && TryGetTextureDataFromDisk(texture, out string path, out byte[] imageBytes))
			{
				if(IsPng(path))
				{
					image.MimeType = "image/png";
					_bufferWriter.Write(imageBytes);
					wasAbleToExportFromDisk = true;
				}
				else if(IsJpeg(path))
				{
					image.MimeType = "image/jpeg";
					_bufferWriter.Write(imageBytes);
					wasAbleToExportFromDisk = true;
				}
				else
				{
					Debug.Log("Texture can't be exported from disk: " + path + ". Only PNG & JPEG are supported. The texture will be re-encoded as PNG.", texture);
				}
			}


			if(!wasAbleToExportFromDisk)
		    {
				image.MimeType = "image/png";

				// TODO we could make sure texture size is power-of-two here

				var destRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
				GL.sRGBWrite = true;
				switch (texturMapType)
				{
					case TextureMapType.MetallicGloss:
					Graphics.Blit(texture, destRenderTexture, _metalGlossChannelSwapMaterial);
					break;
					case TextureMapType.Bump:
					Graphics.Blit(texture, destRenderTexture, _normalChannelMaterial);
					break;
					default:
					Graphics.Blit(texture, destRenderTexture);
					break;
				}

				var exportTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
				exportTexture.ReadPixels(new Rect(0, 0, destRenderTexture.width, destRenderTexture.height), 0, 0);
				exportTexture.Apply();

				var pngImageData = exportTexture.EncodeToPNG();
				_bufferWriter.Write(pngImageData);

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

			// Check for potential warnings in GLTF validation
			if (!Mathf.IsPowerOfTwo(texture.width) || !Mathf.IsPowerOfTwo(texture.height))
			{
				Debug.LogWarning("Validation Warning: " + "Image has non-power-of-two dimensions: " + texture.width + "x" + texture.height + ".", texture);
			}

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
					Debug.LogWarning("Unsupported Texture.wrapMode: " + texture.wrapMode);
					sampler.WrapS = WrapMode.Repeat;
					sampler.WrapT = WrapMode.Repeat;
					break;
			}

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
					Debug.LogWarning("Unsupported Texture.filterMode: " + texture.filterMode);
					sampler.MinFilter = MinFilterMode.LinearMipmapLinear;
					sampler.MagFilter = MagFilterMode.Linear;
					break;
			}

			samplerId = new SamplerId
			{
				Id = _root.Samplers.Count,
				Root = _root
			};

			_root.Samplers.Add(sampler);

			return samplerId;
		}

		private AccessorId ExportAccessor(int[] arr, bool isIndices = false)
		{
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

			return id;
		}

		private AccessorId ExportAccessor(Vector2[] arr)
		{
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

			return id;
		}

		private AccessorId ExportAccessor(Vector3[] arr)
		{
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

			foreach (var vec in arr)
			{
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
			}

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(Vector4[] arr)
		{
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

			foreach (var vec in arr)
			{
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
				_bufferWriter.Write(vec.w);
			}

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(Color[] arr)
		{
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

			return id;
		}

		private BufferViewId ExportBufferView(uint byteOffset, uint byteLength)
		{
			var bufferView = new BufferView
			{
				Buffer = _bufferId,
				ByteOffset = byteOffset,
				ByteLength = byteLength
			};

			var id = new BufferViewId
			{
				Id = _root.BufferViews.Count,
				Root = _root
			};

			_root.BufferViews.Add(bufferView);

			return id;
		}

		public MaterialId GetMaterialId(GLTFRoot root, Material materialObj)
		{
			for (var i = 0; i < _materials.Count; i++)
			{
				if (_materials[i] == materialObj)
				{
					return new MaterialId
					{
						Id = i,
						Root = root
					};
				}
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


		public enum AnimationKeyRotationType
		{
			Unknown,
			Quaternion,
			Euler
		};

		private struct TargetCurveSet
		{
			public AnimationCurve[] translationCurves;
			public AnimationCurve[] rotationCurves;
			public AnimationCurve[] scaleCurves;
			public AnimationKeyRotationType rotationType;

			public void Init()
			{
				translationCurves = new AnimationCurve[3];
				rotationCurves = new AnimationCurve[4];
				scaleCurves = new AnimationCurve[3];
			}
		}

		// Parses Animation/Animator component and generate a glTF animation for the active clip
		// This may need additional work to fully support animatorControllers
		public void ExportAnimationFromNode(ref Transform transform)
		{
			Animator animator = transform.GetComponent<Animator>();
			if (animator != null)
			{
#if UNITY_EDITOR
                AnimationClip[] clips = UnityEditor.AnimationUtility.GetAnimationClips(transform.gameObject);
                var animatorController = animator.runtimeAnimatorController as AnimatorController;

                ExportAnimationClips(transform, clips, animatorController);
#endif
			}

			UnityEngine.Animation animation = transform.GetComponent<UnityEngine.Animation>();
			if (animation != null)
			{
#if UNITY_EDITOR
                AnimationClip[] clips = UnityEditor.AnimationUtility.GetAnimationClips(transform.gameObject);
                ExportAnimationClips(transform, clips);
#endif
			}

			float GetAnimatorMotionSpeedForClip(AnimationClip clip, AnimatorController animatorController)
			{
				if (!animatorController) return 1f;

				foreach (var layer in animatorController.layers)
				{
					foreach (var state in layer.stateMachine.states)
					{
						// find a matching clip in the animator
						if (state.state.motion is AnimationClip c && c == clip)
							return state.state.speed * (state.state.speedParameterActive ? animator.GetFloat(state.state.speedParameter) : 1f);
					}
				}

				return 1f;
			}

			/// <summary>Creates GLTFAnimation for each clip and adds it to the _root</summary>
			void ExportAnimationClips(Transform nodeTransform, AnimationClip[] clips, AnimatorController animatorController = null)
			{
				for (int i = 0; i < clips.Length; i++)
				{
					// track animation clips to ensure we don't save duplicates
					if (_animationClips.Contains(clips[i])) continue;

					_animationClips.Add(clips[i]);

					GLTFAnimation anim = new GLTFAnimation();
					anim.Name = clips[i].name;
					ConvertClipToGLTFAnimation(ref clips[i], ref nodeTransform, ref anim, animatorController ? GetAnimatorMotionSpeedForClip(clips[i], animatorController) : 1f);

					if (anim.Channels.Count > 0 && anim.Samplers.Count > 0)
					{
						_root.Animations.Add(anim);
					}
				}
			}
		}

		private int getTargetIdFromTransform(ref Transform transform)
		{
			if (_exportedTransforms.ContainsKey(transform.GetInstanceID()))
			{
				return _exportedTransforms[transform.GetInstanceID()];
			}
			else
			{
				Debug.LogError("Transform is not part of _exportedTransforms: " + transform.name + " " + transform.GetInstanceID(), transform);
				return 0;
			}
		}



		private void ConvertClipToGLTFAnimation(ref AnimationClip clip, ref Transform transform, ref GLTF.Schema.GLTFAnimation animation, float speed = 1f)
		{
			// Generate GLTF.Schema.AnimationChannel and GLTF.Schema.AnimationSampler
			// 1 channel per node T/R/S, one sampler per node T/R/S
			// Need to keep a list of nodes to convert to indexes

			// 1. browse clip, collect all curves and create a TargetCurveSet for each target
			Dictionary<string, TargetCurveSet> targetCurvesBinding = new Dictionary<string, TargetCurveSet>();
			CollectClipCurves(clip, ref targetCurvesBinding);

			//foreach(var str in targetCurvesBinding)
			//{
			//	Debug.Log(clip.name + ": " + transform.name + ": " + str);
			//}

			// Baking needs all properties, fill missing curves with transform data in 2 keyframes (start, endTime)
			// where endTime is clip duration
			// Note: we should avoid creating curves for a property if none of it's components is animated

			// GenerateMissingCurves(clip.length, ref transform, ref targetCurvesBinding);

			if (BakeAnimationData)
			{
				// Debug.Log("<b>Animated Properties: " + targetCurvesBinding.Count + "</b>");
				// Bake animation for all animated nodes
				foreach (string target in targetCurvesBinding.Keys)
				{
					Transform targetTr = target.Length > 0 ? transform.Find(target) : transform;
					if (targetTr == null || targetTr.GetComponent<SkinnedMeshRenderer>())
					{
						continue;
					}

					TargetCurveSet current = targetCurvesBinding[target];
					int haveTranslationCurves = current.translationCurves[0] != null ? current.translationCurves.Length : 0;
					int haveScaleCurves = current.scaleCurves[0] != null ? current.scaleCurves.Length : 0;
					int haveRotationCurves = current.rotationCurves[0] != null ? current.rotationCurves.Length : 0;

					// Initialize data
					// Bake and populate animation data
					float[] times = null;
					Vector3[] positions = null;
					Vector3[] scales = null;
					Vector4[] rotations = null;

					var speedMultiplier = Mathf.Clamp(speed, 0.01f, Mathf.Infinity);
					if (!BakeCurveSet(targetCurvesBinding[target], clip.length, AnimationBakingFramerate, speedMultiplier, ref times, ref positions, ref rotations, ref scales))
					{
						Debug.LogWarning("Warning: Export failed for animation curve " + target + " in " + clip + " from " + transform, transform);
					}

					int channelTargetId = getTargetIdFromTransform(ref targetTr);

					bool haveAnimation = positions != null || rotations != null || scales != null;

					if(haveAnimation)
					{
						AccessorId timeAccessor = ExportAccessor(times);

						// Translation
						if(positions != null)
						{
							AnimationChannel Tchannel = new AnimationChannel();
							AnimationChannelTarget TchannelTarget = new AnimationChannelTarget();
							TchannelTarget.Path = GLTFAnimationChannelPath.translation;
							TchannelTarget.Node = new NodeId
							{
								Id = channelTargetId,
								Root = _root
							};

							Tchannel.Target = TchannelTarget;

							AnimationSampler Tsampler = new AnimationSampler();
							Tsampler.Input = timeAccessor;
							Tsampler.Output = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(positions, SchemaExtensions.CoordinateSpaceConversionScale));
							Tchannel.Sampler = new AnimationSamplerId
							{
								Id = animation.Samplers.Count,
								GLTFAnimation = animation,
								Root = _root
							};

							animation.Samplers.Add(Tsampler);
							animation.Channels.Add(Tchannel);
						}

						if(rotations != null)
						{
							// Rotation
							AnimationChannel Rchannel = new AnimationChannel();
							AnimationChannelTarget RchannelTarget = new AnimationChannelTarget();
							RchannelTarget.Path = GLTFAnimationChannelPath.rotation;
							RchannelTarget.Node = new NodeId
							{
								Id = channelTargetId,
								Root = _root
							};

							Rchannel.Target = RchannelTarget;

							AnimationSampler Rsampler = new AnimationSampler();
							Rsampler.Input = timeAccessor; // Float, for time
							Rsampler.Output = ExportAccessor(rotations, true); // Vec4 for
							Rchannel.Sampler = new AnimationSamplerId
							{
								Id = animation.Samplers.Count,
								GLTFAnimation = animation,
								Root = _root
							};

							animation.Samplers.Add(Rsampler);
							animation.Channels.Add(Rchannel);
						}

						if(scales != null)
						{
							// Scale
							AnimationChannel Schannel = new AnimationChannel();
							AnimationChannelTarget SchannelTarget = new AnimationChannelTarget();
							SchannelTarget.Path = GLTFAnimationChannelPath.scale;
							SchannelTarget.Node = new NodeId
							{
								Id = channelTargetId,
								Root = _root
							};

							Schannel.Target = SchannelTarget;

							AnimationSampler Ssampler = new AnimationSampler();
							Ssampler.Input = timeAccessor; // Float, for time
							Ssampler.Output = ExportAccessor(scales); // Vec3 for scale
							Schannel.Sampler = new AnimationSamplerId
							{
								Id = animation.Samplers.Count,
								GLTFAnimation = animation,
								Root = _root
							};

							animation.Samplers.Add(Ssampler);
							animation.Channels.Add(Schannel);
						}
					}
				}
			}
			else
			{
				Debug.LogError("Only baked animation is supported for now. Skipping animation");
			}

		}

		private void CollectClipCurves(AnimationClip clip, ref Dictionary<string, TargetCurveSet> targetCurves)
		{
			#if UNITY_EDITOR
			foreach (var binding in UnityEditor.AnimationUtility.GetCurveBindings(clip))
			{
				AnimationCurve curve = UnityEditor.AnimationUtility.GetEditorCurve(clip, binding);

				if (!targetCurves.ContainsKey(binding.path))
				{
					TargetCurveSet curveSet = new TargetCurveSet();
					curveSet.Init();
					targetCurves.Add(binding.path, curveSet);
				}

				TargetCurveSet current = targetCurves[binding.path];
				if (binding.propertyName.Contains("m_LocalPosition"))
				{
					if (binding.propertyName.Contains(".x"))
						current.translationCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.translationCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.translationCurves[2] = curve;
				}
				else if (binding.propertyName.Contains("m_LocalScale"))
				{
					if (binding.propertyName.Contains(".x"))
						current.scaleCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.scaleCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.scaleCurves[2] = curve;
				}
				else if (binding.propertyName.ToLower().Contains("localrotation"))
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
				else if (binding.propertyName.ToLower().Contains("localeuler"))
				{
					current.rotationType = AnimationKeyRotationType.Euler;
					if (binding.propertyName.Contains(".x"))
						current.rotationCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.rotationCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.rotationCurves[2] = curve;
				}
				targetCurves[binding.path] = current;
			}
			#endif
		}

		private void GenerateMissingCurves(float endTime, ref Transform tr, ref Dictionary<string, TargetCurveSet> targetCurvesBinding)
		{
			foreach (string target in targetCurvesBinding.Keys)
			{
				Transform targetTr = target.Length > 0 ? tr.Find(target) : tr;
				if (targetTr == null)
					continue;

				TargetCurveSet current = targetCurvesBinding[target];
				if (current.translationCurves[0] == null)
				{
					current.translationCurves[0] = CreateConstantCurve(targetTr.localPosition.x, endTime);
					current.translationCurves[1] = CreateConstantCurve(targetTr.localPosition.y, endTime);
					current.translationCurves[2] = CreateConstantCurve(targetTr.localPosition.z, endTime);
				}

				if (current.scaleCurves[0] == null)
				{
					current.scaleCurves[0] = CreateConstantCurve(targetTr.localScale.x, endTime);
					current.scaleCurves[1] = CreateConstantCurve(targetTr.localScale.y, endTime);
					current.scaleCurves[2] = CreateConstantCurve(targetTr.localScale.z, endTime);
				}

				if (current.rotationCurves[0] == null)
				{
					current.rotationCurves[0] = CreateConstantCurve(targetTr.localRotation.x, endTime);
					current.rotationCurves[1] = CreateConstantCurve(targetTr.localRotation.y, endTime);
					current.rotationCurves[2] = CreateConstantCurve(targetTr.localRotation.z, endTime);
					current.rotationCurves[3] = CreateConstantCurve(targetTr.localRotation.w, endTime);
				}
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

		private bool BakeCurveSet(TargetCurveSet curveSet, float length, float bakingFramerate, float speedMultiplier, ref float[] times, ref Vector3[] positions, ref Vector4[] rotations, ref Vector3[] scales)
		{
			int nbSamples = Mathf.Max(1, Mathf.CeilToInt(length * bakingFramerate));
			float deltaTime = length / nbSamples;

			bool haveTranslationKeys = curveSet.translationCurves != null && curveSet.translationCurves.Length > 0 && curveSet.translationCurves[0] != null;
			bool haveRotationKeys = curveSet.rotationCurves != null && curveSet.rotationCurves.Length > 0 && curveSet.rotationCurves[0] != null;
			bool haveScaleKeys = curveSet.scaleCurves != null && curveSet.scaleCurves.Length > 0 && curveSet.scaleCurves[0] != null;

			if(haveScaleKeys)
			{
				if(curveSet.scaleCurves.Length < 3)
				{
					Debug.LogError("Have Scale Animation, but not all properties are animated. Ignoring for now");
					return false;
				}
				bool anyIsNull = false;
				foreach (var sc in curveSet.scaleCurves)
					anyIsNull |= sc == null;

				if (anyIsNull)
				{
					Debug.LogWarning("A scale curve has at least one null property curve! Ignoring");
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
					Debug.LogWarning("A rotation curve has at least one null property curve! Ignoring");
					haveRotationKeys = false;
				}
			}

			if(!haveTranslationKeys && !haveRotationKeys && !haveScaleKeys)
			{
				Debug.LogWarning("No keys in curve set");
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
			}

			// remove keys again where prev/next keyframe are identical
			List<float> t2 = new List<float>();
			List<Vector3> p2 = new List<Vector3>();
			List<Vector3> s2 = new List<Vector3>();
			List<Vector4> r2 = new List<Vector4>();

			t2.Add(times[0]);
			if (haveTranslationKeys) p2.Add(positions[0]);
			if (haveRotationKeys) r2.Add(rotations[0]);
			if (haveScaleKeys) s2.Add(scales[0]);

			for (int i = 1; i < times.Length - 1; i++)
			{
				// check identical
				bool isIdentical = true;
				if (haveTranslationKeys)
					isIdentical &= positions[i - 1] == positions[i] && positions[i] == positions[i + 1];
				if(haveRotationKeys)
					isIdentical &= rotations[i - 1] == rotations[i] && rotations[i] == rotations[i + 1];
				if (haveScaleKeys)
					isIdentical &= scales[i - 1] == scales[i] && scales[i] == scales[i + 1];

				if(!isIdentical)
				{
					t2.Add(times[i]);
					if (haveTranslationKeys) p2.Add(positions[i]);
					if (haveRotationKeys) r2.Add(rotations[i]);
					if (haveScaleKeys) s2.Add(scales[i]);
				}
			}
			var max = times.Length - 1;

			t2.Add(times[max]);
			if (haveTranslationKeys) p2.Add(positions[max]);
			if (haveRotationKeys) r2.Add(rotations[max]);
			if (haveScaleKeys) s2.Add(scales[max]);

			// Debug.Log("Keyframes before compression: " + times.Length + "; " + "Keyframes after compression: " + t2.Count);

			times = t2.ToArray();
			if (haveTranslationKeys) positions = p2.ToArray();
			if (haveRotationKeys) rotations = r2.ToArray();
			if (haveScaleKeys) scales = s2.ToArray();

			return true;
		}

		private UnityEngine.Mesh getMesh(GameObject gameObject)
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

		private UnityEngine.Material getMaterial(GameObject gameObject)
		{
			if (gameObject.GetComponent<MeshRenderer>())
			{
				return gameObject.GetComponent<MeshRenderer>().sharedMaterial;
			}

			if (gameObject.GetComponent<SkinnedMeshRenderer>())
			{
				return gameObject.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
			}

			return null;
		}

		private void ExportSkinFromNode(Transform transform)
		{
			PrimKey key = new PrimKey();
			UnityEngine.Mesh mesh = getMesh(transform.gameObject);
			key.Mesh = mesh;
			key.Material = getMaterial(transform.gameObject);
			MeshId val;
			if (!_primOwner.TryGetValue(key, out val))
			{
				Debug.Log("No mesh found for skin");
				return;
			}
			SkinnedMeshRenderer skin = transform.GetComponent<SkinnedMeshRenderer>();
			GLTF.Schema.Skin gltfSkin = new Skin();

			for (int i = 0; i < skin.bones.Length; ++i)
			{
				gltfSkin.Joints.Add(
					new NodeId
					{
						Id = _exportedTransforms[skin.bones[i].GetInstanceID()],
						Root = _root
					});
			}

			gltfSkin.InverseBindMatrices = ExportAccessor(mesh.bindposes);

			Vector4[] bones = boneWeightToBoneVec4(mesh.boneWeights);
			Vector4[] weights = boneWeightToWeightVec4(mesh.boneWeights);

			GLTF.Schema.GLTFMesh gltfMesh = _root.Meshes[val.Id];
			foreach (MeshPrimitive prim in gltfMesh.Primitives)
			{
				if (!prim.Attributes.ContainsKey("JOINTS_0"))
					prim.Attributes.Add("JOINTS_0", ExportAccessorUint(bones));
				if (!prim.Attributes.ContainsKey("WEIGHTS_0"))
					prim.Attributes.Add("WEIGHTS_0", ExportAccessor(weights));
			}

			_root.Nodes[_exportedTransforms[transform.GetInstanceID()]].Skin = new SkinId() { Id = _root.Skins.Count, Root = _root };
			_root.Skins.Add(gltfSkin);
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

			foreach (var vec in arr)
			{
				_bufferWriter.Write((ushort)vec.x);
				_bufferWriter.Write((ushort)vec.y);
				_bufferWriter.Write((ushort)vec.z);
				_bufferWriter.Write((ushort)vec.w);
			}

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		// This is used for Quaternions / Rotations
		private AccessorId ExportAccessor(Vector4[] arr, bool switchHandedness = false)
		{
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
			a0 = switchHandedness ? a0.switchHandedness() : a0;
			a0 = a0.normalized;
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
				cur = switchHandedness ? cur.switchHandedness() : cur;
				cur = cur.normalized;

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

			foreach (var vec in arr)
			{
				Vector4 vect = switchHandedness ? vec.switchHandedness() : vec;
				vect = vect.normalized;
				_bufferWriter.Write(vect.x);
				_bufferWriter.Write(vect.y);
				_bufferWriter.Write(vect.z);
				_bufferWriter.Write(vect.w);
			}

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(float[] arr)
		{
			var count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.SCALAR;

			float min = arr[0];
			float max = arr[0];

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

			accessor.Min = new List<double> { min };
			accessor.Max = new List<double> { max };

			AlignToBoundary(_bufferWriter.BaseStream, 0x00);
			uint byteOffset = CalculateAlignment((uint)_bufferWriter.BaseStream.Position, 4);

			foreach (var value in arr)
			{
				_bufferWriter.Write(value);
			}

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};

			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(Matrix4x4[] arr)
		{
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

			uint byteLength = CalculateAlignment((uint)_bufferWriter.BaseStream.Position - byteOffset, 4);

			accessor.BufferView = ExportBufferView((uint)byteOffset, (uint)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

	}
}
