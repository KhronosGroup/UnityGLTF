using GLTF;
using GLTF.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Loader;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
#if !WINDOWS_UWP && !UNITY_WEBGL
using ThreadPriority = System.Threading.ThreadPriority;
#endif
using WrapMode = UnityEngine.WrapMode;

namespace UnityGLTF
{
	public class ImportOptions
	{
#pragma warning disable CS0618 // Type or member is obsolete
		public ILoader ExternalDataLoader = null;
#pragma warning restore CS0618 // Type or member is obsolete

		/// <summary>
		/// Optional <see cref="IDataLoader"/> for loading references from the GLTF to external streams.  May also optionally implement <see cref="IDataLoader2"/>.
		/// </summary>
		public IDataLoader DataLoader = null;
		public AsyncCoroutineHelper AsyncCoroutineHelper = null;
		public bool ThrowOnLowMemory = true;
		public AnimationMethod AnimationMethod = AnimationMethod.Mecanim;
		public bool AnimationLoopTime = true;
		public bool AnimationLoopPose = false;

		[NonSerialized]
		public ILogger logger;
	}

	public enum AnimationMethod
	{
		None,
		Legacy,
		Mecanim,
		MecanimHumanoid,
	}

	public class UnityMeshData
	{
		public Vector3[] Vertices;
		public Vector3[] Normals;
		public Vector4[] Tangents;
		public Vector2[] Uv1;
		public Vector2[] Uv2;
		public Vector2[] Uv3;
		public Vector2[] Uv4;
		public Color[] Colors;
		public BoneWeight[] BoneWeights;

		public Vector3[][] MorphTargetVertices;
		public Vector3[][] MorphTargetNormals;
		public Vector3[][] MorphTargetTangents;

		public MeshTopology[] Topology;
		public int[][] Indices;
	}

	public struct ImportProgress
	{
		public bool IsDownloaded;

		public int NodeTotal;
		public int NodeLoaded;

		public int TextureTotal;
		public int TextureLoaded;

		public int BuffersTotal;
		public int BuffersLoaded;

		public float Progress
		{
			get
			{
				int total = NodeTotal + TextureTotal + BuffersTotal;
				int loaded = NodeLoaded + TextureLoaded + BuffersLoaded;
				if (total > 0)
				{
					return (float)loaded / total;
				}
				else
				{
					return 0.0f;
				}
			}
		}

		public override string ToString()
		{
			return $"{(Progress * 100.0):F2}% (Buffers: {BuffersLoaded}/{BuffersTotal}, Nodes: {NodeLoaded}/{NodeTotal}, Texs: {TextureLoaded}/{TextureTotal})";
		}
	}

	public struct ImportStatistics
	{
		public long TriangleCount;
		public long VertexCount;
	}

	/// <summary>
	/// Converts gltf animation data to unity
	/// </summary>
	public delegate float[] ValuesConvertion(NumericArray data, int frame);

	public partial class GLTFSceneImporter : IDisposable
	{
		public enum ColliderType
		{
			None,
			Box,
			Mesh,
			MeshConvex
		}

		protected struct GLBStream
		{
			public Stream Stream;
			public long StartPosition;
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
				return (Application.isEditor || Application.platform == RuntimePlatform.WebGLPlayer) ? false : _isMultithreaded;
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
		/// All created animation clips
		/// </summary>
		public AnimationClip[] CreatedAnimationClips { get; private set; }

		/// <summary>
		/// Adds colliders to primitive objects when created
		/// </summary>
		public ColliderType Collider { get; set; }

		/// <summary>
		/// Override for the shader to use on created materials
		/// </summary>
		public string CustomShaderName { get; set; }

		public GameObject LastLoadedScene
		{
			get { return _lastLoadedScene; }
		}

		public TextureCacheData[] TextureCache => _assetCache.TextureCache;

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
		/// Specifies whether the MipMap chain should be generated for model textures
		/// </summary>
		public bool GenerateMipMapsForTextures = true;

		/// <summary>
		/// When screen coverage is above threshold and no LOD mesh, cull the object
		/// </summary>
		public bool CullFarLOD = false;

		/// <summary>
		/// Statistics from the scene
		/// </summary>
		public ImportStatistics Statistics;

		protected ImportOptions _options;
		protected MemoryChecker _memoryChecker;

		protected GameObject _lastLoadedScene;
		protected readonly GLTFMaterial DefaultMaterial = new GLTFMaterial();
		protected MaterialCacheData _defaultLoadedMaterial = null;

