using GLTF;
using GLTF.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
#if WINDOWS_UWP
using System.Threading.Tasks;
#endif
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Loader;

namespace UnityGLTF
{
	public struct MeshConstructionData
	{
		public MeshPrimitive Primitive { get; set; }
		public Dictionary<string, AttributeAccessor> MeshAttributes { get; set; }
	}

	public class UnityObjectConstructionData
	{
		public UnityEngine.Mesh LoadedMesh { get; set; }
		public UnityEngine.Material Material { get; set; }
		public GLTF.Schema.Material MaterialSchema { get; set; }
		public MeshPrimitive Primitive { get; set; }
		public bool IsUsingDefaultMaterial { get; set; }
		public int MaterialIndex { get; set; }
	}

	public struct TransformAssignment
	{
		public GameObject Child { get; set; }
		public Transform Parent { get; set; }
	}

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

		private const string Base64StringInitializer = "^data:[a-z-]+/[a-z-]+;base64,";

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
		/// Loads a GLTF scene into unity
		/// </summary>
		/// <param name="sceneIndex">Index into scene to load. -1 means load default</param>
		/// <param name="isMultithreaded">Whether to do loading operation on a thread</param>
#if WINDOWS_UWP
		public async Task LoadScene(int sceneIndex = -1, bool isMultithreaded = false)
		{
			if (_gltfRoot == null)
			{
				await LoadJson(_gltfFileName);
			}
			await ImportScene(sceneIndex, isMultithreaded);
		}
#else
		public IEnumerator LoadScene(int sceneIndex = -1, bool isMultithreaded = false)
		{
			if(_gltfRoot == null)
			{
				LoadJson(_gltfFileName);
			}
			yield return ImportScene(sceneIndex, isMultithreaded);
		}
#endif

#if WINDOWS_UWP
		public async Task<GameObject> LoadNode(int nodeIndex)
#else
		public GameObject LoadNode(int nodeIndex)
#endif
		{
			if (_gltfRoot == null)
			{
				throw new InvalidOperationException("GLTF root must first be loaded and parsed");
			}

			if (_assetCache == null)
			{
				InitializeAssetCache();
			}

#if WINDOWS_UWP
			return await _LoadNode(nodeIndex);
#else
			return _LoadNode(nodeIndex);
#endif
		}

