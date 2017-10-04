using GLTF;
using GLTF.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Loader;

namespace UnityGLTF
{
	public class GLTFSceneImporter
	{
		public enum MaterialType
		{
			PbrMetallicRoughness,
			PbrSpecularGlossiness,
			CommonConstant,
			CommonPhong,
			CommonBlinn,
			CommonLambert
		}

		public int MaximumLod = 300;
		public Transform SceneParent { get; set; }

		protected GameObject _lastLoadedScene;
		protected readonly Dictionary<MaterialType, Shader> _shaderCache = new Dictionary<MaterialType, Shader>();
		protected readonly GLTF.Schema.Material DefaultMaterial = new GLTF.Schema.Material();
		protected string _gltfFileName;
		protected Stream _gltfStream;
		protected GLTFRoot _gltfRoot;
		protected AssetCache _assetCache;
		protected AsyncAction _asyncAction;
		protected byte[] _gltfData;
		protected ILoader _loader;

		/// <summary>
		/// Creates a GLTFSceneBuilder object which will be able to construct a scene based off a url
		/// </summary>
		/// <param name="gltfFileName">glTF file relative to data loader path</param>
		/// <param name="parent"></param>
		public GLTFSceneImporter(string gltfFileName, ILoader externalDataLoader) : this(externalDataLoader)
		{
			_gltfFileName = gltfFileName;
		}

		public GLTFSceneImporter(GLTFRoot rootNode, ILoader externalDataLoader, Stream glbStream = null) : this(externalDataLoader)
		{
			_gltfRoot = rootNode;
			_loader = externalDataLoader;
			_gltfStream = glbStream;
		}

		private GLTFSceneImporter(ILoader externalDataLoader)
		{
			_loader = externalDataLoader;
			_asyncAction = new AsyncAction();
		}

		public GameObject LastLoadedScene
		{
			get { return _lastLoadedScene; }
		}

		/// <summary>
		/// Configures shaders in the shader cache for a given material type
		/// </summary>
		/// <param name="type">Material type to setup shader for</param>
		/// <param name="shader">Shader object to apply</param>
		public virtual void SetShaderForMaterialType(MaterialType type, Shader shader)
		{
			_shaderCache.Add(type, shader);
		}
		
		/// <summary>
		/// Loads via a web call the gltf file
		/// </summary>
		/// <returns></returns>
		public void LoadJson(string jsonFilePath)
		{
			_gltfStream = _loader.LoadJSON(jsonFilePath);
			_gltfRoot = GLTFParser.ParseJson(_gltfStream);
		}
		
		/// <summary>
		/// Loads a GLTF scene into unity
		/// </summary>
		/// <param name="sceneIndex">Index into scene to load. -1 means load default</param>
		/// <param name="isMultithreaded">Whether to do loading operation on a thread</param>
		public IEnumerator LoadScene(int sceneIndex = -1, bool isMultithreaded = false)
		{
			if(_gltfRoot == null)
			{
				LoadJson(_gltfFileName);
			}
			yield return ImportScene(sceneIndex, isMultithreaded);
		}

		public GameObject LoadNode(int nodeIndex)
		{
			if (_gltfRoot == null)
			{
				throw new InvalidOperationException("GLTF root must first be loaded and parsed");
			}

			if (_assetCache == null)
			{
				InitializeAssetCache();
			}

			return _LoadNode(nodeIndex);
		}

		private GameObject _LoadNode(int nodeIndex)
		{
			if(nodeIndex >= _gltfRoot.Nodes.Count)
			{
				throw new ArgumentException("nodeIndex is out of range");
			}

			return CreateNode(_gltfRoot.Nodes[nodeIndex]);
		}

		protected void InitializeAssetCache()
		{
			_assetCache = new AssetCache(
				_gltfRoot.Images != null ? _gltfRoot.Images.Count : 0,
				_gltfRoot.Textures != null ? _gltfRoot.Textures.Count : 0,
				_gltfRoot.Materials != null ? _gltfRoot.Materials.Count : 0,
				_gltfRoot.Buffers != null ? _gltfRoot.Buffers.Count : 0,
				_gltfRoot.Meshes != null ? _gltfRoot.Meshes.Count : 0
				);
		}
		
