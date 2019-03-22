using GLTF;
using GLTF.Extensions;
using GLTF.Schema;
using GLTF.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
#if !WINDOWS_UWP
using System.Threading;
#endif
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Loader;
using Matrix4x4 = GLTF.Math.Matrix4x4;
using Object = UnityEngine.Object;
#if !WINDOWS_UWP
using ThreadPriority = System.Threading.ThreadPriority;
#endif
using WrapMode = UnityEngine.WrapMode;

namespace UnityGLTF
{
	public struct MeshConstructionData
	{
		public MeshPrimitive Primitive { get; set; }
		public Dictionary<string, AttributeAccessor> MeshAttributes { get; set; }
	}

	public class UnityMeshData
	{
		public Vector3[] Vertices;
		public Vector3[] Normals;
		public Vector2[] Uv1;
		public Vector2[] Uv2;
		public Vector2[] Uv3;
		public Vector2[] Uv4;
		public Color[] Colors;
		public int[] Triangles;
		public Vector4[] Tangents;
		public BoneWeight[] BoneWeights;
	}

	/// <summary>
	/// Converts gltf animation data to unity
	/// </summary>
	public delegate float[] ValuesConvertion(NumericArray data, int frame);

	public class GLTFSceneImporter : IDisposable
	{
		public enum ColliderType
		{
			None,
			Box,
			Mesh,
			MeshConvex
		}

		/// <summary>
		/// Maximum LOD
		/// </summary>
		public int MaximumLod = 300;

		/// <summary>
		/// Timeout for certain threading operations
		/// </summary>
		public int Timeout = 8;

		private bool _isMultithreaded;

		/// <summary>
		/// Use Multithreading or not.
		/// In editor, this is always false. This is to prevent a freeze in editor (noticed in Unity versions 2017.x and 2018.x)
		/// </summary>
		public bool IsMultithreaded
		{
			get
			{
				return Application.isEditor ? false : _isMultithreaded;
			}
			set
			{
				_isMultithreaded = value;
			}
		}

		/// <summary>
		/// The parent transform for the created GameObject
		/// </summary>
		public Transform SceneParent { get; set; }

		/// <summary>
		/// The last created object
		/// </summary>
		public GameObject CreatedObject { get; private set; }

		/// <summary>
		/// Adds colliders to primitive objects when created
		/// </summary>
		public ColliderType Collider { get; set; }

		/// <summary>
		/// Override for the shader to use on created materials
		/// </summary>
		public string CustomShaderName { get; set; }

		/// <summary>
		/// Whether to keep a CPU-side copy of the mesh after upload to GPU (for example, in case normals/tangents need recalculation)
		/// </summary>
		public bool KeepCPUCopyOfMesh = true;

		/// <summary>
		/// Whether to keep a CPU-side copy of the texture after upload to GPU
		/// </summary>
		/// <remaks>
		/// This is is necessary when a texture is used with different sampler states, as Unity doesn't allow setting
		/// of filter and wrap modes separately form the texture object. Setting this to false will omit making a copy
		/// of a texture in that case and use the original texture's sampler state wherever it's referenced; this is
		/// appropriate in cases such as the filter and wrap modes being specified in the shader instead
		/// </remaks>
		public bool KeepCPUCopyOfTexture = true;

		/// <summary>
		/// When screen coverage is above threashold and no LOD mesh cull the object
		/// </summary>
		public bool CullFarLOD = false;

		protected struct GLBStream
		{
			public Stream Stream;
			public long StartPosition;
		}

		protected IAsyncCoroutineHelper _asyncCoroutineHelper;

		protected GameObject _lastLoadedScene;
		protected readonly GLTFMaterial DefaultMaterial = new GLTFMaterial();
		protected MaterialCacheData _defaultLoadedMaterial = null;

		protected string _gltfFileName;
		protected GLBStream _gltfStream;
		protected GLTFRoot _gltfRoot;
		protected AssetCache _assetCache;
		protected ILoader _loader;
		protected bool _isRunning = false;


		/// <summary>
		/// Creates a GLTFSceneBuilder object which will be able to construct a scene based off a url
		/// </summary>
		/// <param name="gltfFileName">glTF file relative to data loader path</param>
		/// <param name="externalDataLoader">Loader to load external data references</param>
		/// <param name="asyncCoroutineHelper">Helper to load coroutines on a seperate thread</param>
		public GLTFSceneImporter(string gltfFileName, ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper) : this(externalDataLoader, asyncCoroutineHelper)
		{
			_gltfFileName = gltfFileName;
		}

		public GLTFSceneImporter(GLTFRoot rootNode, ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper, Stream gltfStream = null) : this(externalDataLoader, asyncCoroutineHelper)
		{
			_gltfRoot = rootNode;
			_loader = externalDataLoader;
			if (gltfStream != null)
			{
				_gltfStream = new GLBStream {Stream = gltfStream, StartPosition = gltfStream.Position};
			}
		}

		private GLTFSceneImporter(ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper)
		{
			_loader = externalDataLoader;
			_asyncCoroutineHelper = asyncCoroutineHelper;
		}

		public void Dispose()
		{
			if (_assetCache != null)
			{
				Cleanup();
			}
		}

		public GameObject LastLoadedScene
		{
			get { return _lastLoadedScene; }
		}

		/// <summary>
		/// Loads a glTF Scene into the LastLoadedScene field
		/// </summary>
		/// <param name="sceneIndex">The scene to load, If the index isn't specified, we use the default index in the file. Failing that we load index 0.</param>
		/// <param name="showSceneObj"></param>
		/// <param name="onLoadComplete">Callback function for when load is completed</param>
		/// <returns></returns>
		public async Task LoadSceneAsync(int sceneIndex = -1, bool showSceneObj = true, Action<GameObject, ExceptionDispatchInfo> onLoadComplete = null)
		{
			try
			{
				lock (this)
				{
					if (_isRunning)
					{
						throw new GLTFLoadException("Cannot call LoadScene while GLTFSceneImporter is already running");
					}

					_isRunning = true;
				}

				if (_gltfRoot == null)
				{
					await LoadJson(_gltfFileName);
				}

				if (_assetCache == null)
				{
					_assetCache = new AssetCache(_gltfRoot);
				}

				await _LoadScene(sceneIndex, showSceneObj);
			}
			catch (Exception ex)
			{
				onLoadComplete?.Invoke(null, ExceptionDispatchInfo.Capture(ex));
				throw;
			}
			finally
			{
				lock (this)
				{
					_isRunning = false;
				}
			}

			onLoadComplete?.Invoke(LastLoadedScene, null);
		}

		public IEnumerator LoadScene(int sceneIndex = -1, bool showSceneObj = true, Action<GameObject, ExceptionDispatchInfo> onLoadComplete = null)
		{
			return LoadSceneAsync(sceneIndex, showSceneObj, onLoadComplete).AsCoroutine();
		}

		/// <summary>
		/// Loads a node tree from a glTF file into the LastLoadedScene field
		/// </summary>
		/// <param name="nodeIndex">The node index to load from the glTF</param>
		/// <returns></returns>
		public async Task LoadNodeAsync(int nodeIndex)
		{
			try
			{
				lock (this)
				{
					if (_isRunning)
					{
						throw new GLTFLoadException("Cannot call LoadNode while GLTFSceneImporter is already running");
					}

					_isRunning = true;
				}

				if (_gltfRoot == null)
				{
					await LoadJson(_gltfFileName);
				}

				if (_assetCache == null)
				{
					_assetCache = new AssetCache(_gltfRoot);
				}

				await _LoadNode(nodeIndex);
				CreatedObject = _assetCache.NodeCache[nodeIndex];
				InitializeGltfTopLevelObject();
			}
			finally
			{
				lock (this)
				{
					_isRunning = false;
				}
			}
		}