		protected string _gltfFileName;
		protected GLBStream _gltfStream;
		protected GLTFRoot _gltfRoot;
		protected AssetCache _assetCache;
		protected bool _isRunning = false;

		protected ImportProgress progressStatus = default(ImportProgress);
		protected IProgress<ImportProgress> progress = null;

		private static ILogger Debug = UnityEngine.Debug.unityLogger;

		public GLTFSceneImporter(string gltfFileName, ImportOptions options)
		{
			_gltfFileName = gltfFileName;
			_options = options;

			if (options.logger != null)
				Debug = options.logger;
			else
				Debug = UnityEngine.Debug.unityLogger;

			if (_options.DataLoader == null)
			{
				_options.DataLoader = LegacyLoaderWrapper.Wrap(_options.ExternalDataLoader);
			}
		}

		public GLTFSceneImporter(GLTFRoot rootNode, Stream gltfStream, ImportOptions options)
		{
			_gltfRoot = rootNode;

			if (gltfStream != null)
			{
				_gltfStream = new GLBStream { Stream = gltfStream, StartPosition = gltfStream.Position };
			}

			_options = options;
			if (_options.DataLoader == null)
			{
				_options.DataLoader = LegacyLoaderWrapper.Wrap(_options.ExternalDataLoader);
			}
		}

		/// <summary>
		/// Creates a GLTFSceneBuilder object which will be able to construct a scene based off a url
		/// </summary>
		/// <param name="gltfFileName">glTF file relative to data loader path</param>
		/// <param name="externalDataLoader">Loader to load external data references</param>
		/// <param name="asyncCoroutineHelper">Helper to load coroutines on a seperate thread</param>
		[Obsolete("Please switch to GLTFSceneImporter(string gltfFileName, ImportOptions options).  This constructor is deprecated and will be removed in a future release.")]
		public GLTFSceneImporter(string gltfFileName, ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper)
			: this(externalDataLoader, asyncCoroutineHelper)
		{
			_gltfFileName = gltfFileName;
		}

		[Obsolete("Please switch to GLTFSceneImporter(GLTFRoot rootNode, Stream gltfStream, ImportOptions options).  This constructor is deprecated and will be removed in a future release.")]
		public GLTFSceneImporter(GLTFRoot rootNode, ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper, Stream gltfStream = null)
			: this(externalDataLoader, asyncCoroutineHelper)
		{
			_gltfRoot = rootNode;

			if (gltfStream != null)
			{
				_gltfStream = new GLBStream { Stream = gltfStream, StartPosition = gltfStream.Position };
			}
		}

		[Obsolete("Only called by obsolete public constructors.  This will be removed when those obsolete constructors are removed.")]
		private GLTFSceneImporter(ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper)
		{
			_options = new ImportOptions
			{
				DataLoader = LegacyLoaderWrapper.Wrap(externalDataLoader),
				AsyncCoroutineHelper = asyncCoroutineHelper
			};
		}

		public void Dispose()
		{
			Cleanup();
		}

		/// <summary>
		/// Loads a glTF Scene into the LastLoadedScene field
		/// </summary>
		/// <param name="sceneIndex">The scene to load, If the index isn't specified, we use the default index in the file. Failing that we load index 0.</param>
		/// <param name="showSceneObj"></param>
		/// <param name="onLoadComplete">Callback function for when load is completed</param>
		/// <param name="cancellationToken">Cancellation token for loading</param>
		/// <returns></returns>
		public async Task LoadSceneAsync(int sceneIndex = -1, bool showSceneObj = true, Action<GameObject, ExceptionDispatchInfo> onLoadComplete = null, CancellationToken cancellationToken = default(CancellationToken), IProgress<ImportProgress> progress = null)
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

				if (_options.ThrowOnLowMemory)
				{
					_memoryChecker = new MemoryChecker();
				}

				this.progressStatus = new ImportProgress();
				this.progress = progress;

				Statistics = new ImportStatistics();
				progress?.Report(progressStatus);

				if (_gltfRoot == null)
				{
					await LoadJson(_gltfFileName);
					progressStatus.IsDownloaded = true;
				}

				cancellationToken.ThrowIfCancellationRequested();

				if (_gltfRoot.ExtensionsRequired != null && _gltfRoot.ExtensionsRequired.Contains("KHR_draco_mesh_compression"))
					throw new NotSupportedException("Draco compression (using KHR_draco_mesh_compression) isn't currently supported. Use glTFast instead for loading meshes that use Draco compression.");

