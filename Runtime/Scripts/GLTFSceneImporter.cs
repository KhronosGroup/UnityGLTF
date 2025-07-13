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
using Unity.Collections;
using UnityEngine;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Loader;
using UnityGLTF.Plugins;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
#if !WINDOWS_UWP && !UNITY_WEBGL
using ThreadPriority = System.Threading.ThreadPriority;
#endif
using WrapMode = UnityEngine.WrapMode;

namespace UnityGLTF
{
	[Flags]
	public enum DeduplicateOptions
	{
		None = 0,
		Meshes = 1,
		Textures = 2,
	}

	public enum RuntimeTextureCompression
	{
		None,
		LowQuality ,
		HighQuality,
	}
	
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
		public AnimationMethod AnimationMethod = AnimationMethod.Legacy;
		public bool AnimationLoopTime = true;
		public bool AnimationLoopPose = false;
		public DeduplicateOptions DeduplicateResources = DeduplicateOptions.None;
		public bool SwapUVs = false;
		public GLTFImporterNormals ImportNormals = GLTFImporterNormals.Import;
		public GLTFImporterNormals ImportTangents = GLTFImporterNormals.Import;
		public bool ImportBlendShapeNames = true;
		public CameraImportOption CameraImport = CameraImportOption.ImportAndCameraDisabled;
		public RuntimeTextureCompression RuntimeTextureCompression = RuntimeTextureCompression.None;
		public BlendShapeFrameWeightSetting BlendShapeFrameWeight = new BlendShapeFrameWeightSetting(BlendShapeFrameWeightSetting.MultiplierOption.Multiplier1);

#if UNITY_EDITOR
		public GLTFImportContext ImportContext = new GLTFImportContext(null, GLTFSettings.GetOrCreateSettings());
#else
		public GLTFImportContext ImportContext = new GLTFImportContext(GLTFSettings.GetOrCreateSettings());
#endif