		/// <summary>
		/// Load a Material from the glTF by index
		/// </summary>
		/// <param name="materialIndex"></param>
		/// <returns></returns>
		public virtual async Task<Material> LoadMaterialAsync(int materialIndex)
		{
			try
			{
				lock (this)
				{
					if (_isRunning)
					{
						throw new GLTFLoadException("Cannot CreateTexture while GLTFSceneImporter is already running");
					}

					_isRunning = true;
				}

				if (_gltfRoot == null)
				{
					await LoadJson(_gltfFileName);
				}

				if (materialIndex < 0 || materialIndex >= _gltfRoot.Materials.Count)
				{
					throw new ArgumentException($"There is no material for index {materialIndex}");
				}

				if (_assetCache == null)
				{
					_assetCache = new AssetCache(_gltfRoot);
				}

				if (_assetCache.MaterialCache[materialIndex] == null)
				{
					var def = _gltfRoot.Materials[materialIndex];
					await ConstructMaterialImageBuffers(def);
					await ConstructMaterial(def, materialIndex);
				}
			}
			finally
			{
				lock (this)
				{
					_isRunning = false;
				}
			}

			return _assetCache.MaterialCache[materialIndex].UnityMaterialWithVertexColor;
		}

		/// <summary>
		/// Initializes the top-level created node by adding an instantiated GLTF object component to it,
		/// so that it can cleanup after itself properly when destroyed
		/// </summary>
		private void InitializeGltfTopLevelObject()
		{
			InstantiatedGLTFObject instantiatedGltfObject = CreatedObject.AddComponent<InstantiatedGLTFObject>();
			instantiatedGltfObject.CachedData = new RefCountedCacheData
			{
				MaterialCache = _assetCache.MaterialCache,
				MeshCache = _assetCache.MeshCache,
				TextureCache = _assetCache.TextureCache
			};
		}

		private async Task ConstructBufferData(Node node)
		{
			MeshId mesh = node.Mesh;
			if (mesh != null)
			{
				if (mesh.Value.Primitives != null)
				{
					await ConstructMeshAttributes(mesh.Value, mesh);
				}
			}

			if (node.Children != null)
			{
				foreach (NodeId child in node.Children)
				{
					await ConstructBufferData(child.Value);
				}
			}

			const string msft_LODExtName = MSFT_LODExtensionFactory.EXTENSION_NAME;
			MSFT_LODExtension lodsextension = null;
			if (_gltfRoot.ExtensionsUsed != null
				&& _gltfRoot.ExtensionsUsed.Contains(msft_LODExtName)
				&& node.Extensions != null
				&& node.Extensions.ContainsKey(msft_LODExtName))
			{
				lodsextension = node.Extensions[msft_LODExtName] as MSFT_LODExtension;
				if (lodsextension != null && lodsextension.MeshIds.Count > 0)
				{
					for (int i = 0; i < lodsextension.MeshIds.Count; i++)
					{
						int lodNodeId = lodsextension.MeshIds[i];
						await ConstructBufferData(_gltfRoot.Nodes[lodNodeId]);
					}
				}
			}
		}

		private async Task ConstructMeshAttributes(GLTFMesh mesh, MeshId meshId)
		{
			int meshIdIndex = meshId.Id;

			if (_assetCache.MeshCache[meshIdIndex] == null)
			{
				_assetCache.MeshCache[meshIdIndex] = new MeshCacheData[mesh.Primitives.Count];
			}

			for (int i = 0; i < mesh.Primitives.Count; ++i)
			{
				MeshPrimitive primitive = mesh.Primitives[i];
				if (_assetCache.MeshCache[meshIdIndex][i] == null)
				{
					_assetCache.MeshCache[meshIdIndex][i] = new MeshCacheData();
				}

				if (_assetCache.MeshCache[meshIdIndex][i].MeshAttributes.Count == 0)
				{
					await ConstructMeshAttributes(primitive, meshIdIndex, i);
					if (primitive.Material != null)
					{
						await ConstructMaterialImageBuffers(primitive.Material.Value);
					}
				}
			}
		}

		protected async Task ConstructImageBuffer(GLTFTexture texture, int textureIndex)
		{
			int sourceId = GetTextureSourceId(texture);
			if (_assetCache.ImageStreamCache[sourceId] == null)
			{
				GLTFImage image = _gltfRoot.Images[sourceId];

				// we only load the streams if not a base64 uri, meaning the data is in the uri
				if (image.Uri != null && !URIHelper.IsBase64Uri(image.Uri))
				{
					await _loader.LoadStream(image.Uri);
					_assetCache.ImageStreamCache[sourceId] = _loader.LoadedStream;
				}
				else if (image.Uri == null && image.BufferView != null && _assetCache.BufferCache[image.BufferView.Value.Buffer.Id] == null)
				{
					int bufferIndex = image.BufferView.Value.Buffer.Id;
					await ConstructBuffer(_gltfRoot.Buffers[bufferIndex], bufferIndex);
				}
			}

			_assetCache.TextureCache[textureIndex] = new TextureCacheData
			{
				TextureDefinition = texture
			};
		}

		protected IEnumerator WaitUntilEnum(WaitUntil waitUntil)
		{
			yield return waitUntil;
		}

		private async Task LoadJson(string jsonFilePath)
		{
#if !WINDOWS_UWP
			 if (IsMultithreaded && _loader.HasSyncLoadMethod)
			 {
				Thread loadThread = new Thread(() => _loader.LoadStreamSync(jsonFilePath));
				loadThread.Priority = ThreadPriority.Highest;
				loadThread.Start();
				RunCoroutineSync(WaitUntilEnum(new WaitUntil(() => !loadThread.IsAlive)));
			 }
			 else
#endif
			 {
				// HACK: Force the coroutine to run synchronously in the editor
				await _loader.LoadStream(jsonFilePath);
			 }

			_gltfStream.Stream = _loader.LoadedStream;
			_gltfStream.StartPosition = 0;

#if !WINDOWS_UWP
			if (IsMultithreaded)
			{
				Thread parseJsonThread = new Thread(() => GLTFParser.ParseJson(_gltfStream.Stream, out _gltfRoot, _gltfStream.StartPosition));
				parseJsonThread.Priority = ThreadPriority.Highest;
				parseJsonThread.Start();
				RunCoroutineSync(WaitUntilEnum(new WaitUntil(() => !parseJsonThread.IsAlive)));
				if (_gltfRoot == null)
				{
					throw new GLTFLoadException("Failed to parse glTF");
				}
			}
			else
#endif
			{
				GLTFParser.ParseJson(_gltfStream.Stream, out _gltfRoot, _gltfStream.StartPosition);
			}
		}

		private static void RunCoroutineSync(IEnumerator streamEnum)
		{
			var stack = new Stack<IEnumerator>();
			stack.Push(streamEnum);
			while (stack.Count > 0)
			{
				var enumerator = stack.Pop();
				if (enumerator.MoveNext())
				{
					stack.Push(enumerator);
					var subEnumerator = enumerator.Current as IEnumerator;
					if (subEnumerator != null)
					{
						stack.Push(subEnumerator);
					}
				}
			}
		}

		private async Task _LoadNode(int nodeIndex)
		{
			if (nodeIndex >= _gltfRoot.Nodes.Count)
			{
				throw new ArgumentException("nodeIndex is out of range");
			}

			Node nodeToLoad = _gltfRoot.Nodes[nodeIndex];

			if (!IsMultithreaded)
			{
				await ConstructBufferData(nodeToLoad);
			}
			else
			{
				await Task.Run(() => ConstructBufferData(nodeToLoad));
			}

			await ConstructNode(nodeToLoad, nodeIndex);
		}