		/// <summary>
		/// Creates a scene based off loaded JSON. Includes loading in binary and image data to construct the meshes required.
		/// </summary>
		/// <param name="sceneIndex">The index of scene in gltf file to load</param>
		/// <param name="isMultithreaded">Whether to use a thread to do loading</param>
		/// <returns></returns>
		protected IEnumerator ImportScene(int sceneIndex = -1, bool isMultithreaded = false)
		{
			Scene scene;
			if (sceneIndex >= 0 && sceneIndex < _gltfRoot.Scenes.Count)
			{
				scene = _gltfRoot.Scenes[sceneIndex];
			}
			else
			{
				scene = _gltfRoot.GetDefaultScene();
			}

			if (scene == null)
			{
				throw new Exception("No default scene in gltf file.");
			}
			

			if (_lastLoadedScene == null)
			{
				InitializeAssetCache();
				
				if (_gltfRoot.Buffers != null)
				{
					// todo add fuzzing to verify that buffers are before uri
					for (int i = 0; i < _gltfRoot.Buffers.Count; ++i)
					{
						GLTF.Schema.Buffer buffer = _gltfRoot.Buffers[i];
						if (_assetCache.BufferCache[i] == null)
						{
							_assetCache.BufferCache[i] = LoadBuffer(buffer, i);
						}
					}
				}

				if (_gltfRoot.Images != null)
				{
					for(int i = 0; i < _gltfRoot.Images.Count; ++i)
					{
						if (_assetCache.ImageCache[i] == null)
						{
							_assetCache.ImageCache[i] = LoadImage(_gltfRoot.Images[i], i);
						}
					}
				}
#if !WINDOWS_UWP
				// generate these in advance instead of as-needed
				if (isMultithreaded)
				{
					yield return _asyncAction.RunOnWorkerThread(() => BuildAttributesForMeshes());
				}
#endif
			}

			var sceneObj = CreateScene(scene);

			if (SceneParent != null)
			{
				sceneObj.transform.SetParent(SceneParent, false);
			}

			_lastLoadedScene = sceneObj;
		}

		protected BufferCacheData LoadBuffer(GLTF.Schema.Buffer buffer, int index)
		{
			if (buffer.Uri == null)
			{
				return LoadBufferFromGLB(index);
			}
			else
			{
				return new BufferCacheData()
				{
					Stream = _loader.LoadBuffer(buffer)
				};
			}
		}

		protected Texture2D LoadImage(GLTF.Schema.Image image, int index)
		{
			if (image.Uri == null)
			{
				return LoadImageFromGLB(image, index);
			}
			else
			{
				return _loader.LoadImage(image);
			}
		}

		protected virtual void BuildAttributesForMeshes()
		{
			for (int i = 0; i < _gltfRoot.Meshes.Count; ++i)
			{
				GLTF.Schema.Mesh mesh = _gltfRoot.Meshes[i];
				if (_assetCache.MeshCache[i] == null)
				{
					_assetCache.MeshCache[i] = new MeshCacheData[mesh.Primitives.Count];
				}

				for(int j = 0; j < mesh.Primitives.Count; ++j)
				{
					_assetCache.MeshCache[i][j] = new MeshCacheData();
					var primitive = mesh.Primitives[j];
					BuildMeshAttributes(primitive, i, j);
				}
			}
		}

		protected virtual void BuildMeshAttributes(MeshPrimitive primitive, int meshID, int primitiveIndex)
		{
			if (_assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes.Count == 0)
			{
				Dictionary<string, AttributeAccessor> attributeAccessors = new Dictionary<string, AttributeAccessor>(primitive.Attributes.Count + 1);
				foreach (var attributePair in primitive.Attributes)
				{
					BufferId bufferIdPair = attributePair.Value.Value.BufferView.Value.Buffer;
					GLTF.Schema.Buffer buffer = bufferIdPair.Value;
					int bufferId = bufferIdPair.Id;
					
					// on cache miss, load the buffer
					if (_assetCache.BufferCache[bufferId] == null)
					{
						_assetCache.BufferCache[bufferId] = LoadBuffer(buffer, bufferId);
					}

					AttributeAccessor AttributeAccessor = new AttributeAccessor()
					{
						AccessorId = attributePair.Value,
						Stream = _assetCache.BufferCache[bufferId].Stream,
						Offset = _assetCache.BufferCache[bufferId].ChunkOffset
					};

					attributeAccessors[attributePair.Key] = AttributeAccessor;
				}

				if (primitive.Indices != null)
				{
					int bufferId = primitive.Indices.Value.BufferView.Value.Buffer.Id;
					AttributeAccessor indexBuilder = new AttributeAccessor()
					{
						AccessorId = primitive.Indices,
						Stream = _assetCache.BufferCache[bufferId].Stream,
						Offset = _assetCache.BufferCache[bufferId].ChunkOffset
					};

					attributeAccessors[SemanticProperties.INDICES] = indexBuilder;
				}

				GLTFHelpers.BuildMeshAttributes(ref attributeAccessors);
				_assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes = attributeAccessors;
			}
		}