		/// <summary>
		/// Loads via a web call the gltf file
		/// </summary>
		/// <returns></returns>
#if WINDOWS_UWP
		private async Task LoadJson(string jsonFilePath)
#else
		public void LoadJson(string jsonFilePath)
#endif
		{
#if WINDOWS_UWP
			_gltfStream = await _loader.LoadStream(jsonFilePath);
#else
			_gltfStream = _loader.LoadStream(jsonFilePath);
#endif
			_gltfRoot = GLTFParser.ParseJson(_gltfStream);
		}

#if WINDOWS_UWP
		private async Task<GameObject> _LoadNode(int nodeIndex)
#else
		private GameObject _LoadNode(int nodeIndex)
#endif
		{
			if(nodeIndex >= _gltfRoot.Nodes.Count)
			{
				throw new ArgumentException("nodeIndex is out of range");
			}

#if WINDOWS_UWP
			return await CreateNode(_gltfRoot.Nodes[nodeIndex]);
#else
			return CreateNode(_gltfRoot.Nodes[nodeIndex]);
#endif
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
#if WINDOWS_UWP
		protected async Task ImportScene(int sceneIndex = -1, bool isMultithreaded = false)
#else
		protected IEnumerator ImportScene(int sceneIndex = -1, bool isMultithreaded = false)
#endif
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
#if WINDOWS_UWP
							_assetCache.BufferCache[i] = await LoadBuffer(buffer, i);
#else
							_assetCache.BufferCache[i] = LoadBuffer(buffer, i);
#endif
						}
					}
				}

				if (_gltfRoot.Images != null)
				{
					for(int i = 0; i < _gltfRoot.Images.Count; ++i)
					{
						if (_assetCache.ImageCache[i] == null)
						{
#if WINDOWS_UWP
							_assetCache.ImageCache[i] = await LoadImage(_gltfRoot.Images[i], i);
#else
							_assetCache.ImageCache[i] = LoadImage(_gltfRoot.Images[i], i);
#endif
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
#if WINDOWS_UWP
			var sceneObj = await CreateScene(scene);
#else
			var sceneObj = CreateScene(scene);
#endif

			if (SceneParent != null)
			{
				sceneObj.transform.SetParent(SceneParent, false);
			}

			_lastLoadedScene = sceneObj;
		}

#if WINDOWS_UWP
		protected async Task<BufferCacheData> LoadBuffer(GLTF.Schema.Buffer buffer, int index)
#else
		protected BufferCacheData LoadBuffer(GLTF.Schema.Buffer buffer, int index)
#endif
		{
			if (buffer.Uri == null)
			{
				return LoadBufferFromGLB(index);
			}
			else
			{
				Stream bufferDataStream = null;
				var uri = buffer.Uri;

				byte[] bufferData;
				TryParseBase64(uri, out bufferData);
				if(bufferData != null)
				{
					bufferDataStream = new MemoryStream(bufferData, 0, bufferData.Length, false, true);
				}
				else
				{
#if WINDOWS_UWP
					bufferDataStream = await _loader.LoadStream(buffer.Uri);
#else
					bufferDataStream = _loader.LoadStream(buffer.Uri);
#endif
				}
				return new BufferCacheData()
				{
					Stream = bufferDataStream
				};
			}
		}

#if WINDOWS_UWP
		protected async Task<Texture2D> LoadImage(GLTF.Schema.Image image, int index)
#else
		protected Texture2D LoadImage(GLTF.Schema.Image image, int index)
#endif
		{
			if (image.Uri == null)
			{
				return LoadImageFromGLB(image, index);
			}
			else
			{
				Texture2D texture = null;
				var uri = image.Uri;

				byte[] bufferData;
				TryParseBase64(uri, out bufferData);
				if(bufferData != null)
				{
					texture = new Texture2D(0, 0);
					texture.LoadImage(bufferData);
				}
				else
				{
#if WINDOWS_UWP
					Stream stream = await _loader.LoadStream(uri);
#else
					Stream stream = _loader.LoadStream(uri);
#endif
					texture = LoadTexture(stream);
				}
				
				return texture;
			}
		}

		/// <summary>
		/// Tries to parse the uri as a base 64 encoded string
		/// </summary>
		/// <param name="uri">The string that represents the data</param>
		/// <param name="bufferData">Returns the deencoded bytes</param>
		protected void TryParseBase64(string uri, out byte[] bufferData)
		{
			Regex regex = new Regex(Base64StringInitializer);
			Match match = regex.Match(uri);
			bufferData = null;
			if (match.Success)
			{
				var base64Data = uri.Substring(match.Length);
				bufferData = Convert.FromBase64String(base64Data);
			}
		}

		/// <summary>
		/// Loads texture from a stream. Is responsible for stream clean up
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		protected Texture2D LoadTexture(Stream stream)
		{
			Texture2D texture = new Texture2D(0, 0);

			if (stream is MemoryStream)
			{
				using (MemoryStream memoryStream = stream as MemoryStream)
				{
					texture.LoadImage(memoryStream.ToArray());
				}
			}
			else
			{
				byte[] buffer = new byte[stream.Length];
				using (stream)
				{
					if (stream.Length > int.MaxValue)
					{
						throw new Exception("Stream is larger than can be copied into byte array");
					}

					stream.Read(buffer, 0, (int)stream.Length);
				}

				texture.LoadImage(buffer);
			}

			texture.Apply();
			return texture;
		}

#if WINDOWS_UWP
		protected virtual async Task BuildAttributesForMeshes()
#else
		protected virtual void BuildAttributesForMeshes()
#endif
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
#if WINDOWS_UWP
					await BuildMeshAttributes(primitive, i, j);
#else
					BuildMeshAttributes(primitive, i, j);
#endif
				}
			}
		}

#if WINDOWS_UWP
		protected virtual async Task BuildMeshAttributes(MeshPrimitive primitive, int meshID, int primitiveIndex)
#else
		protected virtual void BuildMeshAttributes(MeshPrimitive primitive, int meshID, int primitiveIndex)
#endif
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
#if WINDOWS_UWP
						_assetCache.BufferCache[bufferId] = await LoadBuffer(buffer, bufferId);
#else
						_assetCache.BufferCache[bufferId] = LoadBuffer(buffer, bufferId);
#endif
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

#if WINDOWS_UWP
		protected virtual async Task<GameObject> CreateScene(Scene scene)
#else
		protected virtual GameObject CreateScene(Scene scene)
#endif
		{
			var sceneObj = new GameObject(scene.Name ?? "GLTFScene");

			foreach (var node in scene.Nodes)
			{
#if WINDOWS_UWP
				var nodeObj = await CreateNode(node.Value);
#else
				var nodeObj = CreateNode(node.Value);
#endif
				nodeObj.transform.SetParent(sceneObj.transform, false);
			}

			return sceneObj;
		}

#if WINDOWS_UWP
		protected virtual async Task<GameObject> CreateNode(Node node)
#else
		protected virtual GameObject CreateNode(Node node)
#endif
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
#if WINDOWS_UWP
				await CreateMeshObject(node.Mesh.Value, nodeObj.transform, node.Mesh.Id);
#else
				CreateMeshObject(node.Mesh.Value, nodeObj.transform, node.Mesh.Id);