		/// <summary>
		/// Creates a scene based off loaded JSON. Includes loading in binary and image data to construct the meshes required.
		/// </summary>
		/// <param name="sceneIndex">The bufferIndex of scene in gltf file to load</param>
		/// <returns></returns>
		protected async Task _LoadScene(int sceneIndex = -1, bool showSceneObj = true)
		{
			GLTFScene scene;

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
				throw new GLTFLoadException("No default scene in gltf file.");
			}

			await ConstructScene(scene, showSceneObj);

			if (SceneParent != null)
			{
				CreatedObject.transform.SetParent(SceneParent, false);
			}

			_lastLoadedScene = CreatedObject;
		}

		protected async Task ConstructBuffer(GLTFBuffer buffer, int bufferIndex)
		{
			if (buffer.Uri == null)
			{
				_assetCache.BufferCache[bufferIndex] = ConstructBufferFromGLB(bufferIndex);
			}
			else
			{
				Stream bufferDataStream = null;
				var uri = buffer.Uri;

				byte[] bufferData;
				URIHelper.TryParseBase64(uri, out bufferData);
				if (bufferData != null)
				{
					bufferDataStream = new MemoryStream(bufferData, 0, bufferData.Length, false, true);
				}
				else
				{
					await _loader.LoadStream(buffer.Uri);
					bufferDataStream = _loader.LoadedStream;
				}

				_assetCache.BufferCache[bufferIndex] = new BufferCacheData
				{
					Stream = bufferDataStream
				};
			}
		}

		protected async Task ConstructImage(GLTFImage image, int imageCacheIndex, bool markGpuOnly, bool isLinear)
		{
			if (_assetCache.ImageCache[imageCacheIndex] == null)
			{
				Stream stream = null;
				if (image.Uri == null)
				{
					var bufferView = image.BufferView.Value;
					var data = new byte[bufferView.ByteLength];

					BufferCacheData bufferContents = _assetCache.BufferCache[bufferView.Buffer.Id];
					bufferContents.Stream.Position = bufferView.ByteOffset + bufferContents.ChunkOffset;
					stream = new SubStream(bufferContents.Stream, 0, data.Length);
				}
				else
				{
					string uri = image.Uri;

					byte[] bufferData;
					URIHelper.TryParseBase64(uri, out bufferData);
					if (bufferData != null)
					{
						stream = new MemoryStream(bufferData, 0, bufferData.Length, false, true);
					}
					else
					{
						stream = _assetCache.ImageStreamCache[imageCacheIndex];
					}
				}

				if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
				await ConstructUnityTexture(stream, markGpuOnly, isLinear, image, imageCacheIndex);
			}
		}

		protected virtual async Task ConstructUnityTexture(Stream stream, bool markGpuOnly, bool isLinear, GLTFImage image, int imageCacheIndex)
		{
			Texture2D texture = new Texture2D(0, 0, TextureFormat.RGBA32, true, isLinear);

			if (stream is MemoryStream)
			{
				using (MemoryStream memoryStream = stream as MemoryStream)
				{
					texture.LoadImage(memoryStream.ToArray(), markGpuOnly);
				}
			}
			else
			{
				byte[] buffer = new byte[stream.Length];

				// todo: potential optimization is to split stream read into multiple frames (or put it on a thread?)
				using (stream)
				{
					if (stream.Length > int.MaxValue)
					{
						throw new Exception("Stream is larger than can be copied into byte array");
					}
					stream.Read(buffer, 0, (int)stream.Length);
				}

				if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
				//	NOTE: the second parameter of LoadImage() marks non-readable, but we can't mark it until after we call Apply()
				texture.LoadImage(buffer, markGpuOnly);
			}

			_assetCache.ImageCache[imageCacheIndex] = texture;
		}