		protected virtual GameObject CreateScene(Scene scene)
		{
			var sceneObj = new GameObject(scene.Name ?? "GLTFScene");

			foreach (var node in scene.Nodes)
			{
				var nodeObj = CreateNode(node.Value);
				nodeObj.transform.SetParent(sceneObj.transform, false);
			}

			return sceneObj;
		}

		protected virtual GameObject CreateNode(Node node)
		{
			var nodeObj = new GameObject(node.Name ?? "GLTFNode");

			Vector3 position;
			Quaternion rotation;
			Vector3 scale;
			node.GetUnityTRSProperties(out position, out rotation, out scale);
			nodeObj.transform.localPosition = position;
			nodeObj.transform.localRotation = rotation;
			nodeObj.transform.localScale = scale;

			// TODO: Add support for skin/morph targets
			if (node.Mesh != null)
			{
				CreateMeshObject(node.Mesh.Value, nodeObj.transform, node.Mesh.Id);
			}

			/* TODO: implement camera (probably a flag to disable for VR as well)
			if (camera != null)
			{
				GameObject cameraObj = camera.Value.Create();
				cameraObj.transform.parent = nodeObj.transform;
			}
			*/

			if (node.Children != null)
			{
				foreach (var child in node.Children)
				{
					// todo blgross: replace with an iterartive solution
					var childObj = CreateNode(child.Value);
					childObj.transform.SetParent(nodeObj.transform, false);
				}
			}

			return nodeObj;
		}

		protected virtual void CreateMeshObject(GLTF.Schema.Mesh mesh, Transform parent, int meshId)
		{
			if(_assetCache.MeshCache[meshId] == null)
			{
				_assetCache.MeshCache[meshId] = new MeshCacheData[mesh.Primitives.Count];
			}

			for(int i = 0; i < mesh.Primitives.Count; ++i)
			{
				var primitive = mesh.Primitives[i];
				var primitiveObj = CreateMeshPrimitive(primitive, meshId, i);
				primitiveObj.transform.SetParent(parent, false);
				primitiveObj.SetActive(true);
			}
		}

		protected virtual GameObject CreateMeshPrimitive(MeshPrimitive primitive, int meshID, int primitiveIndex)
		{
			var primitiveObj = new GameObject("Primitive");
			var meshFilter = primitiveObj.AddComponent<MeshFilter>();
			
			if (_assetCache.MeshCache[meshID][primitiveIndex] == null)
			{
				_assetCache.MeshCache[meshID][primitiveIndex] = new MeshCacheData();
			}
			if (_assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh == null)
			{
				if (_assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes.Count == 0)
				{
					BuildMeshAttributes(primitive, meshID, primitiveIndex);
				}
				var meshAttributes = _assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes;
				var vertexCount = primitive.Attributes[SemanticProperties.POSITION].Value.Count;

				// todo optimize: There are multiple copies being performed to turn the buffer data into mesh data. Look into reducing them
				UnityEngine.Mesh mesh = new UnityEngine.Mesh
				{
					vertices = primitive.Attributes.ContainsKey(SemanticProperties.POSITION)
						? meshAttributes[SemanticProperties.POSITION].AccessorContent.AsVertices.ToUnityVector3()
						: null,
					normals = primitive.Attributes.ContainsKey(SemanticProperties.NORMAL)
						? meshAttributes[SemanticProperties.NORMAL].AccessorContent.AsNormals.ToUnityVector3()
						: null,

					uv = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(0))
						? meshAttributes[SemanticProperties.TexCoord(0)].AccessorContent.AsTexcoords.ToUnityVector2()
						: null,

					uv2 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(1))
						? meshAttributes[SemanticProperties.TexCoord(1)].AccessorContent.AsTexcoords.ToUnityVector2()
						: null,

					uv3 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(2))
						? meshAttributes[SemanticProperties.TexCoord(2)].AccessorContent.AsTexcoords.ToUnityVector2()
						: null,

					uv4 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(3))
						? meshAttributes[SemanticProperties.TexCoord(3)].AccessorContent.AsTexcoords.ToUnityVector2()
						: null,

					colors = primitive.Attributes.ContainsKey(SemanticProperties.Color(0))
						? meshAttributes[SemanticProperties.Color(0)].AccessorContent.AsColors.ToUnityColor()
						: null,

					triangles = primitive.Indices != null
						? meshAttributes[SemanticProperties.INDICES].AccessorContent.AsUInts.ToIntArray()
						: MeshPrimitive.GenerateTriangles(vertexCount),