#endif
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
#if WINDOWS_UWP
					var childObj = await CreateNode(child.Value);
#else
					// todo blgross: replace with an iterartive solution
					var childObj = CreateNode(child.Value);
#endif
					var transformAssignment = new TransformAssignment()
					{
						Child = childObj,
						Parent = nodeObj.transform
					};

					childObj.transform.SetParent(nodeObj.transform, false);
				}
			}

			return nodeObj;
		}

#if WINDOWS_UWP
		protected virtual async Task CreateMeshObject(GLTF.Schema.Mesh mesh, Transform parent, int meshId)
#else
		protected virtual void CreateMeshObject(GLTF.Schema.Mesh mesh, Transform parent, int meshId)
#endif
		{
			if(_assetCache.MeshCache[meshId] == null)
			{
				_assetCache.MeshCache[meshId] = new MeshCacheData[mesh.Primitives.Count];
			}

			for(int i = 0; i < mesh.Primitives.Count; ++i)
			{
				var primitive = mesh.Primitives[i];
#if WINDOWS_UWP
				var primitiveObj = await CreateMeshPrimitive(primitive, meshId, i);
#else
				var primitiveObj = CreateMeshPrimitive(primitive, meshId, i);
#endif

				var transformAssignment = new TransformAssignment()
				{
					Child = primitiveObj,
					Parent = parent
				};

				primitiveObj.transform.SetParent(parent, false);
				primitiveObj.SetActive(true);
			}
		}

#if WINDOWS_UWP
		protected virtual async Task<GameObject> CreateMeshPrimitive(MeshPrimitive primitive, int meshID, int primitiveIndex)
#else
		protected virtual GameObject CreateMeshPrimitive(MeshPrimitive primitive, int meshID, int primitiveIndex)