		[NonSerialized]
		public ILogger logger;
	}

	public enum CameraImportOption
	{
		None,
		ImportAndActive,
		ImportAndCameraDisabled
	}
	
	public enum AnimationMethod
	{
		None,
		Legacy,
		Mecanim,
		MecanimHumanoid,
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

		public GLTFRoot Root => _gltfRoot;

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

		private bool AnyAnimationTimeNotIncreasing;

		public TextureCacheData[] TextureCache => _assetCache.TextureCache;
		public Texture2D[] InvalidImageCache => _assetCache.InvalidImageCache;
		public MaterialCacheData[] MaterialCache => _assetCache.MaterialCache;
		public AnimationCacheData[] AnimationCache => _assetCache.AnimationCache;
		public GameObject[] NodeCache => _assetCache.NodeCache;
		public MeshCacheData[] MeshCache => _assetCache.MeshCache;

		/// <summary>
		/// Add here any objects, which are not GameObject, Materials, Textures and Animation Clips,
		/// that need to be cleaned up when the scene is destroyed
		/// </summary>
		public List<Object> GenericObjectReferences { get; private set; } = new List<Object>();

		private Dictionary<Stream, NativeArray<byte>> _nativeBuffers = new Dictionary<Stream, NativeArray<byte>>(); 
#if HAVE_MESHOPT_DECOMPRESS
		private List<NativeArray<byte>> meshOptNativeBuffers = new List<NativeArray<byte>>();
#endif
		/// <summary>
		/// Whether to keep a CPU-side copy of the mesh after upload to GPU (for example, in case normals/tangents need recalculation)
		/// </summary>
		public bool KeepCPUCopyOfMesh = true;

		/// <summary>
		/// Whether to keep a CPU-side copy of the texture after upload to GPU
		/// </summary>
		/// <remaks>
		/// This is necessary when a texture is used with different sampler states, as Unity doesn't allow setting
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

		public bool IsRunning => _isRunning;

		public bool LoadUnreferencedImagesAndMaterials = false;

		/// <summary>
		/// Statistics from the scene
		/// </summary>
		public ImportStatistics Statistics;

		protected GLTFImportContext Context => _options.ImportContext;

		protected ImportOptions _options;
		protected MemoryChecker _memoryChecker;

		protected GameObject _lastLoadedScene;
		protected readonly GLTFMaterial DefaultMaterial;
		internal MaterialCacheData _defaultLoadedMaterial = null;

		protected string _gltfFileName;
		protected GLBStream _gltfStream;
		protected GLTFRoot _gltfRoot;
		protected AssetCache _assetCache;
		protected bool _isRunning = false;
		
		protected ImportProgress progressStatus = default(ImportProgress);
		protected IProgress<ImportProgress> progress = null;

		private static ILogger Debug = UnityEngine.Debug.unityLogger;

		protected ColorSpace _activeColorSpace; 
		
		public GLTFSceneImporter(string gltfFileName, ImportOptions options) : this(options)
		{
			_gltfFileName = gltfFileName;

			VerifyDataLoader();
		}

		public GLTFSceneImporter(GLTFRoot rootNode, Stream gltfStream, ImportOptions options) : this(options)
		{
			_gltfRoot = rootNode;

			if (gltfStream != null)
			{
				_gltfStream = new GLBStream { Stream = gltfStream, StartPosition = gltfStream.Position };
			}

			VerifyDataLoader();
		}
		
		/// <summary>
		/// Loads a glTF file from a stream. It's recommended to load only gltf data without any external references. 
		/// </summary>
		/// <example>
		/// <code>
		/// var stream = new FileStream(filePath, FileMode.Open);
		///	var importOptions = new ImportOptions();
		///	var importer = new GLTFSceneImporter(stream, importOptions);
		///	await importer.LoadSceneAsync();
		///	stream.Dispose();
		/// </code>
		/// </example>
		public GLTFSceneImporter(Stream gltfStream, ImportOptions options) : this(options)
		{
			if (gltfStream != null)
			{
				_gltfStream = new GLBStream { Stream = gltfStream, StartPosition = gltfStream.Position };
			}
			GLTFParser.ParseJson(_gltfStream.Stream, out _gltfRoot, _gltfStream.StartPosition);

			VerifyDataLoader();
		}
		
		private GLTFSceneImporter(ImportOptions options)
		{
			if (options.ImportContext != null)
			{
				options.ImportContext.SceneImporter = this;
			}
			
			if (options.logger != null)
				Debug = options.logger;
			else
				Debug = UnityEngine.Debug.unityLogger;
			
			DefaultMaterial = new GLTFMaterial
			{
				Name = "Default",
				AlphaMode = AlphaMode.OPAQUE,
				DoubleSided = false,
				PbrMetallicRoughness = new PbrMetallicRoughness
				{
					MetallicFactor = 1, 
					RoughnessFactor = 1,
				}
			};
			
			_activeColorSpace = QualitySettings.activeColorSpace; 
			_options = options;
		}

		private NativeArray<byte> GetOrCreateNativeBuffer(Stream stream)
		{
			if (_nativeBuffers.TryGetValue(stream, out var buffer))
			{
				return buffer;
			}

			var buf = new byte[stream.Length];

			stream.Position = 0;
			long remainingSize = stream.Length;
			while (remainingSize != 0)
			{
				int sizeToLoad = (int)System.Math.Min(remainingSize, int.MaxValue);
				sizeToLoad = stream.Read(buf, (int)(stream.Length - remainingSize), sizeToLoad);
				remainingSize -= (uint)sizeToLoad;

				if (sizeToLoad == 0 && remainingSize > 0)
				{
					throw new Exception($"Unexpected end of stream while loading buffer view (File: {_gltfFileName})");
				}
			}			
			
			var newNativeBuffer = new NativeArray<byte>(buf, Allocator.Persistent);
			
			_nativeBuffers.Add(stream,newNativeBuffer);
			
			return newNativeBuffer;
		}

		private void VerifyDataLoader()
		{
			if (_options.DataLoader == null)
			{
				if (_options.ExternalDataLoader == null)
				{
					if (string.IsNullOrEmpty(_gltfFileName))
					{
						Debug.Log(LogType.Warning, "No filename specified for GLTFSceneImporter, external references will not be loaded");
						return;
					}
					_options.DataLoader = new UnityWebRequestLoader(URIHelper.GetDirectoryName(_gltfFileName));
					_gltfFileName = URIHelper.GetFileFromUri(new Uri(_gltfFileName));
				}
				else
					_options.DataLoader = LegacyLoaderWrapper.Wrap(_options.ExternalDataLoader);
			}
		}

		public void Dispose()
		{
			Cleanup();
			DisposeNativeBuffers();
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
						throw new GLTFLoadException($"Cannot call LoadScene while GLTFSceneImporter is already running (File: {_gltfFileName})");
					}

					_isRunning = true;
				}
				
				// TODO check where the right place is to call OnBeforeImport as early as possible
				foreach (var plugin in Context.Plugins)
				{
					plugin.OnBeforeImport();
				}

				if (_options.ThrowOnLowMemory)
				{
					_memoryChecker = new MemoryChecker();
				}

				this.progressStatus = new ImportProgress();
				this.progress = progress;

				Statistics = new ImportStatistics();
				progress?.Report(progressStatus);

