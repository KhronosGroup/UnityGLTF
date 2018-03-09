using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GLTF;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;

namespace UnityGLTF
{
	public class GLTFSceneImporter : IDisposable
	{
		private enum LoadType
		{
			Uri,
			Stream
		}

		public enum ColliderType
		{
			None,
			Box,
			Mesh
		}

		protected struct GLBStream
		{
			public Stream Stream;
			public long StartPosition;
		}

		protected GameObject _lastLoadedScene;
		protected readonly Transform _sceneParent;
		public int MaximumLod = 300;
		protected readonly GLTF.Schema.Material DefaultMaterial = new GLTF.Schema.Material();
		protected string _gltfUrl;
		protected string _gltfDirectoryPath;
		protected GLBStream _gltfStream;
		protected GLTFRoot _root;
		protected AssetCache _assetCache;
		protected AsyncAction _asyncAction;
		protected ColliderType _defaultColliderType = ColliderType.None;
		private LoadType _loadType;

		/// <summary>
		/// Creates a GLTFSceneBuilder object which will be able to construct a scene based off a url
		/// </summary>
		/// <param name="gltfUrl">URL to load</param>
		/// <param name="parent"></param>
		/// <param name="addColliders">Option to add mesh colliders to primitives</param>
		public GLTFSceneImporter(string gltfUrl, Transform parent = null, ColliderType DefaultCollider = ColliderType.None)
		{
			_gltfUrl = gltfUrl;
			_gltfDirectoryPath = AbsoluteUriPath(gltfUrl);
			_sceneParent = parent;
			_asyncAction = new AsyncAction();
			_loadType = LoadType.Uri;
			_defaultColliderType = DefaultCollider;
		}

		public GLTFSceneImporter(string rootPath, Stream stream, Transform parent = null, ColliderType DefaultCollider = ColliderType.None)
		{
			_gltfUrl = rootPath;
			_gltfDirectoryPath = AbsoluteFilePath(rootPath);
			_gltfStream = new GLBStream {Stream = stream, StartPosition = stream.Position};
			_sceneParent = parent;
			_asyncAction = new AsyncAction();
			_loadType = LoadType.Stream;
			_defaultColliderType = DefaultCollider;
		}

		public GameObject LastLoadedScene
		{
			get { return _lastLoadedScene; }
		}