					tangents = primitive.Attributes.ContainsKey(SemanticProperties.TANGENT)
						? meshAttributes[SemanticProperties.TANGENT].AccessorContent.AsTangents.ToUnityVector4()
						: null
				};

				_assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh = mesh;
			}

			meshFilter.sharedMesh = _assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh;
			var meshRenderer = primitiveObj.AddComponent<MeshRenderer>();

			UnityEngine.Material materialToSet = null;
			bool shouldUseDefaultMaterial = primitive.Material == null;
			GLTF.Schema.Material materialToLoad = shouldUseDefaultMaterial ? DefaultMaterial : primitive.Material.Value;
			int materialIndex = primitive.Material != null ? primitive.Material.Id : -1;
			var material = CreateMaterial(materialToLoad);
			MaterialCacheData materialWrapper = new MaterialCacheData
			{
				UnityMaterial = material,
				UnityMaterialWithVertexColor = new UnityEngine.Material(material),
				GLTFMaterial = materialToLoad
			};
			materialWrapper.UnityMaterialWithVertexColor.EnableKeyword("VERTEX_COLOR_ON");
			materialToSet = materialWrapper.GetContents(primitive.Attributes.ContainsKey(SemanticProperties.Color(0)));

			if (!shouldUseDefaultMaterial)
			{
				_assetCache.MaterialCache[materialIndex] = materialWrapper;
			}
			meshRenderer.material = materialToSet;

			return primitiveObj;
		}

		protected virtual UnityEngine.Material CreateMaterial(GLTF.Schema.Material def)
		{
			Shader shader;

			// get the shader to use for this material
			try
			{
				if (def.PbrMetallicRoughness != null)
					shader = _shaderCache[MaterialType.PbrMetallicRoughness];
				else if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains("KHR_materials_common")
							 && def.CommonConstant != null)
					shader = _shaderCache[MaterialType.CommonConstant];
				else
					shader = _shaderCache[MaterialType.PbrMetallicRoughness];
			}
			catch (KeyNotFoundException)
			{
				Debug.LogWarningFormat("No shader supplied for type of glTF material {0}, using Standard fallback", def.Name);
				shader = Shader.Find("Standard");
			}

			shader.maximumLOD = MaximumLod;

			var material = new UnityEngine.Material(shader);

			if (def.AlphaMode == AlphaMode.MASK)
			{
				material.SetOverrideTag("RenderType", "TransparentCutout");
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.EnableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
					material.SetFloat("_Cutoff", (float)def.AlphaCutoff);
			}
			else if (def.AlphaMode == AlphaMode.BLEND)
			{
				material.SetOverrideTag("RenderType", "Transparent");
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
			}
			else
			{
				material.SetOverrideTag("RenderType", "Opaque");
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = -1;
			}

			if (def.DoubleSided)
			{
					material.SetInt("_Cull", (int)CullMode.Off);
			}
			else
			{
					material.SetInt("_Cull", (int)CullMode.Back);
			}

			if (def.PbrMetallicRoughness != null)
			{
				var pbr = def.PbrMetallicRoughness;

				material.SetColor("_Color", pbr.BaseColorFactor.ToUnityColor());

				if (pbr.BaseColorTexture != null)
				{
					var texture = pbr.BaseColorTexture.Index.Value;
					material.SetTexture("_MainTex", CreateTexture(texture));
				}

					material.SetFloat("_Metallic", (float)pbr.MetallicFactor);

				if (pbr.MetallicRoughnessTexture != null)
				{
					var texture = pbr.MetallicRoughnessTexture.Index.Value;
					material.SetTexture("_MetallicRoughnessMap", CreateTexture(texture));
				}

					material.SetFloat("_Roughness", (float)pbr.RoughnessFactor);
			}

			if (def.CommonConstant != null)
			{
				material.SetColor("_AmbientFactor", def.CommonConstant.AmbientFactor.ToUnityColor());

				if (def.CommonConstant.LightmapTexture != null)
				{
					material.EnableKeyword("LIGHTMAP_ON");

					var texture = def.CommonConstant.LightmapTexture.Index.Value;
					material.SetTexture("_LightMap", CreateTexture(texture));
					material.SetInt("_LightUV", def.CommonConstant.LightmapTexture.TexCoord);
				}

				material.SetColor("_LightFactor", def.CommonConstant.LightmapFactor.ToUnityColor());
			}

			if (def.NormalTexture != null)
			{
				var texture = def.NormalTexture.Index.Value;
				material.SetTexture("_BumpMap", CreateTexture(texture));
					material.SetFloat("_BumpScale", (float)def.NormalTexture.Scale);
			}

			if (def.OcclusionTexture != null)
			{
				var texture = def.OcclusionTexture.Index;

					material.SetFloat("_OcclusionStrength", (float)def.OcclusionTexture.Strength);

				if (def.PbrMetallicRoughness != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id == texture.Id)
				{
					material.EnableKeyword("OCC_METAL_ROUGH_ON");
				}
				else
				{
					material.SetTexture("_OcclusionMap", CreateTexture(texture.Value));
				}
			}

			if (def.EmissiveTexture != null)
			{
				var texture = def.EmissiveTexture.Index.Value;
				material.EnableKeyword("EMISSION_MAP_ON");
				material.SetTexture("_EmissionMap", CreateTexture(texture));
				material.SetInt("_EmissionUV", def.EmissiveTexture.TexCoord);
			}

			material.SetColor("_EmissionColor", def.EmissiveFactor.ToUnityColor());

			return material;
		}

		protected virtual UnityEngine.Texture CreateTexture(GLTF.Schema.Texture texture)
		{
			if (_assetCache.TextureCache[texture.Source.Id] == null)
			{
				if(_assetCache.ImageCache[texture.Source.Id] == null)
				{
					_assetCache.ImageCache[texture.Source.Id] = LoadImage(_gltfRoot.Images[texture.Source.Id], texture.Source.Id);
				}

				var source = _assetCache.ImageCache[texture.Source.Id];
				var desiredFilterMode = FilterMode.Bilinear;
				var desiredWrapMode = TextureWrapMode.Repeat;

				if (texture.Sampler != null)
				{
					var sampler = texture.Sampler.Value;
					switch (sampler.MinFilter)
					{
						case MinFilterMode.Nearest:
							desiredFilterMode = FilterMode.Point;
							break;
						case MinFilterMode.Linear:
						default:
							desiredFilterMode = FilterMode.Bilinear;
							break;
					}

					switch (sampler.WrapS)
					{
						case GLTF.Schema.WrapMode.ClampToEdge:
							desiredWrapMode = UnityEngine.TextureWrapMode.Clamp;
							break;
						case GLTF.Schema.WrapMode.Repeat:
						default:
							desiredWrapMode = UnityEngine.TextureWrapMode.Repeat;
							break;
					}
				}

				if (source.filterMode == desiredFilterMode && source.wrapMode == desiredWrapMode)
				{
					_assetCache.TextureCache[texture.Source.Id] = source;
				}
				else
				{
					var unityTexture = UnityEngine.Object.Instantiate(source);
					unityTexture.filterMode = desiredFilterMode;
					unityTexture.wrapMode = desiredWrapMode;
					_assetCache.TextureCache[texture.Source.Id] = unityTexture;
				}
			}

			return _assetCache.TextureCache[texture.Source.Id];
		}
		
		protected virtual Texture2D LoadImageFromGLB(Image image, int imageID)
		{
			var texture = new Texture2D(0, 0);
			var bufferView = image.BufferView.Value;
			var buffer = bufferView.Buffer.Value;
			var data = new byte[bufferView.ByteLength];

			var bufferContents = _assetCache.BufferCache[bufferView.Buffer.Id];
			bufferContents.Stream.Position = bufferView.ByteOffset + bufferContents.ChunkOffset;
			bufferContents.Stream.Read(data, 0, data.Length);
			texture.LoadImage(data);

			return texture;
		}

		protected virtual BufferCacheData LoadBufferFromGLB(int bufferIndex)
		{
			GLTFParser.SeekToBinaryChunk(_gltfStream, bufferIndex);  // sets stream to correct start position
			return new BufferCacheData
			{
				Stream = _gltfStream,
				ChunkOffset = _gltfStream.Position
			};
		}

		/// <summary>
		///  Get the absolute path to a gltf uri reference.
		/// </summary>
		/// <param name="gltfPath">The path to the gltf file</param>
		/// <returns>A path without the filename or extension</returns>
		protected static string AbsoluteUriPath(string gltfPath)
		{
			var uri = new Uri(gltfPath);
			var partialPath = uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments[uri.Segments.Length - 1].Length);
			return partialPath;
		}

		/// <summary>
		/// Get the absolute path a gltf file directory
		/// </summary>
		/// <param name="gltfPath">The path to the gltf file</param>
		/// <returns>A path without the filename or extension</returns>
		protected static string AbsoluteFilePath(string gltfPath)
		{
			var fileName = Path.GetFileName(gltfPath);
			var lastIndex = gltfPath.IndexOf(fileName);
			var partialPath = gltfPath.Substring(0, lastIndex);
			return partialPath;
		}
	}
}