#endif
		{
			if (_assetCache.MeshCache[meshID][primitiveIndex] == null)
			{
				_assetCache.MeshCache[meshID][primitiveIndex] = new MeshCacheData();
			}
			if (_assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh == null)
			{
				if (_assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes.Count == 0)
				{
#if WINDOWS_UWP
					await BuildMeshAttributes(primitive, meshID, primitiveIndex);
#else
					BuildMeshAttributes(primitive, meshID, primitiveIndex);
#endif
				}
				var meshAttributes = _assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes;
				var meshConstructionData = new MeshConstructionData
				{
					Primitive = primitive,
					MeshAttributes = meshAttributes
				};
				
				UnityEngine.Mesh mesh = CreateUnityMesh(meshConstructionData);
				_assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh = mesh;
			}

			bool shouldUseDefaultMaterial = primitive.Material == null;

			GLTF.Schema.Material materialToLoad = shouldUseDefaultMaterial ? DefaultMaterial : primitive.Material.Value;
			int materialIndex = primitive.Material != null ? primitive.Material.Id : -1;

#if WINDOWS_UWP
			await LoadMaterialTextures(materialToLoad);
#else
			LoadMaterialTextures(materialToLoad);
#endif

			var material = CreateMaterial(materialToLoad);

			UnityEngine.Material materialToSet = null;
			var primitiveObj = new GameObject("Primitive");
			var meshFilter = primitiveObj.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = _assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh;
			var meshRenderer = primitiveObj.AddComponent<MeshRenderer>();
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

#if WINDOWS_UWP
		protected async Task LoadMaterialTextures(GLTF.Schema.Material def)
#else
		protected void LoadMaterialTextures(GLTF.Schema.Material def)
#endif
		{
			if (def.PbrMetallicRoughness != null)
			{
				var pbr = def.PbrMetallicRoughness;

				if (pbr.BaseColorTexture != null)
				{
					var texture = pbr.BaseColorTexture.Index.Value;
					var textureId = pbr.BaseColorTexture.Index.Id;
#if WINDOWS_UWP
					await CreateTexture(texture, textureId);
#else
					CreateTexture(texture, textureId);
#endif
				}
				if (pbr.MetallicRoughnessTexture != null)
				{
					var texture = pbr.MetallicRoughnessTexture.Index.Value;
					var textureId = pbr.MetallicRoughnessTexture.Index.Id;

#if WINDOWS_UWP
					await CreateTexture(texture, textureId);
#else
					CreateTexture(texture, textureId);
#endif
				}
			}

			if (def.CommonConstant != null)
			{
				if (def.CommonConstant.LightmapTexture != null)
				{
					var texture = def.CommonConstant.LightmapTexture.Index.Value;
					var textureId = def.CommonConstant.LightmapTexture.Index.Id;

#if WINDOWS_UWP
					await CreateTexture(texture, textureId);
#else
					CreateTexture(texture, textureId);
#endif
				}
				
			}

			if (def.NormalTexture != null)
			{
				var texture = def.NormalTexture.Index.Value;
				var textureId = def.NormalTexture.Index.Id;
#if WINDOWS_UWP
				await CreateTexture(texture, textureId);
#else
				CreateTexture(texture, textureId);
#endif
			}

			if (def.OcclusionTexture != null)
			{
				var texture = def.OcclusionTexture.Index.Value;
				var textureId = def.OcclusionTexture.Index.Id;

				if (!(def.PbrMetallicRoughness != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id == textureId))
				{
#if WINDOWS_UWP
					await CreateTexture(texture, textureId);
#else
					CreateTexture(texture, textureId);
#endif
				}
			}

			if (def.EmissiveTexture != null)
			{
				var texture = def.EmissiveTexture.Index.Value;
				var textureId = def.EmissiveTexture.Index.Id;
#if WINDOWS_UWP
				await CreateTexture(texture, textureId);
#else
				CreateTexture(texture, textureId);
#endif
			}
		}
		
		protected UnityEngine.Mesh CreateUnityMesh(MeshConstructionData meshConstructionData)
		{
			MeshPrimitive primitive = meshConstructionData.Primitive;
			var meshAttributes = meshConstructionData.MeshAttributes;
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

			return mesh;
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
					var textureId = pbr.BaseColorTexture.Index.Id;
					material.SetTexture("_MainTex", _assetCache.TextureCache[textureId]);
				}

					material.SetFloat("_Metallic", (float)pbr.MetallicFactor);

				if (pbr.MetallicRoughnessTexture != null)
				{
					var textureId = pbr.MetallicRoughnessTexture.Index.Id;
					material.SetTexture("_MetallicRoughnessMap", _assetCache.TextureCache[textureId]);
				}

				material.SetFloat("_Roughness", (float)pbr.RoughnessFactor);
			}

			if (def.CommonConstant != null)
			{
				material.SetColor("_AmbientFactor", def.CommonConstant.AmbientFactor.ToUnityColor());

				if (def.CommonConstant.LightmapTexture != null)
				{
					material.EnableKeyword("LIGHTMAP_ON");

					var textureId = def.CommonConstant.LightmapTexture.Index.Id;
					material.SetTexture("_LightMap", _assetCache.TextureCache[textureId]);
					material.SetInt("_LightUV", def.CommonConstant.LightmapTexture.TexCoord);
				}

				material.SetColor("_LightFactor", def.CommonConstant.LightmapFactor.ToUnityColor());
			}

			if (def.NormalTexture != null)
			{
				var textureId = def.NormalTexture.Index.Id;
				material.SetTexture("_BumpMap", _assetCache.TextureCache[textureId]);
				material.SetFloat("_BumpScale", (float)def.NormalTexture.Scale);
			}

			if (def.OcclusionTexture != null)
			{
				var textureId = def.OcclusionTexture.Index.Id;

					material.SetFloat("_OcclusionStrength", (float)def.OcclusionTexture.Strength);

				if (def.PbrMetallicRoughness != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id == textureId)
				{
					material.EnableKeyword("OCC_METAL_ROUGH_ON");
				}
				else
				{
					material.SetTexture("_OcclusionMap", _assetCache.TextureCache[textureId]);
				}
			}

			if (def.EmissiveTexture != null)
			{
				var textureId = def.EmissiveTexture.Index.Id;
				material.EnableKeyword("EMISSION_MAP_ON");
				material.SetTexture("_EmissionMap", _assetCache.TextureCache[textureId]);
				material.SetInt("_EmissionUV", def.EmissiveTexture.TexCoord);
			}

			material.SetColor("_EmissionColor", def.EmissiveFactor.ToUnityColor());

			return material;
		}

#if WINDOWS_UWP
		protected virtual async Task<UnityEngine.Texture> CreateTexture(GLTF.Schema.Texture texture, int textureIndex)
#else
		protected virtual UnityEngine.Texture CreateTexture(GLTF.Schema.Texture texture, int textureIndex)
#endif
		{
			if (_assetCache.TextureCache[texture.Source.Id] == null)
			{
				if(_assetCache.ImageCache[texture.Source.Id] == null)
				{
#if WINDOWS_UWP
					_assetCache.ImageCache[texture.Source.Id] = await LoadImage(_gltfRoot.Images[texture.Source.Id], texture.Source.Id);
#else
					_assetCache.ImageCache[texture.Source.Id] = LoadImage(_gltfRoot.Images[texture.Source.Id], texture.Source.Id);
#endif
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
					_assetCache.TextureCache[textureIndex] = source;
				}
				else
				{
					var unityTexture = UnityEngine.Object.Instantiate(source);
					unityTexture.filterMode = desiredFilterMode;
					unityTexture.wrapMode = desiredWrapMode;
					_assetCache.TextureCache[textureIndex] = unityTexture;
				}
			}

			return _assetCache.TextureCache[textureIndex];
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