		/// <summary>
		/// Loads via a web call the gltf file and then constructs a scene
		/// </summary>
		/// <param name="sceneIndex">Index into scene to load. -1 means load default</param>
		/// <param name="isMultithreaded">Whether to do loading operation on a thread</param>
		/// <returns></returns>
		public IEnumerator Load(int sceneIndex = -1, bool isMultithreaded = false)
		{
			if (_loadType == LoadType.Uri)
			{
				using (var www = UnityWebRequest.Get(_gltfUrl))
				{
					yield return www.Send();

					if (www.responseCode >= 400 || www.responseCode == 0)
					{
						throw new WebRequestException(www);
					}
					// Property new accessor creates buffer every time
					var gltfData = www.downloadHandler.data;
					using (var byteStream = new MemoryStream(gltfData, 0, gltfData.Length, false, true))
					{
						_gltfStream.Stream = byteStream;
						_root = GLTFParser.ParseJson(_gltfStream.Stream, _gltfStream.StartPosition);
						yield return ImportScene(sceneIndex, isMultithreaded);
					}
				}
			}
			else if (_loadType == LoadType.Stream)
			{
				_root = GLTFParser.ParseJson(_gltfStream.Stream, _gltfStream.StartPosition);
				yield return ImportScene(sceneIndex, isMultithreaded);
			}
			else
			{
				throw new Exception("Invalid load type specified: " + _loadType);
			}
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
			if (sceneIndex >= 0 && sceneIndex < _root.Scenes.Count)
			{
				scene = _root.Scenes[sceneIndex];
			}
			else
			{
				scene = _root.GetDefaultScene();
			}

			if (scene == null)
			{
				throw new Exception("No default scene in gltf file.");
			}

			_assetCache = new AssetCache(
				_root.Images != null ? _root.Images.Count : 0,
				_root.Textures != null ? _root.Textures.Count : 0,
				_root.Materials != null ? _root.Materials.Count : 0,
				_root.Buffers != null ? _root.Buffers.Count : 0,
				_root.Meshes != null ? _root.Meshes.Count : 0
			);

			if (_lastLoadedScene == null)
			{
				if (_root.Buffers != null)
				{
					// todo add fuzzing to verify that buffers are before uri
					for (int i = 0; i < _root.Buffers.Count; ++i)
					{
						GLTF.Schema.Buffer buffer = _root.Buffers[i];
						if (buffer.Uri != null)
						{
							yield return LoadBuffer(_gltfDirectoryPath, buffer, i);
						}
						else //null buffer uri indicates GLB buffer loading
						{
							GLTFParser.SeekToBinaryChunk(_gltfStream.Stream, i, _gltfStream.StartPosition);
							_assetCache.BufferCache[i] = new BufferCacheData()
							{
								ChunkOffset = _gltfStream.Stream.Position,
								Stream = _gltfStream.Stream
							};
						}
					}
				}

				if (_root.Images != null)
				{
					for (int i = 0; i < _root.Images.Count; ++i)
					{
						Image image = _root.Images[i];
						yield return LoadImage(_gltfDirectoryPath, image, i);
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

			if (_sceneParent != null)
			{
				sceneObj.transform.SetParent(_sceneParent, false);
			}

			_lastLoadedScene = sceneObj;
		}

		protected virtual void BuildAttributesForMeshes()
		{
			for (int i = 0; i < _root.Meshes.Count; ++i)
			{
				GLTF.Schema.Mesh mesh = _root.Meshes[i];
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
					BufferCacheData bufferCacheData = _assetCache.BufferCache[attributePair.Value.Value.BufferView.Value.Buffer.Id];
					AttributeAccessor AttributeAccessor = new AttributeAccessor()
					{
						AccessorId = attributePair.Value,
						Stream = bufferCacheData.Stream,
						Offset = bufferCacheData.ChunkOffset
					};

					attributeAccessors[attributePair.Key] = AttributeAccessor;
				}

				if (primitive.Indices != null)
				{
					BufferCacheData bufferCacheData = _assetCache.BufferCache[primitive.Indices.Value.BufferView.Value.Buffer.Id];
					AttributeAccessor indexBuilder = new AttributeAccessor()
					{
						AccessorId = primitive.Indices,
						Stream = bufferCacheData.Stream,
						Offset = bufferCacheData.ChunkOffset
					};

					attributeAccessors[SemanticProperties.INDICES] = indexBuilder;
				}

				GLTFHelpers.BuildMeshAttributes(ref attributeAccessors);
				TransformAttributes(ref attributeAccessors);
				_assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes = attributeAccessors;
			}
		}

		protected void TransformAttributes(ref Dictionary<string, AttributeAccessor> attributeAccessors)
		{
			// Flip vectors and triangles to the Unity coordinate system.
			if (attributeAccessors.ContainsKey(SemanticProperties.POSITION))
			{
				AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.POSITION];
				SchemaExtensions.ConvertVector3CoordinateSpace(ref attributeAccessor, SchemaExtensions.CoordinateSpaceConversionScale);
			}
			if (attributeAccessors.ContainsKey(SemanticProperties.INDICES))
			{
				AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.INDICES];
				SchemaExtensions.FlipFaces(ref attributeAccessor);
			}
			if (attributeAccessors.ContainsKey(SemanticProperties.NORMAL))
			{
				AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.NORMAL];
				SchemaExtensions.ConvertVector3CoordinateSpace(ref attributeAccessor, SchemaExtensions.CoordinateSpaceConversionScale);
			}
			// TexCoord goes from 0 to 3 to match GLTFHelpers.BuildMeshAttributes
			for (int i = 0; i < 4; i++)
			{
				if (attributeAccessors.ContainsKey(SemanticProperties.TexCoord(i)))
				{
					AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.TexCoord(i)];
					SchemaExtensions.FlipTexCoordArrayV(ref attributeAccessor);
				}
			}
			if (attributeAccessors.ContainsKey(SemanticProperties.TANGENT))
			{
				AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.TANGENT];
				SchemaExtensions.ConvertVector4CoordinateSpace(ref attributeAccessor, SchemaExtensions.TangentSpaceConversionScale);
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
						? meshAttributes[SemanticProperties.POSITION].AccessorContent.AsVertices.ToUnityVector3Raw()
						: null,
					normals = primitive.Attributes.ContainsKey(SemanticProperties.NORMAL)
						? meshAttributes[SemanticProperties.NORMAL].AccessorContent.AsNormals.ToUnityVector3Raw()
						: null,