				if (_assetCache == null)
				{
					_assetCache = new AssetCache(_gltfRoot);
				}

				await _LoadScene(sceneIndex, showSceneObj, cancellationToken);
			}
			catch (Exception ex)
			{
				Cleanup();

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

			if (progressStatus.NodeLoaded != progressStatus.NodeTotal) Debug.Log(LogType.Error, $"Nodes loaded ({progressStatus.NodeLoaded}) does not match node total in the scene ({progressStatus.NodeTotal})");
			if (progressStatus.TextureLoaded > progressStatus.TextureTotal) Debug.Log(LogType.Error, $"Textures loaded ({progressStatus.TextureLoaded}) is larger than texture total in the scene ({progressStatus.TextureTotal})");

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
		public async Task LoadNodeAsync(int nodeIndex, CancellationToken cancellationToken)
		{
			await SetupLoad(async () =>
			{
				CreatedObject = await GetNode(nodeIndex, cancellationToken);
				InitializeGltfTopLevelObject();
			});
		}

		/// <summary>
		/// Load a Material from the glTF by index
		/// </summary>
		/// <param name="materialIndex"></param>
		/// <returns></returns>
		public virtual async Task<Material> LoadMaterialAsync(int materialIndex)
		{
			await SetupLoad(async () =>
			{
				if (materialIndex < 0 || materialIndex >= _gltfRoot.Materials.Count)
				{
					throw new ArgumentException($"There is no material for index {materialIndex}");
				}

				if (_assetCache.MaterialCache[materialIndex] == null)
				{
					var def = _gltfRoot.Materials[materialIndex];
					await ConstructMaterialImageBuffers(def);
					await ConstructMaterial(def, materialIndex);
				}
			});
			return _assetCache.MaterialCache[materialIndex].UnityMaterialWithVertexColor;
		}

		/// <summary>
		/// Load a Mesh from the glTF by index
		/// </summary>
		/// <param name="meshIndex"></param>
		/// <returns></returns>
		public virtual async Task<Mesh> LoadMeshAsync(int meshIndex, CancellationToken cancellationToken)
		{
			await SetupLoad(async () =>
			{
				if (meshIndex < 0 || meshIndex >= _gltfRoot.Meshes.Count)
				{
					throw new ArgumentException($"There is no mesh for index {meshIndex}");
				}

				if (_assetCache.MeshCache[meshIndex] == null)
				{
					var def = _gltfRoot.Meshes[meshIndex];
					await ConstructMeshAttributes(def, new MeshId() { Id = meshIndex, Root = _gltfRoot });
					await ConstructMesh(def, meshIndex, cancellationToken);
				}
			});
			return _assetCache.MeshCache[meshIndex].LoadedMesh;
		}

		private async Task LoadJson(string jsonFilePath)
		{
#if !WINDOWS_UWP && !UNITY_WEBGL
			var dataLoader2 = _options.DataLoader as IDataLoader2;
			if (IsMultithreaded && dataLoader2 != null)
			{
				Thread loadThread = new Thread(() => _gltfStream.Stream = dataLoader2.LoadStream(jsonFilePath));
				loadThread.Priority = ThreadPriority.Highest;
				loadThread.Start();
				RunCoroutineSync(WaitUntilEnum(new WaitUntil(() => !loadThread.IsAlive)));
			}
			else
#endif
			{
				_gltfStream.Stream = await _options.DataLoader.LoadStreamAsync(jsonFilePath);
			}

			_gltfStream.StartPosition = 0;

#if !WINDOWS_UWP && !UNITY_WEBGL
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

		/// <summary>
		/// Initializes the top-level created node by adding an instantiated GLTF object component to it,
		/// so that it can cleanup after itself properly when destroyed
		/// </summary>
		private void InitializeGltfTopLevelObject()
		{
			InstantiatedGLTFObject instantiatedGltfObject = CreatedObject.AddComponent<InstantiatedGLTFObject>();
			instantiatedGltfObject.CachedData = new RefCountedCacheData
			(
				_assetCache.MaterialCache,
				_assetCache.MeshCache,
				_assetCache.TextureCache,
				_assetCache.ImageCache
			);
		}

		/// <summary>
		/// Creates a scene based off loaded JSON. Includes loading in binary and image data to construct the meshes required.
		/// </summary>
		/// <param name="sceneIndex">The bufferIndex of scene in gltf file to load</param>
		/// <returns></returns>
		protected async Task _LoadScene(int sceneIndex = -1, bool showSceneObj = true, CancellationToken cancellationToken = default(CancellationToken))
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

			GetGtlfContentTotals(scene);

			await ConstructScene(scene, showSceneObj, cancellationToken);

			if (SceneParent != null)
			{
				CreatedObject.transform.SetParent(SceneParent, false);
			}

			_lastLoadedScene = CreatedObject;
		}

		private void GetGtlfContentTotals(GLTFScene scene)
		{
			// Count Nodes
			Queue<NodeId> nodeQueue = new Queue<NodeId>();

			// Add scene nodes
			if (scene.Nodes != null)
			{
				for (int i = 0; i < scene.Nodes.Count; ++i)
				{
					nodeQueue.Enqueue(scene.Nodes[i]);
				}
			}

			// BFS of nodes
			while (nodeQueue.Count > 0)
			{
				var cur = nodeQueue.Dequeue();
				progressStatus.NodeTotal++;

				if (cur.Value.Children != null)
				{
					for (int i = 0; i < cur.Value.Children.Count; ++i)
					{
						nodeQueue.Enqueue(cur.Value.Children[i]);
					}
				}
			}

			// Total textures
			progressStatus.TextureTotal += _gltfRoot.Textures?.Count ?? 0;

			// Total buffers
			progressStatus.BuffersTotal += _gltfRoot.Buffers?.Count ?? 0;

			// Send report
			progress?.Report(progressStatus);
		}

		private async Task<BufferCacheData> GetBufferData(BufferId bufferId)
		{
			if (_assetCache.BufferCache[bufferId.Id] == null)
			{
				await ConstructBuffer(bufferId.Value, bufferId.Id);
			}

			return _assetCache.BufferCache[bufferId.Id];
		}

		private float GetLodCoverage(List<double> lodCoverageExtras, int lodIndex)
		{
			if (lodCoverageExtras != null && lodIndex < lodCoverageExtras.Count)
			{
				return (float)lodCoverageExtras[lodIndex];
			}
			else
			{
				return 1.0f / (lodIndex + 2);
			}
		}

		private async Task<GameObject> GetNode(int nodeId, CancellationToken cancellationToken)
		{
			try
			{
				if (_assetCache.NodeCache[nodeId] == null)
				{
					if (nodeId >= _gltfRoot.Nodes.Count)
					{
						throw new ArgumentException("nodeIndex is out of range");
					}

					var node = _gltfRoot.Nodes[nodeId];

					cancellationToken.ThrowIfCancellationRequested();
					if (!IsMultithreaded)
					{
						await ConstructBufferData(node, cancellationToken);
					}
					else
					{
						await Task.Run(() => ConstructBufferData(node, cancellationToken));
					}

					await ConstructNode(node, nodeId, cancellationToken);

					// HACK belongs in an extension, but we don't have Importer callbacks yet
					const string msft_LODExtName = MSFT_LODExtensionFactory.EXTENSION_NAME;
					if (_gltfRoot.ExtensionsUsed != null
					    && _gltfRoot.ExtensionsUsed.Contains(msft_LODExtName)
					    && node.Extensions != null
					    && node.Extensions.ContainsKey(msft_LODExtName))
					{
						var lodsExtension = node.Extensions[msft_LODExtName] as MSFT_LODExtension;
						if (lodsExtension != null && lodsExtension.NodeIds.Count > 0)
						{
							for (int i = 0; i < lodsExtension.NodeIds.Count; i++)
							{
								int lodNodeId = lodsExtension.NodeIds[i];
								await GetNode(lodNodeId, cancellationToken);
							}
						}
					}
				}

				return _assetCache.NodeCache[nodeId];
			}
			catch (Exception ex)
			{
				// If some failure occured during loading, remove the node

				if (_assetCache.NodeCache[nodeId] != null)
				{
					GameObject.DestroyImmediate(_assetCache.NodeCache[nodeId]);
					_assetCache.NodeCache[nodeId] = null;
				}

				if (ex is OutOfMemoryException)
				{
					Resources.UnloadUnusedAssets();
				}

				throw;
			}
		}

		protected virtual async Task ConstructNode(Node node, int nodeIndex, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

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
			_assetCache.NodeCache[nodeIndex] = nodeObj;

			if (node.Children != null)
			{
				foreach (var child in node.Children)
				{
					GameObject childObj = await GetNode(child.Id, cancellationToken);
					childObj.transform.SetParent(nodeObj.transform, false);
				}
			}

			if (node.Mesh != null)
			{
				var mesh = node.Mesh.Value;
				await ConstructMesh(mesh, node.Mesh.Id, cancellationToken);
				var unityMesh = _assetCache.MeshCache[node.Mesh.Id].LoadedMesh;
				var materials = node.Mesh.Value.Primitives.Select(p =>
					p.Material != null ?
					_assetCache.MaterialCache[p.Material.Id].UnityMaterialWithVertexColor :
					_defaultLoadedMaterial.UnityMaterialWithVertexColor
				).ToArray();

				var morphTargets = mesh.Primitives[0].Targets;
				var weights = node.Weights ?? mesh.Weights ??
					(morphTargets != null ? new List<double>(morphTargets.Select(mt => 0.0)) : null);
				if (node.Skin != null || weights != null)
				{
					var renderer = nodeObj.AddComponent<SkinnedMeshRenderer>();
					renderer.sharedMesh = unityMesh;
					renderer.sharedMaterials = materials;
					renderer.quality = SkinQuality.Auto;

					if (node.Skin != null)
						await SetupBones(node.Skin.Value, renderer, cancellationToken);

					// morph target weights
					if (weights != null)
					{
						for (int i = 0; i < weights.Count; ++i)
						{
							// GLTF weights are [0, 1] range; Unity weights must match the frame weight
							renderer.SetBlendShapeWeight(i, (float)(weights[i] * 1f));
						}
					}
				}
				else
				{
					var filter = nodeObj.AddComponent<MeshFilter>();
					filter.sharedMesh = unityMesh;
					var renderer = nodeObj.AddComponent<MeshRenderer>();
					renderer.sharedMaterials = materials;
				}

#if UNITY_PHYSICS
				switch (Collider)
				{
					case ColliderType.Box:
						var boxCollider = nodeObj.AddComponent<BoxCollider>();
						boxCollider.center = unityMesh.bounds.center;
						boxCollider.size = unityMesh.bounds.size;
						break;
					case ColliderType.Mesh:
						var meshCollider = nodeObj.AddComponent<MeshCollider>();
						meshCollider.sharedMesh = unityMesh;
						break;
					case ColliderType.MeshConvex:
						var meshConvexCollider = nodeObj.AddComponent<MeshCollider>();
						meshConvexCollider.sharedMesh = unityMesh;
						meshConvexCollider.convex = true;
						break;
				}
#endif
			}

			await ConstructLods(_gltfRoot, nodeObj, node, nodeIndex, cancellationToken);

			/* TODO: implement camera (probably a flag to disable for VR as well)
			if (camera != null)
			{
				GameObject cameraObj = camera.Value.Create();
				cameraObj.transform.parent = nodeObj.transform;
			}
			*/

			ConstructLights(nodeObj, node);

			nodeObj.SetActive(true);

			progressStatus.NodeLoaded++;
			progress?.Report(progressStatus);
		}

		private async Task ConstructBufferData(Node node, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

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
					await ConstructBufferData(child.Value, cancellationToken);
				}
			}
		}