#if UNITY_EDITOR
				// When loading from a buffer, this is not set; sanitizing that here
				// so we can log proper file names later on
				if (_gltfFileName == null)
				{
					var importSource = _options?.ImportContext?.AssetContext?.assetPath;
					if (importSource != null)
						_gltfFileName = importSource;
				}
#endif

				if (_gltfRoot == null)
				{
					foreach (var plugin in Context.Plugins)
						plugin.OnBeforeImportRoot();
					await LoadJson(_gltfFileName);
					progressStatus.IsDownloaded = true;
				}

				foreach (var plugin in Context.Plugins)
				{
					plugin.OnAfterImportRoot(_gltfRoot);
				}

				cancellationToken.ThrowIfCancellationRequested();

				if (_assetCache == null)
				{
					_assetCache = new AssetCache(_gltfRoot);
				}

#if HAVE_MESHOPT_DECOMPRESS
				if (Context.TryGetPlugin<MeshoptImportContext>(out _))
				{
					await MeshOptDecodeBuffer(_gltfRoot);
				}
#endif
				await _LoadScene(sceneIndex, showSceneObj, cancellationToken);

				// for Editor import, we also want to load unreferenced assets that wouldn't be loaded at runtime

				if (LoadUnreferencedImagesAndMaterials)
					await LoadUnreferencedAssetsAsync();

			}
			catch (Exception ex)
			{
				Cleanup();
				DisposeNativeBuffers();

				onLoadComplete?.Invoke(null, ExceptionDispatchInfo.Capture(ex));
				Debug.Log(LogType.Error, $"Error loading file: {_gltfFileName}" 
				                         + System.Environment.NewLine + "Message: " + ex.Message
				                         + System.Environment.NewLine + "StackTrace: " + ex.StackTrace);
				throw;
			}
			finally
			{
				lock (this)
				{
					_isRunning = false;
				}
			}
			_gltfStream.Stream.Close();
			DisposeNativeBuffers();
			
			if (this.progress != null)
				await Task.Yield();
			
			onLoadComplete?.Invoke(LastLoadedScene, null);
		}

		private async Task LoadUnreferencedAssetsAsync()
		{
			for (int i = 0; i < TextureCache.Length; i++)
			{
				if (TextureCache[i] == null)
				{
					await CreateNotReferencedTexture(i);
				}
			}

			// check which additional materials are in the root, but not yet in the MaterialCache
			for (var index = 0; index < MaterialCache.Length; index++)
			{
				if (_assetCache.MaterialCache[index] == null)
				{
					var def = _gltfRoot.Materials[index];
					await ConstructMaterialImageBuffers(def);
					await ConstructMaterial(def, index);
				}
			}
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
					throw new ArgumentException($"There is no material for index {materialIndex} (File: {_gltfFileName})");
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
					throw new ArgumentException($"There is no mesh for index {meshIndex} (File: {_gltfFileName})");
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
				await Task.Run(() => _gltfStream.Stream = dataLoader2.LoadStream(jsonFilePath));
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
				await Task.Run(() => GLTFParser.ParseJson(_gltfStream.Stream, out _gltfRoot, _gltfStream.StartPosition));
				if (_gltfRoot == null)
				{
					throw new GLTFLoadException($"Failed to parse glTF (File: {_gltfFileName})");
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
				_assetCache.ImageCache,
				_assetCache.AnimationCache,
				GenericObjectReferences.ToArray()
			);
		}

		private GameObject NoSceneRoot;

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
				// throw new GLTFLoadException("No default scene in gltf file.");
			}

			try
			{
				foreach (var plugin in Context.Plugins)
					plugin.OnBeforeImportScene(scene);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			GetGltfContentTotals(scene);

			await PreparePrimitiveAttributes();
			if (IsMultithreaded)
				await Task.Run(PrepareUnityMeshData, cancellationToken);
			else
				PrepareUnityMeshData();

			// Free up some Memory, Accessor contents are no longer needed
			FreeUpAccessorContents();

			if (_options.DeduplicateResources != DeduplicateOptions.None)
			{
				if (IsMultithreaded)
				{
					if (_options.DeduplicateResources.HasFlag(DeduplicateOptions.Meshes))
						await Task.Run(CheckForMeshDuplicates, cancellationToken);
					if (_options.DeduplicateResources.HasFlag(DeduplicateOptions.Textures))
						await Task.Run(CheckForDuplicateImages, cancellationToken);
				}
				else
				{
					if (_options.DeduplicateResources.HasFlag(DeduplicateOptions.Meshes))
						CheckForMeshDuplicates();
					if (_options.DeduplicateResources.HasFlag(DeduplicateOptions.Textures))
						CheckForDuplicateImages();
				}
			}
			
			await ConstructScene(scene, showSceneObj, cancellationToken);
			
			if (SceneParent != null && CreatedObject)
			{
				CreatedObject.transform.SetParent(SceneParent, false);
			}

			_lastLoadedScene = CreatedObject;

			try
			{
				foreach (var plugin in Context.Plugins)
					plugin.OnAfterImportScene(scene, sceneIndex, CreatedObject);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		private void GetGltfContentTotals(GLTFScene scene)
		{
			// Count Nodes
			Queue<NodeId> nodeQueue = new Queue<NodeId>();

			// Add scene nodes
			if (scene != null && scene.Nodes != null)
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

		public NativeArray<byte> GetBufferViewData(BufferView bufferView)
		{
			GetBufferData(bufferView.Buffer).Wait();
			GLTFHelpers.LoadBufferView(bufferView, _assetCache.BufferCache[bufferView.Buffer.Id].ChunkOffset, _assetCache.BufferCache[bufferView.Buffer.Id].bufferData, out var bufferViewCache);
			return bufferViewCache;
		}

		private async Task<BufferCacheData> GetBufferData(BufferId bufferId)
		{
			if (bufferId == null) return null;
			
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
						throw new ArgumentException($"nodeIndex is out of range (File: {_gltfFileName})");
					}

					var node = _gltfRoot.Nodes[nodeId];

					cancellationToken.ThrowIfCancellationRequested();
					await ConstructBufferData(node, cancellationToken);

					await ConstructNode(node, nodeId, cancellationToken);

					try
					{
						foreach (var plugin in Context.Plugins)
							plugin.OnAfterImportNode(node, nodeId, _assetCache.NodeCache[nodeId]);
					}
					catch (Exception ex)
					{
						Debug.LogException(ex);
					}

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
#if UNITY_2023_1_OR_NEWER
					await
#endif
					Resources.UnloadUnusedAssets();
				}

				throw;
			}
		}
		
		private async Task<(Vector3, Quaternion, Vector3)[]> GetInstancesTRS(Node node)
		{
			if (Context.TryGetPlugin<GPUInstancingImportContext>(out _) && node.Extensions != null &&
			    node.Extensions.TryGetValue(EXT_mesh_gpu_instancing_Factory.EXTENSION_NAME, out var ext))
			{
				AttributeAccessor positionsAttr = null;
				AttributeAccessor rotationAttr = null;
				AttributeAccessor scaleAttr = null;
				var extMeshGPUInstancing = ext as EXT_mesh_gpu_instancing;

				async Task<AttributeAccessor> GetAttrAccessorAndAccessorContent(AccessorId accessorId, bool isPosition = false)
				{
					var accessor = _gltfRoot.Accessors[accessorId.Id];
					
					var bufferId = accessor.BufferView.Value.Buffer;
					var bufferData = await GetBufferData(bufferId);
					
					var attrAccessor = new AttributeAccessor
					{
						AccessorId = accessorId,
						bufferData = bufferData.bufferData,
						Offset = (uint)bufferData.ChunkOffset
					};					
					GLTFHelpers.LoadBufferView(accessor.BufferView.Value, attrAccessor.Offset, attrAccessor.bufferData, out var bufferViewCache);
					NumericArray resultArray = attrAccessor.AccessorContent;
					switch (accessor.Type)
					{
						case GLTFAccessorAttributeType.VEC3:
							if (isPosition)
								attrAccessor.AccessorId.Value.AsVertexArray(ref resultArray, bufferViewCache);
							else
								attrAccessor.AccessorId.Value.AsFloat3Array(ref resultArray, bufferViewCache);
							break;
						case GLTFAccessorAttributeType.VEC4:
							attrAccessor.AccessorId.Value.AsFloat4Array(ref resultArray, bufferViewCache);
							break;
					}

					attrAccessor.AccessorContent = resultArray;
					return attrAccessor;
				}

				int instancesCount = 0;
				if (extMeshGPUInstancing.attributes.TryGetValue(EXT_mesh_gpu_instancing.ATTRIBUTE_TRANSLATION, out var positionAccessorId))
				{
					positionsAttr = await GetAttrAccessorAndAccessorContent(positionAccessorId, true);
					instancesCount = positionsAttr.AccessorContent.AsFloat3s.Length;
				}
				if (extMeshGPUInstancing.attributes.TryGetValue(EXT_mesh_gpu_instancing.ATTRIBUTE_ROTATION, out var rotationAccessorId))
				{
					rotationAttr = await GetAttrAccessorAndAccessorContent(rotationAccessorId);
					if (instancesCount != 0 && rotationAttr.AccessorContent.AsFloat4s.Length != instancesCount)
					{
						Debug.LogError("Rotation attribute count does not match position attribute count for instances!", this);
						return null;
					}
					else
						instancesCount = rotationAttr.AccessorContent.AsFloat4s.Length;
				}
				if (extMeshGPUInstancing.attributes.TryGetValue(EXT_mesh_gpu_instancing.ATTRIBUTE_SCALE, out var scaleAccessorId))
				{
					scaleAttr = await GetAttrAccessorAndAccessorContent(scaleAccessorId);
					if (instancesCount != 0 && scaleAttr.AccessorContent.AsFloat4s.Length != instancesCount)
					{
						Debug.LogError("Scale attribute count does not match position attribute count for instances!", this);
						return null;
					}
					else
						instancesCount = scaleAttr.AccessorContent.AsFloat3s.Length;
				}

				if (instancesCount > 0)
				{
					List<(Vector3, Quaternion, Vector3)> instancesTRS = new List<(Vector3, Quaternion, Vector3)>(instancesCount);
					for (int i = 0; i < instancesCount; i++)
					{
						instancesTRS.Add(( 
								positionsAttr != null ? positionsAttr.AccessorContent.AsFloat3s[i].ToUnityVector3Raw() : Vector3.zero,
								rotationAttr != null ? rotationAttr.AccessorContent.AsFloat4s[i].ToUnityQuaternionConvert() : Quaternion.identity,
								scaleAttr != null ? scaleAttr.AccessorContent.AsFloat3s[i].ToUnityVector3Raw() : Vector3.one
								));
					}
					return instancesTRS.ToArray();
				}
			}
			return null;
		}
		
		private bool ShouldBeVisible(Node node, GameObject nodeObj)
		{
			if (node.Extensions != null && node.Extensions.TryGetValue(KHR_node_visibility_Factory.EXTENSION_NAME, out var ext))
			{
				return (ext as KHR_node_visibility).visible;
			}
			else
				return true;
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
			_assetCache.NodeCache[nodeIndex] = nodeObj;
			nodeObj.transform.localPosition = position;
			nodeObj.transform.localRotation = rotation;
			nodeObj.transform.localScale = scale;



			async Task CreateNodeComponentsAndChilds(bool ignoreMesh = false, bool onlyMesh = false)
			{
				// If we're creating a really large node, we need it to not be visible in partial stages. So we hide it while we create it
				nodeObj.SetActive(false);

				if (!onlyMesh && node.Children != null)
				{
					foreach (var child in node.Children)
					{
						GameObject childObj = await GetNode(child.Id, cancellationToken);
						childObj.transform.SetParent(nodeObj.transform, false);
					}
				}

				if (!ignoreMesh && node.Mesh != null && node.Mesh.Value?.Primitives != null)
				{
					var mesh = node.Mesh.Value;
					await ConstructMesh(mesh, node.Mesh.Id, cancellationToken);
					var unityMesh = _assetCache.MeshCache[node.Mesh.Id].LoadedMesh;
					var materials = node.Mesh.Value.Primitives.Select(p =>
						p.Material != null
							? _assetCache.MaterialCache[p.Material.Id].UnityMaterialWithVertexColor
							: _defaultLoadedMaterial.UnityMaterialWithVertexColor
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
								renderer.SetBlendShapeWeight(i, (float)(weights[i] * _options.BlendShapeFrameWeight));
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
					if (!onlyMesh)
					{
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
					}
#endif
				}

				if (onlyMesh)
				{
					nodeObj.SetActive(ShouldBeVisible(node, nodeObj));
					return;
				}
				
				await ConstructLods(_gltfRoot, nodeObj, node, nodeIndex, cancellationToken);

				var hasLight = ConstructLights(nodeObj, node);
				var hasCamera = ConstructCamera(nodeObj, node);

				// Cameras and lights have a different forward axis in glTF vs. Unity.
				// Thus, when importing lights and cameras we have to flip them.
				// To ensure children are still oriented correctly, we need to add an inbetween node
				// That counters the transformation of the parent node.
				// This way, animations can still correctly apply – e.g. if a camera is animated,
				// the childs should move along. We can't just add the camera to an empty child and flip that.
				if ((hasLight || hasCamera) && nodeObj.transform.childCount > 0 && node.Children?.Count > 0)
				{
					var flipQuaternion = Quaternion.Inverse(SchemaExtensions.InvertDirection);
					
					// Special case for hierarchy simplification and roundtrips: if we have
					// - exactly one child
					// - that's flipped 180°
					// - and doesn't have any components
					// - and the node doesn't have any extensions
					// we can just remove that, it's likely an inbetween our own exporter has added.
					// Theoretically, there are more conditions (not checked here):
					// - it's not targeted by any animations
					// - it's not the target of any glTF pointer or index
					var firstNode = node.Children?.FirstOrDefault()?.Value;
					var firstChild = nodeObj.transform.GetChild(0);
					if (nodeObj.transform.childCount == 1 && node.Children?.Count == 1 &&
					    (firstNode.Extensions == null || !firstNode.Extensions.Any()) &&
					    firstChild.GetComponents<Component>().Length == 1 &&
					    Quaternion.Angle(firstChild.localRotation, flipQuaternion) < 0.1f)
					{
						firstChild.localRotation *= flipQuaternion;
						var childCount = firstChild.childCount;
						for (var i = 0; i < childCount; i++)
						{
							// Index 0 is correct here, after removing the first child, the next one is now the first and we want to keep the order.
							var child = firstChild.GetChild(0);
							child.SetParent(nodeObj.transform, true);
						}
						UnityEngine.Object.DestroyImmediate(firstChild.gameObject);
					}
					// Otherwise, we need to add an inbetween object
					else
					{
						var childCount = nodeObj.transform.childCount;
						var inbetween = new GameObject();
						inbetween.name = node.Name + "-flipped";
						// make sure this objects sits exactly where the nodeObj is
						inbetween.transform.SetParent(nodeObj.transform, false);
						inbetween.transform.SetParent(null, true);
						// move all children to the inbetween object
						for (int i = 0; i < childCount; i++)
						{
							// Index 0 is correct here, after removing the first child, the next one is now the first and we want to keep the order.
							nodeObj.transform.GetChild(0).SetParent(inbetween.transform, true);
						}
						inbetween.transform.SetParent(nodeObj.transform, true);
						inbetween.transform.localRotation = Quaternion.Inverse(SchemaExtensions.InvertDirection);
					}
				}
				nodeObj.SetActive( ShouldBeVisible(node, nodeObj));
			}
						
			var instancesTRS = await GetInstancesTRS(node);

			if (instancesTRS == null || instancesTRS.Length == 0)
			{
				await CreateNodeComponentsAndChilds();
			}
			else
			{
				var shouldBeVisible = ShouldBeVisible(node, nodeObj);
				await CreateNodeComponentsAndChilds(true);
				var instanceParentNode = new GameObject("Instances");
				instanceParentNode.transform.SetParent(nodeObj.transform, false);
				instanceParentNode.gameObject.SetActive(false);
				GameObject firstInstance = null;
				for (int i = 0; i < instancesTRS.Length; i++)
				{
					if (!firstInstance)
					{
						nodeObj = new GameObject(string.IsNullOrEmpty(node.Name) ? ("GLTFNode" + nodeIndex) : node.Name);
						nodeObj.transform.SetParent(instanceParentNode.transform, false);
						await CreateNodeComponentsAndChilds(false, true);
						firstInstance = nodeObj;
						
						var renderers = firstInstance.GetComponentsInChildren<Renderer>();
						foreach (var renderer in renderers)
							foreach (var sh in renderer.sharedMaterials)
								sh.enableInstancing = true;
						var skinRenderers = firstInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
						foreach (var renderer in skinRenderers)
							foreach (var sh in renderer.sharedMaterials)
								sh.enableInstancing = true;
					}
					else
					{
						nodeObj = GameObject.Instantiate(firstInstance);
						nodeObj.transform.SetParent(instanceParentNode.transform, false);
					}
					nodeObj.transform.localPosition = instancesTRS[i].Item1;
					nodeObj.transform.localRotation = instancesTRS[i].Item2;
					nodeObj.transform.localScale = instancesTRS[i].Item3;
					nodeObj.name = $"Instance {i.ToString()}";
				}
				instanceParentNode.gameObject.SetActive(shouldBeVisible);
			}
			
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
			if (_assetCache.BufferCache[bufferIndex] != null)
				return;
			
#if HAVE_MESHOPT_DECOMPRESS
			if (buffer.Extensions != null && buffer.Extensions.ContainsKey(EXT_meshopt_compression_Factory.EXTENSION_NAME))
			{
				if (_assetCache.BufferCache[bufferIndex] != null) Debug.Log(LogType.Error, $"_assetCache.BufferCache[bufferIndex] != null; (File: {_gltfFileName})");
				
				var bufferCacheDate = new BufferCacheData
				{
					bufferData = new NativeArray<byte>((int)buffer.ByteLength, Allocator.Persistent),
					ChunkOffset = 0
				};

				meshOptNativeBuffers.Add(bufferCacheDate.bufferData);
				_assetCache.BufferCache[bufferIndex] = bufferCacheDate;
				return;
			}
#else
			if (buffer.Extensions != null &&
			    buffer.Extensions.ContainsKey(EXT_meshopt_compression_Factory.EXTENSION_NAME))
			{
				//TODO: check for fallback URI or Buffer... ?
				throw new NotSupportedException($"Can't import model because it uses the EXT_meshopt_compression extension. Add the package \"com.unity.meshopt.decompress\" to your project to import this file. (File: {_gltfFileName})");
			}
#endif

			if (buffer.Uri == null)
			{
				if (_assetCache.BufferCache[bufferIndex] != null) Debug.Log(LogType.Error, $"Error: _assetCache.BufferCache[bufferIndex] != null. Please report a bug. (File: {_gltfFileName})");
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

				if (_assetCache.BufferCache[bufferIndex] != null) Debug.Log(LogType.Error, $"_assetCache.BufferCache[bufferIndex] != null; (File: {_gltfFileName})");
				_assetCache.BufferCache[bufferIndex] = new BufferCacheData
				{
					Stream = bufferDataStream,
					bufferData = GetOrCreateNativeBuffer(bufferDataStream)
				};

				progressStatus.BuffersLoaded++;
				progress?.Report(progressStatus);
			}
		}

		protected virtual async Task ConstructScene(GLTFScene scene, bool showSceneObj, CancellationToken cancellationToken)
		{
			if (scene == null)
			{
				return;
			}

			var sceneObj = new GameObject(string.IsNullOrEmpty(scene.Name) ? ("Scene") : scene.Name);

			try
			{
				sceneObj.SetActive(showSceneObj);

				if (scene.Nodes != null)
				{
					Transform[] nodeTransforms = new Transform[scene.Nodes.Count];
					for (int i = 0; i < scene.Nodes.Count; ++i)
					{
						NodeId node = scene.Nodes[i];
						GameObject nodeObj = await GetNode(node.Id, cancellationToken);
						nodeObj.transform.SetParent(sceneObj.transform, false);
						nodeTransforms[i] = nodeObj.transform;
					}
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
								clip.wrapMode = _options.AnimationLoopTime ? WrapMode.Loop : WrapMode.Default;
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
								var name = constructedClips[i].name;
								// can't be empty
								if (string.IsNullOrWhiteSpace(name)) name = "clip " + i;
								// can't contain ., / and \
								name = name.Replace(".", "_");
								name = name.Replace("/", "_");
								name = name.Replace("\\", "_");
								var state = baseLayer.stateMachine.AddState(name);
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
						if (AnyAnimationTimeNotIncreasing)
						{
							Debug.Log(LogType.Warning, $"Time of some subsequent animation keyframes is not increasing in {_gltfFileName} (glTF-Validator error ACCESSOR_ANIMATION_INPUT_NON_INCREASING)");
						}
						
						CreatedAnimationClips = constructedClips.ToArray();
					}
				}

				if (_options.AnimationMethod == AnimationMethod.MecanimHumanoid)
                {
                    var animator = sceneObj.GetComponent<Animator>();
                    if (!animator) animator = sceneObj.AddComponent<Animator>();

                    animator.applyRootMotion = true;
                }

				CreatedObject = sceneObj;
				InitializeGltfTopLevelObject();
			}
			catch (Exception ex)
			{
				// If some failure occured during loading, clean up the scene
				UnityEngine.Object.DestroyImmediate(sceneObj);
				CreatedObject = null;

				if (ex is OutOfMemoryException)
				{
#if UNITY_2023_1_OR_NEWER
					await
#endif
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
				ChunkOffset = (uint)_gltfStream.Stream.Position,
				bufferData = GetOrCreateNativeBuffer(_gltfStream.Stream),
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

		private void DisposeNativeBuffers()
		{
			foreach (var buffer in _nativeBuffers)
			{
				if (buffer.Value.IsCreated)
					buffer.Value.Dispose();
			}			
			_nativeBuffers.Clear();
			
#if HAVE_MESHOPT_DECOMPRESS
			foreach (var meshOptBuffer in meshOptNativeBuffers)
			{
				meshOptBuffer.Dispose();
			}
			meshOptNativeBuffers.Clear();
#endif
		}

		private async Task SetupLoad(Func<Task> callback)
		{
			try
			{
				lock (this)
				{
					if (_isRunning)
					{
						throw new GLTFLoadException($"Cannot start a load while GLTFSceneImporter is already running (File: {_gltfFileName})");
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
				_gltfStream.Stream.Close();
				DisposeNativeBuffers();
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
	}
}