		protected virtual async Task ConstructMeshAttributes(MeshPrimitive primitive, int meshID, int primitiveIndex)
		{
			if (_assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes.Count == 0)
			{
				Dictionary<string, AttributeAccessor> attributeAccessors = new Dictionary<string, AttributeAccessor>(primitive.Attributes.Count + 1);
				foreach (var attributePair in primitive.Attributes)
				{
					BufferId bufferIdPair = attributePair.Value.Value.BufferView.Value.Buffer;
					GLTFBuffer buffer = bufferIdPair.Value;
					int bufferId = bufferIdPair.Id;

					// on cache miss, load the buffer
					if (_assetCache.BufferCache[bufferId] == null)
					{
						await ConstructBuffer(buffer, bufferId);
					}

					AttributeAccessor attributeAccessor = new AttributeAccessor
					{
						AccessorId = attributePair.Value,
						Stream = _assetCache.BufferCache[bufferId].Stream,
						Offset = (uint)_assetCache.BufferCache[bufferId].ChunkOffset
					};

					attributeAccessors[attributePair.Key] = attributeAccessor;
				}

				if (primitive.Indices != null)
				{
					int bufferId = primitive.Indices.Value.BufferView.Value.Buffer.Id;

					if (_assetCache.BufferCache[bufferId] == null)
					{
						await ConstructBuffer(primitive.Indices.Value.BufferView.Value.Buffer.Value, bufferId);
					}

					AttributeAccessor indexBuilder = new AttributeAccessor
					{
						AccessorId = primitive.Indices,
						Stream = _assetCache.BufferCache[bufferId].Stream,
						Offset = (uint)_assetCache.BufferCache[bufferId].ChunkOffset
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

#region Animation
		static string RelativePathFrom(Transform self, Transform root)
		{
			var path = new List<String>();
			for (var current = self; current !=  null; current = current.parent)
			{
				if (current == root)
				{
					return String.Join("/", path.ToArray());
				}

				path.Insert(0, current.name);
			}

			throw new Exception("no RelativePath");
		}

		protected virtual void BuildAnimationSamplers(GLTFAnimation animation, int animationId)
		{
			// look up expected data types
			var typeMap = new Dictionary<int, string>();
			foreach (var channel in animation.Channels)
			{
				typeMap[channel.Sampler.Id] = channel.Target.Path.ToString();
			}

			var samplers = _assetCache.AnimationCache[animationId].Samplers;
			var samplersByType = new Dictionary<string, List<AttributeAccessor>>
			{
				{"time", new List<AttributeAccessor>(animation.Samplers.Count)}
			};

			for (var i = 0; i < animation.Samplers.Count; i++)
			{
				// no sense generating unused samplers
				if (!typeMap.ContainsKey(i))
				{
					continue;
				}

				var samplerDef = animation.Samplers[i];

				// set up input accessors
				BufferCacheData bufferCacheData = _assetCache.BufferCache[samplerDef.Input.Value.BufferView.Value.Buffer.Id];
				AttributeAccessor attributeAccessor = new AttributeAccessor
				{
					AccessorId = samplerDef.Input,
					Stream = bufferCacheData.Stream,
					Offset = bufferCacheData.ChunkOffset
				};

				samplers[i].Input = attributeAccessor;
				samplersByType["time"].Add(attributeAccessor);

				// set up output accessors
				bufferCacheData = _assetCache.BufferCache[samplerDef.Output.Value.BufferView.Value.Buffer.Id];
				attributeAccessor = new AttributeAccessor
				{
					AccessorId = samplerDef.Output,
					Stream = bufferCacheData.Stream,
					Offset = bufferCacheData.ChunkOffset
				};

				samplers[i].Output = attributeAccessor;

				if (!samplersByType.ContainsKey(typeMap[i]))
				{
					samplersByType[typeMap[i]] = new List<AttributeAccessor>();
				}

				samplersByType[typeMap[i]].Add(attributeAccessor);
			}

			// populate attributeAccessors with buffer data
			GLTFHelpers.BuildAnimationSamplers(ref samplersByType);
		}

		protected void SetAnimationCurve(
			AnimationClip clip,
			string relativePath,
			string[] propertyNames,
			NumericArray input,
			NumericArray output,
			InterpolationType mode,
			Type curveType,
			ValuesConvertion getConvertedValues)
		{

			var channelCount = propertyNames.Length;
			var frameCount = input.AsFloats.Length;

			// copy all the key frame data to cache
			Keyframe[][] keyframes = new Keyframe[channelCount][];
			for( var ci = 0; ci < channelCount; ++ci)
			{
				keyframes[ci] = new Keyframe[frameCount];
			}

			for (var i = 0; i < frameCount; ++i)
			{
				var time = input.AsFloats[i];

				var values = getConvertedValues(output, i);

				for( var ci = 0; ci < channelCount; ++ci)
				{
					keyframes[ci][i] = new Keyframe(time, values[ci]);
				}
			}

			for( var ci = 0; ci < channelCount; ++ci)
			{
				// set interpolcation for each keyframe
				SetCurveMode(keyframes[ci], mode);
				// copy all key frames data to animation curve and add it to the clip
				AnimationCurve curve = new AnimationCurve();
				curve.keys = keyframes[ci];
				clip.SetCurve(relativePath, curveType, propertyNames[ci], curve);
			}
		}

		protected AnimationClip ConstructClip(Transform root, GameObject[] nodes, int animationId)
		{
			GLTFAnimation animation = _gltfRoot.Animations[animationId];

			AnimationCacheData animationCache = _assetCache.AnimationCache[animationId];
			if (animationCache == null)
			{
				animationCache = new AnimationCacheData(animation.Samplers.Count);
				_assetCache.AnimationCache[animationId] = animationCache;
			}
			else if (animationCache.LoadedAnimationClip != null)
			{
				return animationCache.LoadedAnimationClip;
			}

			// unpack accessors
			BuildAnimationSamplers(animation, animationId);

			// init clip
			AnimationClip clip = new AnimationClip
			{
				name = animation.Name ?? string.Format("animation:{0}", animationId)
			};
			_assetCache.AnimationCache[animationId].LoadedAnimationClip = clip;

			// needed because Animator component is unavailable at runtime
			clip.legacy = true;

			foreach (AnimationChannel channel in animation.Channels)
			{
				AnimationSamplerCacheData samplerCache = animationCache.Samplers[channel.Sampler.Id];
				Transform node = nodes[channel.Target.Node.Id].transform;
				string relativePath = RelativePathFrom(node, root);

				NumericArray input = samplerCache.Input.AccessorContent,
					output = samplerCache.Output.AccessorContent;

				string[] propertyNames;

				switch (channel.Target.Path)
				{
					case GLTFAnimationChannelPath.translation:
						propertyNames = new string[] { "localPosition.x", "localPosition.y", "localPosition.z" };

						SetAnimationCurve(clip, relativePath, propertyNames, input, output,
										  samplerCache.Interpolation, typeof(Transform),
										  (data, frame) => {
											  var position = data.AsVec3s[frame].ToUnityVector3Convert();
											  return new float[] { position.x, position.y, position.z};
										  });
						break;

					case GLTFAnimationChannelPath.rotation:
						propertyNames = new string[] { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };

						SetAnimationCurve(clip, relativePath, propertyNames, input, output,
										  samplerCache.Interpolation, typeof(Transform),
										  (data, frame) => {
											  var rotation = data.AsVec4s[frame];
											  var quaternion = new GLTF.Math.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W).ToUnityQuaternionConvert();
											  return new float[] { quaternion.x, quaternion.y, quaternion.z, quaternion.w};
										  });

						break;

					case GLTFAnimationChannelPath.scale:
						propertyNames = new string[] { "localScale.x", "localScale.y", "localScale.z" };

						SetAnimationCurve(clip, relativePath, propertyNames, input, output,
										  samplerCache.Interpolation, typeof(Transform),
										  (data, frame) => {
											  var scale = data.AsVec3s[frame].ToUnityVector3Raw();
											  return new float[] { scale.x, scale.y, scale.z};
										  });
						break;

					case GLTFAnimationChannelPath.weights:
						// TODO: add support for blend shapes/morph targets

						// var primitives = channel.Target.Node.Value.Mesh.Value.Primitives;
						// var targetCount = primitives[0].Targets.Count;
						// for (int primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
						// {
						// 	for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
						// 	{
						//
						// 		//clip.SetCurve(primitiveObjPath, typeof(SkinnedMeshRenderer), "blendShape." + targetIndex, curves[targetIndex]);
						// 	}
						// }
						break;

					default:
						Debug.LogWarning("Cannot read GLTF animation path");
						break;
				} // switch target type
			} // foreach channel

			clip.EnsureQuaternionContinuity();
			return clip;
		}

		public static void SetCurveMode(Keyframe[] keyframes, InterpolationType mode)
		{
			for (int i = 0; i < keyframes.Length; ++i)
			{
				float intangent = 0;
				float outtangent = 0;
				bool intangent_set = false;
				bool outtangent_set = false;
				Vector2 point1;
				Vector2 point2;
				Vector2 deltapoint;
				var key = keyframes[i];

				if (i == 0)
				{
					intangent = 0; intangent_set = true;
				}

				if (i == keyframes.Length - 1)
				{
					outtangent = 0; outtangent_set = true;
				}
				switch (mode)
				{
					case InterpolationType.STEP:
						{
							intangent = 0;
							outtangent = float.PositiveInfinity;
						}
						break;
					case InterpolationType.LINEAR:
						{
							if (!intangent_set)
							{
								point1.x = keyframes[i - 1].time;
								point1.y = keyframes[i - 1].value;
								point2.x = keyframes[i].time;
								point2.y = keyframes[i].value;

								deltapoint = point2 - point1;

								intangent = deltapoint.y / deltapoint.x;
							}
							if (!outtangent_set)
							{
								point1.x = keyframes[i].time;
								point1.y = keyframes[i].value;
								point2.x = keyframes[i + 1].time;
								point2.y = keyframes[i + 1].value;

								deltapoint = point2 - point1;

								outtangent = deltapoint.y / deltapoint.x;
							}
						}
						break;
					//use default unity curve
					case InterpolationType.CUBICSPLINE:
						break;
					case InterpolationType.CATMULLROMSPLINE:
						break;
					default:
						break;
				}


				key.inTangent = intangent;
				key.outTangent = outtangent;
			}
		}
#endregion

		protected virtual async Task ConstructScene(GLTFScene scene, bool showSceneObj)
		{
			var sceneObj = new GameObject(string.IsNullOrEmpty(scene.Name) ? ("GLTFScene") : scene.Name);
			sceneObj.SetActive(showSceneObj);

			Transform[] nodeTransforms = new Transform[scene.Nodes.Count];
			for (int i = 0; i < scene.Nodes.Count; ++i)
			{
				NodeId node = scene.Nodes[i];
				await _LoadNode(node.Id);
				GameObject nodeObj = _assetCache.NodeCache[node.Id];
				nodeObj.transform.SetParent(sceneObj.transform, false);
				nodeTransforms[i] = nodeObj.transform;
			}

			if (_gltfRoot.Animations != null && _gltfRoot.Animations.Count > 0)
			{
				// create the AnimationClip that will contain animation data
				Animation animation = sceneObj.AddComponent<Animation>();
				for (int i = 0; i < _gltfRoot.Animations.Count; ++i)
				{
					AnimationClip clip = ConstructClip(sceneObj.transform, _assetCache.NodeCache, i);

					clip.wrapMode = WrapMode.Loop;

					animation.AddClip(clip, clip.name);
					if (i == 0)
					{
						animation.clip = clip;
					}
				}
			}

			CreatedObject = sceneObj;
			InitializeGltfTopLevelObject();
		}


		protected virtual async Task ConstructNode(Node node, int nodeIndex)
		{
			if (_assetCache.NodeCache[nodeIndex] != null)
			{
				return;
			}

			var nodeObj = new GameObject(string.IsNullOrEmpty(node.Name) ? ("GLTFNode" + nodeIndex) : node.Name);
			// If we're creating a really large node, we need it to not be visible in partial stages. So we hide it while we create it
			nodeObj.SetActive(false);

			Vector3 position;
			Quaternion rotation;
			Vector3 scale;
			node.GetUnityTRSProperties(out position, out rotation, out scale);
			nodeObj.transform.localPosition = position;
			nodeObj.transform.localRotation = rotation;
			nodeObj.transform.localScale = scale;

			if (node.Mesh != null)
			{
				await ConstructMesh(node.Mesh.Value, nodeObj.transform, node.Mesh.Id, node.Skin != null ? node.Skin.Value : null);
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
					await ConstructNode(child.Value, child.Id);
					GameObject childObj = _assetCache.NodeCache[child.Id];
					childObj.transform.SetParent(nodeObj.transform, false);
				}
			}

			nodeObj.SetActive(true);
			_assetCache.NodeCache[nodeIndex] = nodeObj;

			const string msft_LODExtName = MSFT_LODExtensionFactory.EXTENSION_NAME;
			MSFT_LODExtension lodsextension = null;
			if (_gltfRoot.ExtensionsUsed != null
				&& _gltfRoot.ExtensionsUsed.Contains(msft_LODExtName)
				&& node.Extensions != null
				&& node.Extensions.ContainsKey(msft_LODExtName))
			{
				lodsextension = node.Extensions[msft_LODExtName] as MSFT_LODExtension;
				if (lodsextension != null && lodsextension.MeshIds.Count > 0)
				{
					int lodCount = lodsextension.MeshIds.Count + 1;
					if (!CullFarLOD)
					{
						//create a final lod with the mesh as the last LOD in file
						lodCount += 1;
					}
					LOD[] lods = new LOD[lodsextension.MeshIds.Count + 2];
					List<double> lodCoverage = lodsextension.GetLODCoverage(node);

					var lodGroupNodeObj = new GameObject(string.IsNullOrEmpty(node.Name) ? ("GLTFNode_LODGroup" + nodeIndex) : node.Name);
					lodGroupNodeObj.SetActive(false);
					nodeObj.transform.SetParent(lodGroupNodeObj.transform, false);
					MeshRenderer[] childRenders = nodeObj.GetComponentsInChildren<MeshRenderer>();
					lods[0] = new LOD(GetLodCoverage(lodCoverage, 0), childRenders);

					LODGroup lodGroup = lodGroupNodeObj.AddComponent<LODGroup>();
					for (int i = 0; i < lodsextension.MeshIds.Count; i++)
					{
						int lodNodeId = lodsextension.MeshIds[i];
						await ConstructNode(_gltfRoot.Nodes[lodNodeId], lodNodeId);
						int lodIndex = i + 1;
						GameObject lodNodeObj = _assetCache.NodeCache[lodNodeId];
						lodNodeObj.transform.SetParent(lodGroupNodeObj.transform, false);
						childRenders = lodNodeObj.GetComponentsInChildren<MeshRenderer>();
						lods[lodIndex] = new LOD(GetLodCoverage(lodCoverage, lodIndex), childRenders);
					}

					if (!CullFarLOD)
					{
						//use the last mesh as the LOD
						lods[lodsextension.MeshIds.Count + 1] = new LOD(0, childRenders);
					}

					lodGroup.SetLODs(lods);
					lodGroup.RecalculateBounds();
					lodGroupNodeObj.SetActive(true);
					_assetCache.NodeCache[nodeIndex] = lodGroupNodeObj;
				}
			}

		}

		float GetLodCoverage(List<double> lodcoverageExtras, int lodIndex)
		{
			if (lodcoverageExtras != null && lodIndex < lodcoverageExtras.Count)
			{
				return (float)lodcoverageExtras[lodIndex];
			}
			else
			{
				return 1.0f / (lodIndex + 2);
			}
		}

		private bool NeedsSkinnedMeshRenderer(MeshPrimitive primitive, Skin skin)
		{
			return HasBones(skin) || HasBlendShapes(primitive);
		}

		private bool HasBones(Skin skin)
		{
			return skin != null;
		}

		private bool HasBlendShapes(MeshPrimitive primitive)
		{
			return primitive.Targets != null;
		}

		protected virtual async Task SetupBones(Skin skin, MeshPrimitive primitive, SkinnedMeshRenderer renderer, GameObject primitiveObj, Mesh curMesh)
		{
			var boneCount = skin.Joints.Count;
			Transform[] bones = new Transform[boneCount];

			int bufferId = skin.InverseBindMatrices.Value.BufferView.Value.Buffer.Id;
			AttributeAccessor attributeAccessor = new AttributeAccessor
			{
				AccessorId = skin.InverseBindMatrices,
				Stream = _assetCache.BufferCache[bufferId].Stream,
				Offset = _assetCache.BufferCache[bufferId].ChunkOffset
			};

			GLTFHelpers.BuildBindPoseSamplers(ref attributeAccessor);

			Matrix4x4[] gltfBindPoses = attributeAccessor.AccessorContent.AsMatrix4x4s;
			UnityEngine.Matrix4x4[] bindPoses = new UnityEngine.Matrix4x4[skin.Joints.Count];

			for (int i = 0; i < boneCount; i++)
			{
				if (_assetCache.NodeCache[skin.Joints[i].Id] == null)
				{
					await ConstructNode(_gltfRoot.Nodes[skin.Joints[i].Id], skin.Joints[i].Id);
				}
				bones[i] = _assetCache.NodeCache[skin.Joints[i].Id].transform;
				bindPoses[i] = gltfBindPoses[i].ToUnityMatrix4x4Convert();
			}

			renderer.rootBone = _assetCache.NodeCache[skin.Skeleton.Id].transform;
			curMesh.bindposes = bindPoses;
			renderer.bones = bones;
		}

		private BoneWeight[] CreateBoneWeightArray(Vector4[] joints, Vector4[] weights, int vertCount)
		{
			NormalizeBoneWeightArray(weights);

			BoneWeight[] boneWeights = new BoneWeight[vertCount];
			for (int i = 0; i < vertCount; i++)
			{
				boneWeights[i].boneIndex0 = (int)joints[i].x;
				boneWeights[i].boneIndex1 = (int)joints[i].y;
				boneWeights[i].boneIndex2 = (int)joints[i].z;
				boneWeights[i].boneIndex3 = (int)joints[i].w;

				boneWeights[i].weight0 = weights[i].x;
				boneWeights[i].weight1 = weights[i].y;
				boneWeights[i].weight2 = weights[i].z;
				boneWeights[i].weight3 = weights[i].w;
			}

			return boneWeights;
		}

		/// <summary>
		/// Ensures each bone weight influences applied to the vertices add up to 1
		/// </summary>
		/// <param name="weights">Bone weight array</param>
		private void NormalizeBoneWeightArray(Vector4[] weights)
		{
			for (int i = 0; i < weights.Length; i++)
			{
				var weightSum = (weights[i].x + weights[i].y + weights[i].z + weights[i].w);

				if (!Mathf.Approximately(weightSum, 0))
				{
					weights[i] /= weightSum;
				}
			}
		}

		protected virtual async Task ConstructMesh(GLTFMesh mesh, Transform parent, int meshId, Skin skin)
		{
			if (_assetCache.MeshCache[meshId] == null)
			{
				_assetCache.MeshCache[meshId] = new MeshCacheData[mesh.Primitives.Count];
			}

			for (int i = 0; i < mesh.Primitives.Count; ++i)
			{
				var primitive = mesh.Primitives[i];
				int materialIndex = primitive.Material != null ? primitive.Material.Id : -1;

				await ConstructMeshPrimitive(primitive, meshId, i, materialIndex);

				var primitiveObj = new GameObject("Primitive");

				MaterialCacheData materialCacheData =
					materialIndex >= 0 ? _assetCache.MaterialCache[materialIndex] : _defaultLoadedMaterial;

				Material material = materialCacheData.GetContents(primitive.Attributes.ContainsKey(SemanticProperties.Color(0)));

				Mesh curMesh = _assetCache.MeshCache[meshId][i].LoadedMesh;
				if (NeedsSkinnedMeshRenderer(primitive, skin))
				{
					var skinnedMeshRenderer = primitiveObj.AddComponent<SkinnedMeshRenderer>();
					skinnedMeshRenderer.material = material;
					skinnedMeshRenderer.quality = SkinQuality.Auto;
					// TODO: add support for blend shapes/morph targets
					//if (HasBlendShapes(primitive))
					//	SetupBlendShapes(primitive);
					if (HasBones(skin))
					{
						await SetupBones(skin, primitive, skinnedMeshRenderer, primitiveObj, curMesh);
					}

					skinnedMeshRenderer.sharedMesh = curMesh;
				}
				else
				{
					var meshRenderer = primitiveObj.AddComponent<MeshRenderer>();
					meshRenderer.material = material;
				}

				MeshFilter meshFilter = primitiveObj.AddComponent<MeshFilter>();
				meshFilter.sharedMesh = curMesh;

				switch (Collider)
				{
					case ColliderType.Box:
						var boxCollider = primitiveObj.AddComponent<BoxCollider>();
						boxCollider.center = curMesh.bounds.center;
						boxCollider.size = curMesh.bounds.size;
						break;
					case ColliderType.Mesh:
						var meshCollider = primitiveObj.AddComponent<MeshCollider>();
						meshCollider.sharedMesh = curMesh;
						break;
					case ColliderType.MeshConvex:
						var meshConvexCollider = primitiveObj.AddComponent<MeshCollider>();
						meshConvexCollider.sharedMesh = curMesh;
						meshConvexCollider.convex = true;
						break;
				}

				primitiveObj.transform.SetParent(parent, false);
				primitiveObj.SetActive(true);
				_assetCache.MeshCache[meshId][i].PrimitiveGO = primitiveObj;
			}
		}


		protected virtual async Task ConstructMeshPrimitive(MeshPrimitive primitive, int meshID, int primitiveIndex, int materialIndex)
		{
			if (_assetCache.MeshCache[meshID][primitiveIndex] == null)
			{
				_assetCache.MeshCache[meshID][primitiveIndex] = new MeshCacheData();
			}
			if (_assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh == null)
			{
				var meshAttributes = _assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes;
				var meshConstructionData = new MeshConstructionData
				{
					Primitive = primitive,
					MeshAttributes = meshAttributes
				};

				UnityMeshData unityMeshData = null;
				if (IsMultithreaded)
				{
					await Task.Run(() => unityMeshData = ConvertAccessorsToUnityTypes(meshConstructionData));
				}
				else
				{
					unityMeshData = ConvertAccessorsToUnityTypes(meshConstructionData);
				}

				await ConstructUnityMesh(meshConstructionData, meshID, primitiveIndex, unityMeshData);
			}

			bool shouldUseDefaultMaterial = primitive.Material == null;

			GLTFMaterial materialToLoad = shouldUseDefaultMaterial ? DefaultMaterial : primitive.Material.Value;
			if ((shouldUseDefaultMaterial && _defaultLoadedMaterial == null) ||
				(!shouldUseDefaultMaterial && _assetCache.MaterialCache[materialIndex] == null))
			{
				await ConstructMaterial(materialToLoad, shouldUseDefaultMaterial ? -1 : materialIndex);
			}
		}

		protected UnityMeshData ConvertAccessorsToUnityTypes(MeshConstructionData meshConstructionData)
		{
			// todo optimize: There are multiple copies being performed to turn the buffer data into mesh data. Look into reducing them
			MeshPrimitive primitive = meshConstructionData.Primitive;
			Dictionary<string, AttributeAccessor> meshAttributes = meshConstructionData.MeshAttributes;

			int vertexCount = (int)primitive.Attributes[SemanticProperties.POSITION].Value.Count;

			return new UnityMeshData
			{
				Vertices = primitive.Attributes.ContainsKey(SemanticProperties.POSITION)
					? meshAttributes[SemanticProperties.POSITION].AccessorContent.AsVertices.ToUnityVector3Raw()
					: null,

				Normals = primitive.Attributes.ContainsKey(SemanticProperties.NORMAL)
					? meshAttributes[SemanticProperties.NORMAL].AccessorContent.AsNormals.ToUnityVector3Raw()
					: null,

				Uv1 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(0))
					? meshAttributes[SemanticProperties.TexCoord(0)].AccessorContent.AsTexcoords.ToUnityVector2Raw()
					: null,

				Uv2 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(1))
					? meshAttributes[SemanticProperties.TexCoord(1)].AccessorContent.AsTexcoords.ToUnityVector2Raw()
					: null,

				Uv3 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(2))
					? meshAttributes[SemanticProperties.TexCoord(2)].AccessorContent.AsTexcoords.ToUnityVector2Raw()
					: null,

				Uv4 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(3))
					? meshAttributes[SemanticProperties.TexCoord(3)].AccessorContent.AsTexcoords.ToUnityVector2Raw()
					: null,