					uv = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(0))
						? meshAttributes[SemanticProperties.TexCoord(0)].AccessorContent.AsTexcoords.ToUnityVector2Raw()
						: null,

					uv2 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(1))
						? meshAttributes[SemanticProperties.TexCoord(1)].AccessorContent.AsTexcoords.ToUnityVector2Raw()
						: null,

					uv3 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(2))
						? meshAttributes[SemanticProperties.TexCoord(2)].AccessorContent.AsTexcoords.ToUnityVector2Raw()
						: null,

					uv4 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(3))
						? meshAttributes[SemanticProperties.TexCoord(3)].AccessorContent.AsTexcoords.ToUnityVector2Raw()
						: null,

					colors = primitive.Attributes.ContainsKey(SemanticProperties.Color(0))
						? meshAttributes[SemanticProperties.Color(0)].AccessorContent.AsColors.ToUnityColorRaw()
						: null,

					triangles = primitive.Indices != null
						? meshAttributes[SemanticProperties.INDICES].AccessorContent.AsTriangles.ToIntArrayRaw()
						: MeshPrimitive.GenerateTriangles(vertexCount),

					tangents = primitive.Attributes.ContainsKey(SemanticProperties.TANGENT)
						? meshAttributes[SemanticProperties.TANGENT].AccessorContent.AsTangents.ToUnityVector4Raw()
						: null
				};

				_assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh = mesh;
			}

			meshFilter.sharedMesh = _assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh;

			var materialWrapper = CreateMaterial(
				primitive.Material != null ? primitive.Material.Value : DefaultMaterial,
				primitive.Material != null ? primitive.Material.Id : -1
			);

			var meshRenderer = primitiveObj.AddComponent<MeshRenderer>();
			meshRenderer.material = materialWrapper.GetContents(primitive.Attributes.ContainsKey(SemanticProperties.Color(0)));

			if (_defaultColliderType == ColliderType.Box)
			{
				var boxCollider = primitiveObj.AddComponent<BoxCollider>();
				boxCollider.center = meshFilter.sharedMesh.bounds.center;
				boxCollider.size = meshFilter.sharedMesh.bounds.size;
			}
			else if (_defaultColliderType == ColliderType.Mesh)
			{
				var meshCollider = primitiveObj.AddComponent<MeshCollider>();
				meshCollider.sharedMesh = meshFilter.sharedMesh;
				meshCollider.convex = true;
			}

			return primitiveObj;
		}

		protected virtual MaterialCacheData CreateMaterial(GLTF.Schema.Material def, int materialIndex)
		{
			MaterialCacheData materialWrapper = null;
			if (materialIndex < 0 || _assetCache.MaterialCache[materialIndex] == null)
			{
				IUniformMap mapper;
				const string specGlossExtName = KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME;
				if (_root.ExtensionsUsed != null && _root.ExtensionsUsed.Contains(specGlossExtName)
					&& def.Extensions != null && def.Extensions.ContainsKey(specGlossExtName))
					mapper = new SpecGlossMap(MaximumLod);
				else
					mapper = new MetalRoughMap(MaximumLod);

				mapper.AlphaMode = def.AlphaMode;
				mapper.DoubleSided = def.DoubleSided;

				var mrMapper = mapper as IMetalRoughUniformMap;
				if (def.PbrMetallicRoughness != null && mrMapper != null)
				{
					var pbr = def.PbrMetallicRoughness;

					mrMapper.BaseColorFactor = pbr.BaseColorFactor.ToUnityColorRaw();

					if (pbr.BaseColorTexture != null)
					{
						var textureDef = pbr.BaseColorTexture.Index.Value;
						mrMapper.BaseColorTexture = CreateTexture(textureDef);
						mrMapper.BaseColorTexCoord = pbr.BaseColorTexture.TexCoord;

						//ApplyTextureTransform(pbr.BaseColorTexture, material, "_MainTex");
					}

					mrMapper.MetallicFactor = pbr.MetallicFactor;

					if (pbr.MetallicRoughnessTexture != null)
					{
						var texture = pbr.MetallicRoughnessTexture.Index.Value;
						mrMapper.MetallicRoughnessTexture = CreateTexture(texture);
						mrMapper.MetallicRoughnessTexCoord = pbr.MetallicRoughnessTexture.TexCoord;

						//ApplyTextureTransform(pbr.MetallicRoughnessTexture, material, "_MetallicRoughnessMap");
					}

					mrMapper.RoughnessFactor = pbr.RoughnessFactor;
				}

				var sgMapper = mapper as ISpecGlossUniformMap;
				if (sgMapper != null)
				{
					var specGloss = def.Extensions[specGlossExtName] as KHR_materials_pbrSpecularGlossinessExtension;

					sgMapper.DiffuseFactor = specGloss.DiffuseFactor.ToUnityColorRaw();

					if (specGloss.DiffuseTexture != null)
					{
						var texture = specGloss.DiffuseTexture.Index.Value;
						sgMapper.DiffuseTexture = CreateTexture(texture);
						sgMapper.DiffuseTexCoord = specGloss.DiffuseTexture.TexCoord;

						//ApplyTextureTransform(specGloss.DiffuseTexture, material, "_MainTex");
					}

					sgMapper.SpecularFactor = specGloss.SpecularFactor.ToUnityVector3Raw();
					sgMapper.GlossinessFactor = specGloss.GlossinessFactor;

					if (specGloss.SpecularGlossinessTexture != null)
					{
						var texture = specGloss.SpecularGlossinessTexture.Index.Value;
						sgMapper.SpecularGlossinessTexture = CreateTexture(texture);

						//ApplyTextureTransform(specGloss.SpecularGlossinessTexture, material, "_SpecGlossMap");
					}
				}

				if (def.NormalTexture != null)
				{
					var texture = def.NormalTexture.Index.Value;
					mapper.NormalTexture = CreateTexture(texture);
					mapper.NormalTexCoord = def.NormalTexture.TexCoord;
					mapper.NormalTexScale = def.NormalTexture.Scale;

					//ApplyTextureTransform(def.NormalTexture, material, "_BumpMap");
				}

				if (def.OcclusionTexture != null)
				{
					mapper.OcclusionTexStrength = def.OcclusionTexture.Strength;
					var texture = def.OcclusionTexture.Index;
					mapper.OcclusionTexture = CreateTexture(texture.Value);

					//ApplyTextureTransform(def.OcclusionTexture, material, "_OcclusionMap");
				}

				if (def.EmissiveTexture != null)
				{
					var texture = def.EmissiveTexture.Index.Value;
					mapper.EmissiveTexture = CreateTexture(texture);
					mapper.EmissiveTexCoord = def.EmissiveTexture.TexCoord;

					//ApplyTextureTransform(def.EmissiveTexture, material, "_EmissionMap");
				}

				mapper.EmissiveFactor = def.EmissiveFactor.ToUnityColorRaw();

				var vertColorMapper = mapper.Clone();
				vertColorMapper.VertexColorsEnabled = true;

				materialWrapper = new MaterialCacheData
				{
					UnityMaterial = mapper.Material,
					UnityMaterialWithVertexColor = vertColorMapper.Material,
					GLTFMaterial = def
				};

				if (materialIndex > 0)
				{
					_assetCache.MaterialCache[materialIndex] = materialWrapper;
				}
			}

			return materialIndex > 0 ? _assetCache.MaterialCache[materialIndex] : materialWrapper;
		}

		protected virtual UnityEngine.Texture CreateTexture(GLTF.Schema.Texture texture)
		{
			if (_assetCache.TextureCache[texture.Source.Id] == null)
			{
				var source = _assetCache.ImageCache[texture.Source.Id];
				var desiredFilterMode = FilterMode.Bilinear;
				var desiredWrapMode = UnityEngine.TextureWrapMode.Repeat;

				if (texture.Sampler != null)
				{
					var sampler = texture.Sampler.Value;
					switch (sampler.MinFilter)
					{
						case MinFilterMode.Nearest:
						case MinFilterMode.NearestMipmapNearest:
						case MinFilterMode.NearestMipmapLinear:
							desiredFilterMode = FilterMode.Point;
							break;
						case MinFilterMode.Linear:
						case MinFilterMode.LinearMipmapNearest:
						case MinFilterMode.LinearMipmapLinear:
							desiredFilterMode = FilterMode.Bilinear;
							break;
						default:
							Debug.LogWarning($"Unsupported Sampler.MinFilter: {sampler.MinFilter}");
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

		protected virtual void ApplyTextureTransform(TextureInfo def, UnityEngine.Material mat, string texName)
		{
			IExtension extension;
			if (_root.ExtensionsUsed != null &&
				_root.ExtensionsUsed.Contains(ExtTextureTransformExtensionFactory.EXTENSION_NAME) &&
				def.Extensions != null &&
				def.Extensions.TryGetValue(ExtTextureTransformExtensionFactory.EXTENSION_NAME, out extension))
			{
				ExtTextureTransformExtension ext = (ExtTextureTransformExtension)extension;

				Vector2 temp = ext.Offset.ToUnityVector2Raw();
				temp = new Vector2(temp.x, -temp.y);
				mat.SetTextureOffset(texName, temp);

				mat.SetTextureScale(texName, ext.Scale.ToUnityVector2Raw());
			}
		}

		protected const string Base64StringInitializer = "^data:[a-z-]+/[a-z-]+;base64,";

		protected virtual IEnumerator LoadImage(string rootPath, Image image, int imageID)
		{
			if (_assetCache.ImageCache[imageID] == null)
			{
				Texture2D texture = null;
				if (image.Uri != null)
				{
					var uri = image.Uri;

					Regex regex = new Regex(Base64StringInitializer);
					Match match = regex.Match(uri);
					if (match.Success)
					{
						var base64Data = uri.Substring(match.Length);
						var textureData = Convert.FromBase64String(base64Data);
						texture = new Texture2D(0, 0);
						texture.LoadImage(textureData);
					}
					else if (_loadType == LoadType.Uri)
					{
						using (var www = UnityWebRequest.Get(Path.Combine(rootPath, uri)))
						{
							www.downloadHandler = new DownloadHandlerTexture();

							yield return www.Send();

							// HACK to enable mipmaps :(
							var tempTexture = DownloadHandlerTexture.GetContent(www);
							if (tempTexture != null)
							{
								texture = new Texture2D(tempTexture.width, tempTexture.height, tempTexture.format, true);
								texture.SetPixels(tempTexture.GetPixels());
								texture.Apply(true);
							}
							else
							{
								Debug.LogFormat("{0} {1}", www.responseCode, www.url);
								texture = new Texture2D(16, 16);
							}
						}
					}
					else if (_loadType == LoadType.Stream)
					{
						var pathToLoad = Path.Combine(rootPath, uri);
						var file = File.OpenRead(pathToLoad);
						byte[] bufferData = new byte[file.Length];
						file.Read(bufferData, 0, (int)file.Length);
#if !WINDOWS_UWP
						file.Close();
#else
                        file.Dispose();
#endif
						texture = new Texture2D(0, 0);
						texture.LoadImage(bufferData);
					}
				}
				else
				{
					texture = new Texture2D(0, 0);
					var bufferView = image.BufferView.Value;
					var data = new byte[bufferView.ByteLength];

					var bufferContents = _assetCache.BufferCache[bufferView.Buffer.Id];
					bufferContents.Stream.Position = bufferView.ByteOffset + bufferContents.ChunkOffset;
					bufferContents.Stream.Read(data, 0, data.Length);
					texture.LoadImage(data);
				}

				_assetCache.ImageCache[imageID] = texture;
			}
		}

		/// <summary>
		/// Load the remote URI data into a byte array.
		/// </summary>
		protected virtual IEnumerator LoadBuffer(string sourceUri, GLTF.Schema.Buffer buffer, int bufferIndex)
		{
			if (buffer.Uri != null)
			{
				Stream bufferStream = null;
				var uri = buffer.Uri;

				Regex regex = new Regex(Base64StringInitializer);
				Match match = regex.Match(uri);
				if (match.Success)
				{
					string base64String = uri.Substring(match.Length);
					byte[] base64ByteData = Convert.FromBase64String(base64String);
					bufferStream = new MemoryStream(base64ByteData, 0, base64ByteData.Length, false, true);
				}
				else if (_loadType == LoadType.Uri)
				{
					using (var www = UnityWebRequest.Get(Path.Combine(sourceUri, uri)))
					{
						yield return www.Send();

						bufferStream = new MemoryStream(www.downloadHandler.data, 0, www.downloadHandler.data.Length, false, true);
					}
				}
				else if (_loadType == LoadType.Stream)
				{
					var pathToLoad = Path.Combine(sourceUri, uri);
					bufferStream = File.OpenRead(pathToLoad);
				}

				_assetCache.BufferCache[bufferIndex] = new BufferCacheData()
				{
					Stream = bufferStream
				};
			}
		}

		/// <summary>
		///  Get the absolute path to a gltf uri reference.
		/// </summary>
		/// <param name="gltfPath">The path to the gltf file</param>
		/// <returns>A path without the filename or extension</returns>
		protected static string AbsoluteUriPath(string gltfPath)
		{
			var uri = new Uri(gltfPath);
			var partialPath = uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Query.Length - uri.Segments[uri.Segments.Length - 1].Length);
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

		public void Dispose()
		{
			if (_assetCache != null)
			{
				_assetCache.Dispose();
				_assetCache = null;
			}

		}
	}
}