		protected async Task ConstructBuffer(GLTFBuffer buffer, int bufferIndex)
		{
			if (buffer.Uri == null)
			{
				if (_assetCache.BufferCache[bufferIndex] != null) Debug.Log(LogType.Error, "_assetCache.BufferCache[bufferIndex] != null;");
				_assetCache.BufferCache[bufferIndex] = ConstructBufferFromGLB(bufferIndex);

				progressStatus.BuffersLoaded++;
				progress?.Report(progressStatus);
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
					bufferDataStream = await _options.DataLoader.LoadStreamAsync(buffer.Uri);
				}

				if (_assetCache.BufferCache[bufferIndex] != null) Debug.Log(LogType.Error, "_assetCache.BufferCache[bufferIndex] != null;");
				_assetCache.BufferCache[bufferIndex] = new BufferCacheData
				{
					Stream = bufferDataStream
				};

				progressStatus.BuffersLoaded++;
				progress?.Report(progressStatus);
			}
		}

		protected virtual async Task ConstructScene(GLTFScene scene, bool showSceneObj, CancellationToken cancellationToken)
		{
			var sceneObj = new GameObject(string.IsNullOrEmpty(scene.Name) ? ("Scene") : scene.Name);

			try
			{
				sceneObj.SetActive(showSceneObj);

				Transform[] nodeTransforms = new Transform[scene.Nodes.Count];
				for (int i = 0; i < scene.Nodes.Count; ++i)
				{
					NodeId node = scene.Nodes[i];
					GameObject nodeObj = await GetNode(node.Id, cancellationToken);
					nodeObj.transform.SetParent(sceneObj.transform, false);
					nodeTransforms[i] = nodeObj.transform;
				}

				if (_options.AnimationMethod != AnimationMethod.None)
				{
					if (_gltfRoot.Animations != null && _gltfRoot.Animations.Count > 0)
					{
#if UNITY_ANIMATION || !UNITY_2019_1_OR_NEWER
						// create the AnimationClip that will contain animation data
						var constructedClips = new List<AnimationClip>();
						for (int i = 0; i < _gltfRoot.Animations.Count; ++i)
						{
							AnimationClip clip = await ConstructClip(sceneObj.transform, i, cancellationToken);

							clip.wrapMode = WrapMode.Loop;
#if UNITY_EDITOR
							var settings = UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);
							settings.loopTime = _options.AnimationLoopTime;
							settings.loopBlend = _options.AnimationLoopPose;
							UnityEditor.AnimationUtility.SetAnimationClipSettings(clip, settings);
#endif
							constructedClips.Add(clip);
						}

						if (_options.AnimationMethod == AnimationMethod.Legacy)
						{
							Animation animation = sceneObj.AddComponent<Animation>();
							for (int i = 0; i < constructedClips.Count; i++)
							{
								var clip = constructedClips[i];
								animation.AddClip(clip, clip.name);
								if (i == 0)
								{
									animation.clip = clip;
								}
							}
						}
						else if (_options.AnimationMethod == AnimationMethod.Mecanim || _options.AnimationMethod == AnimationMethod.MecanimHumanoid)
						{
							Animator animator = sceneObj.AddComponent<Animator>();
#if UNITY_EDITOR
							// TODO there's no good way to construct an AnimatorController on import it seems, needs to be a SubAsset etc.
							var controller = new UnityEditor.Animations.AnimatorController();
							controller.name = "AnimatorController";
							controller.AddLayer("Base Layer");
							var baseLayer = controller.layers[0];
							for (int i = 0; i < constructedClips.Count; i++)
							{
								var state = baseLayer.stateMachine.AddState(constructedClips[i].name);
								state.motion = constructedClips[i];
							}
							animator.runtimeAnimatorController = controller;
#else
							Debug.Log(LogType.Warning, "Importing animations at runtime requires the Legacy AnimationMethod to be enabled, or custom handling of the resulting clips.");
#endif
						}
#else
						Debug.Log(LogType.Warning, "glTF scene contains animations but com.unity.modules.animation isn't installed. Install that module to import animations.");
#endif
						CreatedAnimationClips = constructedClips.ToArray();
					}
				}

				if (_options.AnimationMethod == AnimationMethod.MecanimHumanoid)
				{
					if (!sceneObj.GetComponent<Animator>())
						sceneObj.AddComponent<Animator>();
				}

				CreatedObject = sceneObj;
				InitializeGltfTopLevelObject();
			}
			catch (Exception ex)
			{
				// If some failure occured during loading, clean up the scene
				GameObject.DestroyImmediate(sceneObj);
				CreatedObject = null;

				if (ex is OutOfMemoryException)
				{
					Resources.UnloadUnusedAssets();
				}

				throw;
			}
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
			if (_assetCache != null)
			{
				_assetCache.Dispose();
				_assetCache = null;
			}
		}

		private async Task SetupLoad(Func<Task> callback)
		{
			try
			{
				lock (this)
				{
					if (_isRunning)
					{
						throw new GLTFLoadException("Cannot start a load while GLTFSceneImporter is already running");
					}

					_isRunning = true;
				}

				Statistics = new ImportStatistics();
				if (_options.ThrowOnLowMemory)
				{
					_memoryChecker = new MemoryChecker();
				}

				if (_gltfRoot == null)
				{
					await LoadJson(_gltfFileName);
				}

				if (_assetCache == null)
				{
					_assetCache = new AssetCache(_gltfRoot);
				}

				await callback();
			}
			catch
			{
				Cleanup();
				throw;
			}
			finally
			{
				lock (this)
				{
					_isRunning = false;
				}
			}
		}

		protected async Task YieldOnTimeoutAndThrowOnLowMemory()
		{
			if (_options.ThrowOnLowMemory)
			{
				_memoryChecker.ThrowIfOutOfMemory();
			}

			if (_options.AsyncCoroutineHelper != null)
			{
				await _options.AsyncCoroutineHelper.YieldOnTimeout();
			}
		}

		protected IEnumerator WaitUntilEnum(WaitUntil waitUntil)
		{
			yield return waitUntil;
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

	}
}