				Colors = primitive.Attributes.ContainsKey(SemanticProperties.Color(0))
					? meshAttributes[SemanticProperties.Color(0)].AccessorContent.AsColors.ToUnityColorRaw()
					: null,

				Triangles = primitive.Indices != null
					? meshAttributes[SemanticProperties.INDICES].AccessorContent.AsUInts.ToIntArrayRaw()
					: MeshPrimitive.GenerateTriangles(vertexCount),

				Tangents = primitive.Attributes.ContainsKey(SemanticProperties.TANGENT)
					? meshAttributes[SemanticProperties.TANGENT].AccessorContent.AsTangents.ToUnityVector4Raw()
					: null,

				BoneWeights = meshAttributes.ContainsKey(SemanticProperties.Weight(0)) && meshAttributes.ContainsKey(SemanticProperties.Joint(0))
					? CreateBoneWeightArray(meshAttributes[SemanticProperties.Joint(0)].AccessorContent.AsVec4s.ToUnityVector4Raw(),
					meshAttributes[SemanticProperties.Weight(0)].AccessorContent.AsVec4s.ToUnityVector4Raw(), vertexCount)
					: null
			};
		}

		protected virtual Task ConstructMaterialImageBuffers(GLTFMaterial def)
		{
			var tasks = new List<Task>(8);
			if (def.PbrMetallicRoughness != null)
			{
				var pbr = def.PbrMetallicRoughness;

				if (pbr.BaseColorTexture != null)
				{
					var textureId = pbr.BaseColorTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
				if (pbr.MetallicRoughnessTexture != null)
				{
					var textureId = pbr.MetallicRoughnessTexture.Index;

					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			if (def.CommonConstant != null)
			{
				if (def.CommonConstant.LightmapTexture != null)
				{
					var textureId = def.CommonConstant.LightmapTexture.Index;

					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			if (def.NormalTexture != null)
			{
				var textureId = def.NormalTexture.Index;
				tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
			}

			if (def.OcclusionTexture != null)
			{
				var textureId = def.OcclusionTexture.Index;

				if (!(def.PbrMetallicRoughness != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id == textureId.Id))
				{
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			if (def.EmissiveTexture != null)
			{
				var textureId = def.EmissiveTexture.Index;
				tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
			}

			// pbr_spec_gloss extension
			const string specGlossExtName = KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME;
			if (def.Extensions != null && def.Extensions.ContainsKey(specGlossExtName))
			{
				var specGlossDef = (KHR_materials_pbrSpecularGlossinessExtension)def.Extensions[specGlossExtName];
				if (specGlossDef.DiffuseTexture != null)
				{
					var textureId = specGlossDef.DiffuseTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}

				if (specGlossDef.SpecularGlossinessTexture != null)
				{
					var textureId = specGlossDef.SpecularGlossinessTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			return Task.WhenAll(tasks);
		}

		protected async Task ConstructUnityMesh(MeshConstructionData meshConstructionData, int meshId, int primitiveIndex, UnityMeshData unityMeshData)
		{
			MeshPrimitive primitive = meshConstructionData.Primitive;
			int vertexCount = (int)primitive.Attributes[SemanticProperties.POSITION].Value.Count;
			bool hasNormals = unityMeshData.Normals != null;

			if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
			Mesh mesh = new Mesh
			{

#if UNITY_2017_3_OR_NEWER
				indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
#endif
			};

			mesh.vertices = unityMeshData.Vertices;
			if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
			mesh.normals = unityMeshData.Normals;
			if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
			mesh.uv = unityMeshData.Uv1;
			if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
			mesh.uv2 = unityMeshData.Uv2;
			if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
			mesh.uv3 = unityMeshData.Uv3;
			if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
			mesh.uv4 = unityMeshData.Uv4;
			if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
			mesh.colors = unityMeshData.Colors;
			if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
			mesh.triangles = unityMeshData.Triangles;
			if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
			mesh.tangents = unityMeshData.Tangents;
			if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();
			mesh.boneWeights = unityMeshData.BoneWeights;
			if (_asyncCoroutineHelper != null) await _asyncCoroutineHelper.YieldOnTimeout();

			if (!hasNormals)
			{
				mesh.RecalculateNormals();
			}

			if (!KeepCPUCopyOfMesh)
			{
				mesh.UploadMeshData(true);
			}

			_assetCache.MeshCache[meshId][primitiveIndex].LoadedMesh = mesh;
		}

		protected virtual async Task ConstructMaterial(GLTFMaterial def, int materialIndex)
		{
			IUniformMap mapper;
			const string specGlossExtName = KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME;
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(specGlossExtName)
				&& def.Extensions != null && def.Extensions.ContainsKey(specGlossExtName))
			{
				if (!string.IsNullOrEmpty(CustomShaderName))
				{
					mapper = new SpecGlossMap(CustomShaderName, MaximumLod);
				}
				else
				{
					mapper = new SpecGlossMap(MaximumLod);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(CustomShaderName))
				{
					mapper = new MetalRoughMap(CustomShaderName, MaximumLod);
				}
				else
				{
					mapper = new MetalRoughMap(MaximumLod);
				}
			}

			mapper.Material.name = def.Name;
			mapper.AlphaMode = def.AlphaMode;
			mapper.DoubleSided = def.DoubleSided;

			var mrMapper = mapper as IMetalRoughUniformMap;
			if (def.PbrMetallicRoughness != null && mrMapper != null)
			{
				var pbr = def.PbrMetallicRoughness;

				mrMapper.BaseColorFactor = pbr.BaseColorFactor.ToUnityColorRaw();

				if (pbr.BaseColorTexture != null)
				{
					TextureId textureId = pbr.BaseColorTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false);
					mrMapper.BaseColorTexture = _assetCache.TextureCache[textureId.Id].Texture;
					mrMapper.BaseColorTexCoord = pbr.BaseColorTexture.TexCoord;

					var ext = GetTextureTransform(pbr.BaseColorTexture);
					if(ext != null)
					{
						mrMapper.BaseColorXOffset = ext.Offset.ToUnityVector2Raw();
						mrMapper.BaseColorXRotation = ext.Rotation;
						mrMapper.BaseColorXScale = ext.Scale.ToUnityVector2Raw();
						mrMapper.BaseColorXTexCoord = ext.TexCoord;
					}
				}

				mrMapper.MetallicFactor = pbr.MetallicFactor;

				if (pbr.MetallicRoughnessTexture != null)
				{
					TextureId textureId = pbr.MetallicRoughnessTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, true);
					mrMapper.MetallicRoughnessTexture = _assetCache.TextureCache[textureId.Id].Texture;
					mrMapper.MetallicRoughnessTexCoord = pbr.MetallicRoughnessTexture.TexCoord;

					var ext = GetTextureTransform(pbr.MetallicRoughnessTexture);
					if (ext != null)
					{
						mrMapper.MetallicRoughnessXOffset = ext.Offset.ToUnityVector2Raw();
						mrMapper.MetallicRoughnessXRotation = ext.Rotation;
						mrMapper.MetallicRoughnessXScale = ext.Scale.ToUnityVector2Raw();
						mrMapper.MetallicRoughnessXTexCoord = ext.TexCoord;
					}
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
					TextureId textureId = specGloss.DiffuseTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false);
					sgMapper.DiffuseTexture = _assetCache.TextureCache[textureId.Id].Texture;
					sgMapper.DiffuseTexCoord = specGloss.DiffuseTexture.TexCoord;

					var ext = GetTextureTransform(specGloss.DiffuseTexture);
					if (ext != null)
					{
						sgMapper.DiffuseXOffset = ext.Offset.ToUnityVector2Raw();
						sgMapper.DiffuseXRotation = ext.Rotation;
						sgMapper.DiffuseXScale = ext.Scale.ToUnityVector2Raw();
						sgMapper.DiffuseXTexCoord = ext.TexCoord;
					}
				}

				sgMapper.SpecularFactor = specGloss.SpecularFactor.ToUnityVector3Raw();
				sgMapper.GlossinessFactor = specGloss.GlossinessFactor;

				if (specGloss.SpecularGlossinessTexture != null)
				{
					TextureId textureId = specGloss.SpecularGlossinessTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false);
					sgMapper.SpecularGlossinessTexture = _assetCache.TextureCache[textureId.Id].Texture;

					var ext = GetTextureTransform(specGloss.SpecularGlossinessTexture);
					if (ext != null)
					{
						sgMapper.SpecularGlossinessXOffset = ext.Offset.ToUnityVector2Raw();
						sgMapper.SpecularGlossinessXRotation = ext.Rotation;
						sgMapper.SpecularGlossinessXScale = ext.Scale.ToUnityVector2Raw();
						sgMapper.SpecularGlossinessXTexCoord = ext.TexCoord;
					}
				}
			}

			if (def.NormalTexture != null)
			{
				TextureId textureId = def.NormalTexture.Index;
				await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, true);
				mapper.NormalTexture = _assetCache.TextureCache[textureId.Id].Texture;
				mapper.NormalTexCoord = def.NormalTexture.TexCoord;
				mapper.NormalTexScale = def.NormalTexture.Scale;

				var ext = GetTextureTransform(def.NormalTexture);
				if (ext != null)
				{
					mapper.NormalXOffset = ext.Offset.ToUnityVector2Raw();
					mapper.NormalXRotation = ext.Rotation;
					mapper.NormalXScale = ext.Scale.ToUnityVector2Raw();
					mapper.NormalXTexCoord = ext.TexCoord;
				}
			}

			if (def.OcclusionTexture != null)
			{
				mapper.OcclusionTexStrength = def.OcclusionTexture.Strength;
				TextureId textureId = def.OcclusionTexture.Index;
				await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, true);
				mapper.OcclusionTexture = _assetCache.TextureCache[textureId.Id].Texture;

				var ext = GetTextureTransform(def.OcclusionTexture);
				if (ext != null)
				{
					mapper.OcclusionXOffset = ext.Offset.ToUnityVector2Raw();
					mapper.OcclusionXRotation = ext.Rotation;
					mapper.OcclusionXScale = ext.Scale.ToUnityVector2Raw();
					mapper.OcclusionXTexCoord = ext.TexCoord;
				}
			}

			if (def.EmissiveTexture != null)
			{
				TextureId textureId = def.EmissiveTexture.Index;
				await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false);
				mapper.EmissiveTexture = _assetCache.TextureCache[textureId.Id].Texture;
				mapper.EmissiveTexCoord = def.EmissiveTexture.TexCoord;

				var ext = GetTextureTransform(def.EmissiveTexture);
				if (ext != null)
				{
					mapper.EmissiveXOffset = ext.Offset.ToUnityVector2Raw();
					mapper.EmissiveXRotation = ext.Rotation;
					mapper.EmissiveXScale = ext.Scale.ToUnityVector2Raw();
					mapper.EmissiveXTexCoord = ext.TexCoord;
				}
			}

			mapper.EmissiveFactor = def.EmissiveFactor.ToUnityColorRaw();

			var vertColorMapper = mapper.Clone();
			vertColorMapper.VertexColorsEnabled = true;

			MaterialCacheData materialWrapper = new MaterialCacheData
			{
				UnityMaterial = mapper.Material,
				UnityMaterialWithVertexColor = vertColorMapper.Material,
				GLTFMaterial = def
			};

			if (materialIndex >= 0)
			{
				_assetCache.MaterialCache[materialIndex] = materialWrapper;
			}
			else
			{
				_defaultLoadedMaterial = materialWrapper;
			}
		}


		protected virtual int GetTextureSourceId(GLTFTexture texture)
		{
			return texture.Source.Id;
		}

		/// <summary>
		/// Creates a texture from a glTF texture
		/// </summary>
		/// <param name="texture">The texture to load</param>
		/// <param name="textureIndex">The index in the texture cache</param>
		/// <param name="markGpuOnly">Whether the texture is GPU only, instead of keeping a CPU copy</param>
		/// <param name="isLinear">Whether the texture is linear rather than sRGB</param>
		/// <returns>The loading task</returns>
		public virtual async Task LoadTextureAsync(GLTFTexture texture, int textureIndex, bool markGpuOnly, bool isLinear)
		{
			try
			{
				lock (this)
				{
					if (_isRunning)
					{
						throw new GLTFLoadException("Cannot CreateTexture while GLTFSceneImporter is already running");
					}

					_isRunning = true;
				}

				if (_gltfRoot == null)
				{
					await LoadJson(_gltfFileName);
				}

				if (_assetCache == null)
				{
					_assetCache = new AssetCache(_gltfRoot);
				}

				await ConstructImageBuffer(texture, textureIndex);
				await ConstructTexture(texture, textureIndex, markGpuOnly, isLinear);
			}
			finally
			{
				lock (this)
				{
					_isRunning = false;
				}
			}
		}

		public virtual Task LoadTextureAsync(GLTFTexture texture, int textureIndex, bool isLinear)
		{
			return LoadTextureAsync(texture, textureIndex, !KeepCPUCopyOfTexture, isLinear);
		}

		/// <summary>
		/// Gets texture that has been loaded from CreateTexture
		/// </summary>
		/// <param name="textureIndex">The texture to get</param>
		/// <returns>Created texture</returns>
		public virtual Texture GetTexture(int textureIndex)
		{
			if (_assetCache == null)
			{
				throw new GLTFLoadException("Asset cache needs initialized before calling GetTexture");
			}

			if (_assetCache.TextureCache[textureIndex] == null)
			{
				return null;
			}

			return _assetCache.TextureCache[textureIndex].Texture;
		}

		protected virtual async Task ConstructTexture(GLTFTexture texture, int textureIndex,
			bool markGpuOnly, bool isLinear)
		{
			if (_assetCache.TextureCache[textureIndex].Texture == null)
			{
				int sourceId = GetTextureSourceId(texture);
				GLTFImage image = _gltfRoot.Images[sourceId];
				await ConstructImage(image, sourceId, markGpuOnly, isLinear);

				var source = _assetCache.ImageCache[sourceId];
				FilterMode desiredFilterMode;
				TextureWrapMode desiredWrapMode;

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
							desiredFilterMode = FilterMode.Bilinear;
							break;
						case MinFilterMode.LinearMipmapLinear:
							desiredFilterMode = FilterMode.Trilinear;
							break;
						default:
							Debug.LogWarning("Unsupported Sampler.MinFilter: " + sampler.MinFilter);
							desiredFilterMode = FilterMode.Trilinear;
							break;
					}

					switch (sampler.WrapS)
					{
						case GLTF.Schema.WrapMode.ClampToEdge:
							desiredWrapMode = TextureWrapMode.Clamp;
							break;
						case GLTF.Schema.WrapMode.Repeat:
							desiredWrapMode = TextureWrapMode.Repeat;
							break;
						case GLTF.Schema.WrapMode.MirroredRepeat:
							desiredWrapMode = TextureWrapMode.Mirror;
							break;
						default:
							Debug.LogWarning("Unsupported Sampler.WrapS: " + sampler.WrapS);
							desiredWrapMode = TextureWrapMode.Repeat;
							break;
					}
				}
				else
				{
					desiredFilterMode = FilterMode.Trilinear;
					desiredWrapMode = TextureWrapMode.Repeat;
				}

				var matchSamplerState = source.filterMode == desiredFilterMode && source.wrapMode == desiredWrapMode;
				if (matchSamplerState || markGpuOnly)
				{
					_assetCache.TextureCache[textureIndex].Texture = source;

					if (!matchSamplerState)
					{
						Debug.LogWarning($"Ignoring sampler; filter mode: source {source.filterMode}, desired {desiredFilterMode}; wrap mode: source {source.wrapMode}, desired {desiredWrapMode}");
					}
				}
				else
				{
					var unityTexture = Object.Instantiate(source);
					unityTexture.filterMode = desiredFilterMode;
					unityTexture.wrapMode = desiredWrapMode;

					_assetCache.TextureCache[textureIndex].Texture = unityTexture;
				}
			}
		}

		protected virtual void ConstructImageFromGLB(GLTFImage image, int imageCacheIndex)
		{
			var texture = new Texture2D(0, 0);
			var bufferView = image.BufferView.Value;
			var data = new byte[bufferView.ByteLength];

			var bufferContents = _assetCache.BufferCache[bufferView.Buffer.Id];
			bufferContents.Stream.Position = bufferView.ByteOffset + bufferContents.ChunkOffset;
			bufferContents.Stream.Read(data, 0, data.Length);
			texture.LoadImage(data);

			_assetCache.ImageCache[imageCacheIndex] = texture;

		}

		protected virtual BufferCacheData ConstructBufferFromGLB(int bufferIndex)
		{
			GLTFParser.SeekToBinaryChunk(_gltfStream.Stream, bufferIndex, _gltfStream.StartPosition);  // sets stream to correct start position
			return new BufferCacheData
			{
				Stream = _gltfStream.Stream,
				ChunkOffset = (uint)_gltfStream.Stream.Position
			};
		}

		protected virtual ExtTextureTransformExtension GetTextureTransform(TextureInfo def)
		{
			IExtension extension;
			if (_gltfRoot.ExtensionsUsed != null &&
				_gltfRoot.ExtensionsUsed.Contains(ExtTextureTransformExtensionFactory.EXTENSION_NAME) &&
				def.Extensions != null &&
				def.Extensions.TryGetValue(ExtTextureTransformExtensionFactory.EXTENSION_NAME, out extension))
			{
				return (ExtTextureTransformExtension)extension;
			}
			else return null;
		}


		/// <summary>
		///	 Get the absolute path to a gltf uri reference.
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

		/// <summary>
		/// Cleans up any undisposed streams after loading a scene or a node.
		/// </summary>
		private void Cleanup()
		{
			_assetCache.Dispose();
			_assetCache = null;
		}
	}
}
