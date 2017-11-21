using GLTF;
using GLTF.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if WINDOWS_UWP
using System.Threading.Tasks;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Loader;
using Debug = UnityEngine.Debug;

namespace UnityGLTF
{
    public struct MeshConstructionData
    {
        public MeshPrimitive Primitive { get; set; }
        public Dictionary<string, AttributeAccessor> MeshAttributes { get; set; }
    }

    public class GLTFSceneImporter
    {
        public static class MaterialType
        {
            public const string PbrMetallicRoughness = "PbrMetallicRoughness";
            public const string PbrSpecularGlossiness = "PbrSpecularGlossiness";
            public const string CommonConstant = "CommonConstant";
            public const string CommonPhong = "CommonPhong";
            public const string CommonBlinn = "CommonBlinn";
            public const string CommonLambert = "CommonLambert";
        }
        
        // glTF matrix: column vectors, column-major storage, +Y up, -Z forward, +X right, right-handed
        // unity matrix: column vectors, column-major storage, +Y up, +Z forward, +X right, left-handed
        // multiply by a negative Z scale to convert handedness and flip forward direction
        public static readonly GLTF.Math.Vector3 CoordinateSpaceConversionScale = new GLTF.Math.Vector3(1, 1, -1);
        public static readonly GLTF.Math.Vector2 TextureSpaceConversionScale = new GLTF.Math.Vector2(1, -1);
        public static readonly GLTF.Math.Vector4 TangentSpaceConversionScale = new GLTF.Math.Vector4(1, 1, 1, -1);
        
        public int MaximumLod = 300;
        public Transform SceneParent { get; set; }
        public GameObject CreatedObject { get; private set; }

        /// <summary>
        /// Async Coroutine Helper is needed to run the coroutines in UWP
        /// </summary>
        public AsyncCoroutineHelper AsyncCoroutineHelper { get; set; }

        protected GameObject _lastLoadedScene;
        protected readonly Dictionary<string, Shader> _shaderCache = new Dictionary<string, Shader>();
        protected readonly GLTF.Schema.Material DefaultMaterial = new GLTF.Schema.Material();
        protected MaterialCacheData _defaultLoadedMaterial = null;

        protected string _gltfFileName;
        protected Stream _gltfStream;
        protected GLTFRoot _gltfRoot;
        protected AssetCache _assetCache;
        protected AsyncAction _asyncAction;
        protected byte[] _gltfData;
        protected ILoader _loader;
        private bool _isRunning = false;

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
        public virtual void SetShaderForMaterialType(string type, Shader shader)
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
            try
            {
                lock (this)
                {
                    if (_isRunning)
                    {
                        throw new Exception("Cannot call " + nameof(LoadScene) + " while " + nameof(GLTFSceneImporter) +
                                            " is already running");
                    }

                    _isRunning = true;
                }

                if (_gltfRoot == null)
                {
                    await LoadJson(_gltfFileName);
                }
                await ImportScene(sceneIndex, isMultithreaded);

                Cleanup();
            }
            finally
            {
                lock (this)
                {
                    _isRunning = false;
                }
            }
        }
#else
        public IEnumerator LoadScene(int sceneIndex = -1, bool isMultithreaded = false)
        {
            try
            {
                lock (this)
                {
                    if (_isRunning)
                    {
                        throw new Exception("Cannot call LoadScene while GLTFSceneImporter is already running");
                    }

                    _isRunning = true;
                }

                if (_gltfRoot == null)
                {
                    LoadJson(_gltfFileName);
                }
                yield return ImportScene(sceneIndex, isMultithreaded);

                Cleanup();
            }
            finally
            {
                lock (this)
                {
                    _isRunning = false;
                }
            }
        }
#endif

#if WINDOWS_UWP
        public async Task<GameObject> LoadNode(int nodeIndex)
#else
        public IEnumerator LoadNode(int nodeIndex)
#endif
        {
            if (_gltfRoot == null)
            {
                throw new InvalidOperationException("GLTF root must first be loaded and parsed");
            }

            try
            {
                lock (this)
                {
                    if (_isRunning)
                    {
                        throw new Exception("Cannot call LoadNode while GLTFSceneImporter is already running");
                    }

                    _isRunning = true;
                }

                if (_assetCache == null)
                {
                    InitializeAssetCache();
                }

#if WINDOWS_UWP
                await _LoadNode(nodeIndex);
#else
                yield return _LoadNode(nodeIndex);
#endif
                CreatedObject = _assetCache.NodeCache[nodeIndex];
                // todo: optimially the asset cache can be reused between nodes
                Cleanup();

#if WINDOWS_UWP
                return CreatedObject;
#endif
            }
            finally
            {
                lock (this)
                {
                    _isRunning = false;
                }
            }
        }

#if WINDOWS_UWP
        private async Task LoadBufferData(Node node)
#else
        private void LoadBufferData(Node node)
#endif
        {
            GLTF.Schema.MeshId mesh = node.Mesh;
            if (mesh != null)
            {
                if (mesh.Value.Primitives != null)
                {
#if WINDOWS_UWP
                    await ConstructMeshAttributes(mesh.Value, mesh);
#else
                    ConstructMeshAttributes(mesh.Value, mesh);
#endif
                }
            }

            if (node.Children != null)
            {
                foreach (NodeId child in node.Children)
                {
#if WINDOWS_UWP
                    await LoadBufferData(child.Value);
#else
                    LoadBufferData(child.Value);
#endif
                }
            }
        }

#if WINDOWS_UWP
        private async Task ConstructMeshAttributes(GLTF.Schema.Mesh mesh, MeshId meshId)
#else
        private void ConstructMeshAttributes(GLTF.Schema.Mesh mesh, MeshId meshId)
#endif
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
#if WINDOWS_UWP
                    await BuildMeshAttributes(primitive, meshIdIndex, i);
                    await LoadMaterialImageBuffers(primitive.Material.Value);
#else
                    BuildMeshAttributes(primitive, meshIdIndex, i);
                    LoadMaterialImageBuffers(primitive.Material.Value);
#endif
                }
            }
        }

#if WINDOWS_UWP
        protected async Task LoadImageBuffer(GLTF.Schema.Texture texture, int textureIndex)
#else
        protected void LoadImageBuffer(GLTF.Schema.Texture texture, int textureIndex)
#endif
        {
            int sourceId = GetTextureSourceId(texture);
            if (_assetCache.ImageStreamCache[sourceId] == null)
            {
                GLTF.Schema.Image image = _gltfRoot.Images[sourceId];

                // we only load the streams if not a base64 uri, meaning the data is in the uri
                if (image.Uri != null && !URIHelper.IsBase64Uri(image.Uri))
                {
#if WINDOWS_UWP
                    _assetCache.ImageStreamCache[sourceId] = await _loader.LoadStream(image.Uri);
#else
                    _assetCache.ImageStreamCache[sourceId] = _loader.LoadStream(image.Uri);
#endif
                }
            }
            
            _assetCache.TextureCache[textureIndex] = new TextureCacheData
            {
                TextureDefinition = texture
            };
        }
        
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
        private async Task _LoadNode(int nodeIndex)
#else
        private IEnumerator _LoadNode(int nodeIndex)
#endif
        {
            if(nodeIndex >= _gltfRoot.Nodes.Count)
            {
                throw new ArgumentException("nodeIndex is out of range");
            }

            Node nodeToLoad = _gltfRoot.Nodes[nodeIndex];
#if WINDOWS_UWP
            await Task.Run( async () => await LoadBufferData(nodeToLoad));
#else
            yield return _asyncAction.RunOnWorkerThread(() => LoadBufferData(nodeToLoad));
#endif

#if WINDOWS_UWP
            await AsyncCoroutineHelper.RunAsTask(CreateNode(nodeToLoad, nodeIndex));
#else
            yield return CreateNode(nodeToLoad, nodeIndex);
#endif
        }

        protected void InitializeAssetCache()
        {
            _assetCache = new AssetCache(
                _gltfRoot.Images != null ? _gltfRoot.Images.Count : 0,
                _gltfRoot.Textures != null ? _gltfRoot.Textures.Count : 0,
                _gltfRoot.Materials != null ? _gltfRoot.Materials.Count : 0,
                _gltfRoot.Buffers != null ? _gltfRoot.Buffers.Count : 0,
                _gltfRoot.Meshes != null ? _gltfRoot.Meshes.Count : 0,
                _gltfRoot.Nodes != null ? _gltfRoot.Nodes.Count : 0
                );
        }

        /// <summary>
        /// Creates a scene based off loaded JSON. Includes loading in binary and image data to construct the meshes required.
        /// </summary>
        /// <param name="sceneIndex">The bufferIndex of scene in gltf file to load</param>
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

                if (_gltfRoot.Textures != null)
                {
                    for(int i = 0; i < _gltfRoot.Textures.Count; ++i)
                    {
                        if (_assetCache.TextureCache[i] == null)
                        {
                            GLTF.Schema.Texture texture = _gltfRoot.Textures[i];
#if WINDOWS_UWP
                            await LoadImageBuffer(texture, i);
                            await AsyncCoroutineHelper.RunAsTask(LoadImage(texture.Source.Value, texture.Source.Id));
#else
                            LoadImageBuffer(texture, i);
                            yield return LoadImage(texture.Source.Value, texture.Source.Id);
#endif
                        }
                    }
                }
#if WINDOWS_UWP
                await BuildAttributesForMeshes();
#else
                yield return _asyncAction.RunOnWorkerThread(BuildAttributesForMeshes);
#endif
            }

#if WINDOWS_UWP
            await AsyncCoroutineHelper.RunAsTask(CreateScene(scene));
#else
            yield return CreateScene(scene);
#endif

            if (SceneParent != null)
            {
                CreatedObject.transform.SetParent(SceneParent, false);
            }

            _lastLoadedScene = CreatedObject;
        }

#if WINDOWS_UWP
        protected async Task<BufferCacheData> LoadBuffer(GLTF.Schema.Buffer buffer, int bufferIndex)
#else
        protected BufferCacheData LoadBuffer(GLTF.Schema.Buffer buffer, int bufferIndex)
#endif
        {
            if (buffer.Uri == null)
            {
                return LoadBufferFromGLB(bufferIndex);
            }
            else
            {
                Stream bufferDataStream = null;
                var uri = buffer.Uri;

                byte[] bufferData;
                URIHelper.TryParseBase64(uri, out bufferData);
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

        protected IEnumerator LoadImage(GLTF.Schema.Image image, int imageCacheIndex, bool markGpuOnly = true)
        {
	        if (_assetCache.ImageCache[imageCacheIndex] == null)
	        {
		        if (image.Uri == null)
		        {
			        yield return LoadImageFromGLB(image, imageCacheIndex);
		        }
		        else
		        {
			        string uri = image.Uri;

			        byte[] bufferData;
			        URIHelper.TryParseBase64(uri, out bufferData);
			        if (bufferData != null)
			        {
				        Texture2D loadedTexture = new Texture2D(0, 0);
				        loadedTexture.LoadImage(bufferData, true);
                        
			            _assetCache.ImageCache[imageCacheIndex] = loadedTexture;
			            yield return null;
			        }
			        else
			        {
				        Stream stream = _assetCache.ImageStreamCache[imageCacheIndex];
				        yield return LoadUnityTexture(stream, markGpuOnly, image, imageCacheIndex);
			        }
		        }
	        }
        }

        /// <summary>
        /// Loads texture from a stream. Is responsible for stream clean up
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="markGpuOnly">Non-readable textures are saved only on the GPU and take up half as much memory.</param>
        /// <returns></returns>
        protected virtual IEnumerator LoadUnityTexture(Stream stream, bool markGpuOnly, GLTF.Schema.Image image, int imageCacheIndex)
        {
            Texture2D texture = new Texture2D(0, 0);

            if (stream is MemoryStream)
            {
                using (MemoryStream memoryStream = stream as MemoryStream)
                {
                    //  NOTE: the second parameter of LoadImage() marks non-readable, but we can't mark it until after we call Apply()
                    texture.LoadImage(memoryStream.ToArray(), false);
                }

                yield return null;
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

                yield return null;

                //  NOTE: the second parameter of LoadImage() marks non-readable, but we can't mark it until after we call Apply()
                texture.LoadImage(buffer, false);
                yield return null;
            }

            // After we conduct the Apply(), then we can make the texture non-readable and never create a CPU copy
            texture.Apply(true, markGpuOnly);
            
            _assetCache.ImageCache[imageCacheIndex] = texture;
            yield return null;
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

                    AttributeAccessor attributeAccessor = new AttributeAccessor
                    {
                        AccessorId = attributePair.Value,
                        Stream = _assetCache.BufferCache[bufferId].Stream,
                        Offset = _assetCache.BufferCache[bufferId].ChunkOffset
                    };

                    attributeAccessors[attributePair.Key] = attributeAccessor;
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

                GLTFHelpers.BuildMeshAttributes(ref attributeAccessors, CoordinateSpaceConversionScale,
                    TextureSpaceConversionScale, TangentSpaceConversionScale);
                _assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes = attributeAccessors;
            }
        }
        
        protected virtual IEnumerator CreateScene(Scene scene)
        {
            var sceneObj = new GameObject(scene.Name ?? "GLTFScene");

            foreach (var node in scene.Nodes)
            {
                yield return CreateNode(node.Value, node.Id);
                GameObject nodeObj = _assetCache.NodeCache[node.Id];
                nodeObj.transform.SetParent(sceneObj.transform, false);
            }

            CreatedObject = sceneObj;
        }
        
        protected virtual IEnumerator CreateNode(Node node, int nodeIndex)
        {
            var nodeObj = new GameObject(node.Name ?? "GLTFNode");
            // If we're creating a really large node, we need it to not be visible in partial stages. So we hide it while we create it
            nodeObj.SetActive(false);

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
                yield return CreateMeshObject(node.Mesh.Value, nodeObj.transform, node.Mesh.Id);
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
                    yield return CreateNode(child.Value, child.Id);
                    GameObject childObj = _assetCache.NodeCache[child.Id];
                    childObj.transform.SetParent(nodeObj.transform, false);
                }
            }

            nodeObj.SetActive(true);
            _assetCache.NodeCache[nodeIndex] = nodeObj;
        }
        
        protected virtual IEnumerator CreateMeshObject(GLTF.Schema.Mesh mesh, Transform parent, int meshId)
        {
            if(_assetCache.MeshCache[meshId] == null)
            {
                _assetCache.MeshCache[meshId] = new MeshCacheData[mesh.Primitives.Count];
            }

            for(int i = 0; i < mesh.Primitives.Count; ++i)
            {
                var primitive = mesh.Primitives[i];
                int materialIndex = primitive.Material != null ? primitive.Material.Id : -1;

                yield return CreateMeshPrimitive(primitive, meshId, i, materialIndex);
                
                var primitiveObj = new GameObject("Primitive");
                var meshFilter = primitiveObj.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = _assetCache.MeshCache[meshId][i].LoadedMesh;
                var meshRenderer = primitiveObj.AddComponent<MeshRenderer>();

                MaterialCacheData materialCacheData =
                    materialIndex >= 0 ? _assetCache.MaterialCache[materialIndex] : _defaultLoadedMaterial;

                UnityEngine.Material material = materialCacheData.GetContents(primitive.Attributes.ContainsKey(SemanticProperties.Color(0)));

                meshRenderer.material = material;

                primitiveObj.transform.SetParent(parent, false);
                primitiveObj.SetActive(true);
            }
        }
        
        protected virtual IEnumerator CreateMeshPrimitive(MeshPrimitive primitive, int meshID, int primitiveIndex, int materialIndex)
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
                
                yield return CreateUnityMesh(meshConstructionData, meshID, primitiveIndex);
            }

            bool shouldUseDefaultMaterial = primitive.Material == null;

            GLTF.Schema.Material materialToLoad = shouldUseDefaultMaterial ? DefaultMaterial : primitive.Material.Value;
            if ((shouldUseDefaultMaterial && _defaultLoadedMaterial == null) ||
                (!shouldUseDefaultMaterial && _assetCache.MaterialCache[materialIndex] == null))
            {
                yield return LoadMaterialTextures(materialToLoad);
                CreateMaterial(materialToLoad, materialIndex);
            }
        }
        
#if WINDOWS_UWP
        protected virtual async Task LoadMaterialImageBuffers(GLTF.Schema.Material def)
#else
        protected virtual void LoadMaterialImageBuffers(GLTF.Schema.Material def)
#endif
        {
            if (def.PbrMetallicRoughness != null)
            {
                var pbr = def.PbrMetallicRoughness;

                if (pbr.BaseColorTexture != null)
                {
                    var textureId = pbr.BaseColorTexture.Index;
#if WINDOWS_UWP
                    await LoadImageBuffer(textureId.Value, textureId.Id);
#else
                    LoadImageBuffer(textureId.Value, textureId.Id);
#endif
                }
                if (pbr.MetallicRoughnessTexture != null)
                {
                    var textureId = pbr.MetallicRoughnessTexture.Index;

#if WINDOWS_UWP
                    await LoadImageBuffer(textureId.Value, textureId.Id);
#else
                    LoadImageBuffer(textureId.Value, textureId.Id);
#endif
				}
            }

            if (def.CommonConstant != null)
            {
                if (def.CommonConstant.LightmapTexture != null)
                {
                    var textureId = def.CommonConstant.LightmapTexture.Index;

#if WINDOWS_UWP
                    await LoadImageBuffer(textureId.Value, textureId.Id);
#else
                    LoadImageBuffer(textureId.Value, textureId.Id);
#endif
                }
            }

            if (def.NormalTexture != null)
            {
                var textureId = def.NormalTexture.Index;
#if WINDOWS_UWP
                await LoadImageBuffer(textureId.Value, textureId.Id);
#else
                LoadImageBuffer(textureId.Value, textureId.Id);
#endif
            }

            if (def.OcclusionTexture != null)
            {
                var textureId = def.OcclusionTexture.Index;

                if (!(def.PbrMetallicRoughness != null
                        && def.PbrMetallicRoughness.MetallicRoughnessTexture != null
                        && def.PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id == textureId.Id))
                {
#if WINDOWS_UWP
                    await LoadImageBuffer(textureId.Value, textureId.Id);
#else
                    LoadImageBuffer(textureId.Value, textureId.Id);
#endif
                }
            }

            if (def.EmissiveTexture != null)
            {
                var textureId = def.EmissiveTexture.Index;
#if WINDOWS_UWP
                await LoadImageBuffer(textureId.Value, textureId.Id);
#else
                LoadImageBuffer(textureId.Value, textureId.Id);
#endif
            }
        }
        
        protected virtual IEnumerator LoadMaterialTextures(GLTF.Schema.Material def)
        {
            for(int i = 0; i < _assetCache.TextureCache.Length; ++i)
            {
                TextureCacheData textureCacheData = _assetCache.TextureCache[i];
                if (textureCacheData != null && textureCacheData.Texture == null)
                {
                    yield return _CreateTexture(textureCacheData.TextureDefinition, i, true);
                }
            }
        }
        
        protected IEnumerator CreateUnityMesh(MeshConstructionData meshConstructionData, int meshId, int primitiveIndex)
        {
            MeshPrimitive primitive = meshConstructionData.Primitive;
            var meshAttributes = meshConstructionData.MeshAttributes;
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
                    ? meshAttributes[SemanticProperties.INDICES].AccessorContent.AsUInts.ToIntArrayRaw()
                    : MeshPrimitive.GenerateTriangles(vertexCount),

                tangents = primitive.Attributes.ContainsKey(SemanticProperties.TANGENT)
                    ? meshAttributes[SemanticProperties.TANGENT].AccessorContent.AsTangents.ToUnityVector4Raw()
                    : null
            };

            _assetCache.MeshCache[meshId][primitiveIndex].LoadedMesh = mesh;

            yield return null;
        }
        
        protected virtual void CreateMaterial(GLTF.Schema.Material def, int materialIndex)
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

                material.SetColor("_Color", pbr.BaseColorFactor.ToUnityColorRaw());

                if (pbr.BaseColorTexture != null)
                {
                    var textureId = pbr.BaseColorTexture.Index.Id;
                    material.SetTexture("_MainTex", _assetCache.TextureCache[textureId].Texture);
                }

                    material.SetFloat("_Metallic", (float)pbr.MetallicFactor);

                if (pbr.MetallicRoughnessTexture != null)
                {
                    var textureId = pbr.MetallicRoughnessTexture.Index.Id;
                    material.SetTexture("_MetallicRoughnessMap", _assetCache.TextureCache[textureId].Texture);
                }

                material.SetFloat("_Roughness", (float)pbr.RoughnessFactor);
            }

            if (def.CommonConstant != null)
            {
                material.SetColor("_AmbientFactor", def.CommonConstant.AmbientFactor.ToUnityColorRaw());

                if (def.CommonConstant.LightmapTexture != null)
                {
                    material.EnableKeyword("LIGHTMAP_ON");

                    var textureId = def.CommonConstant.LightmapTexture.Index.Id;
                    material.SetTexture("_LightMap", _assetCache.TextureCache[textureId].Texture);
                    material.SetInt("_LightUV", def.CommonConstant.LightmapTexture.TexCoord);
                }

                material.SetColor("_LightFactor", def.CommonConstant.LightmapFactor.ToUnityColorRaw());
            }

            if (def.NormalTexture != null)
            {
                var textureId = def.NormalTexture.Index.Id;
                material.SetTexture("_BumpMap", _assetCache.TextureCache[textureId].Texture);
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
                    material.SetTexture("_OcclusionMap", _assetCache.TextureCache[textureId].Texture);
                }
            }

            if (def.EmissiveTexture != null)
            {
                var textureId = def.EmissiveTexture.Index.Id;
                material.EnableKeyword("EMISSION_MAP_ON");
                material.SetTexture("_EmissionMap", _assetCache.TextureCache[textureId].Texture);
                material.SetInt("_EmissionUV", def.EmissiveTexture.TexCoord);
            }

            material.SetColor("_EmissionColor", def.EmissiveFactor.ToUnityColorRaw());
            
            MaterialCacheData materialWrapper = new MaterialCacheData
            {
                UnityMaterial = material,
                UnityMaterialWithVertexColor = new UnityEngine.Material(material),
                GLTFMaterial = def
            };
            
            materialWrapper.UnityMaterialWithVertexColor.EnableKeyword("VERTEX_COLOR_ON");

            if (materialIndex >= 0)
            {
                _assetCache.MaterialCache[materialIndex] = materialWrapper;
            }
            else
            {
                _defaultLoadedMaterial = materialWrapper;
            }
        }

        protected virtual int GetTextureSourceId(GLTF.Schema.Texture texture)
        {
            return texture.Source.Id;
        }

        /// <summary>
        /// Creates a texture from a glTF texture
        /// </summary>
        /// <param name="texture">The texture to load</param>
        /// <returns>The loaded unity texture</returns>
#if WINDOWS_UWP
        public virtual async Task<UnityEngine.Texture> CreateTexture(GLTF.Schema.Texture texture, int textureIndex, bool markGpuOnly = true)
#else
        public virtual IEnumerator CreateTexture(GLTF.Schema.Texture texture, int textureIndex, bool markGpuOnly = true)
#endif
        {
            try
            {
                lock (this)
                {
                    if (_isRunning)
                    {
                        throw new Exception("Cannot CreateTexture while GLTFSceneImporter is already running");
                    }

                    _isRunning = true;
                }

                if (_assetCache == null)
                {
                    InitializeAssetCache();
                }

#if WINDOWS_UWP
                await LoadImageBuffer(texture, GetTextureSourceId(texture));
                await AsyncCoroutineHelper.RunAsTask(_CreateTexture(texture, textureIndex, markGpuOnly));
                return _assetCache.TextureCache[textureIndex].Texture;
#else
                LoadImageBuffer(texture, GetTextureSourceId(texture));
                yield return _CreateTexture(texture, textureIndex, markGpuOnly);
#endif
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
        /// Gets texture that has been loaded from CreateTexture
        /// </summary>
        /// <param name="textureIndex">The texture to get</param>
        /// <returns>Created texture</returns>
        public virtual UnityEngine.Texture GetTexture(int textureIndex)
        {
            if (_assetCache == null)
            {
                throw new Exception("Asset cache needs initialized before calling GetTexture");
            }

            if (_assetCache.TextureCache[textureIndex] == null)
            {
                return null;
            }

            return _assetCache.TextureCache[textureIndex].Texture;
        }

        protected virtual IEnumerator _CreateTexture(GLTF.Schema.Texture texture, int textureIndex,
            bool markGpuOnly = true)
        {
            if (_assetCache.TextureCache[textureIndex].Texture == null)
            {
                int sourceId = GetTextureSourceId(texture);
                GLTF.Schema.Image image = _gltfRoot.Images[sourceId];
                yield return LoadImage(image, sourceId, markGpuOnly);

                var source = _assetCache.ImageCache[sourceId];
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
                    _assetCache.TextureCache[textureIndex].Texture = source;
                }
                else
                {
                    var unityTexture = UnityEngine.Object.Instantiate(source);
                    unityTexture.filterMode = desiredFilterMode;
                    unityTexture.wrapMode = desiredWrapMode;
                    
                    _assetCache.TextureCache[textureIndex].Texture = unityTexture;
                }

                yield return null;
            }
        }

        protected virtual IEnumerator LoadImageFromGLB(Image image, int imageCacheIndex)
        {
            var texture = new Texture2D(0, 0);
            var bufferView = image.BufferView.Value;
            var data = new byte[bufferView.ByteLength];

            var bufferContents = _assetCache.BufferCache[bufferView.Buffer.Id];
            bufferContents.Stream.Position = bufferView.ByteOffset + bufferContents.ChunkOffset;
            bufferContents.Stream.Read(data, 0, data.Length);
            texture.LoadImage(data);

            _assetCache.ImageCache[imageCacheIndex] = texture;
            yield return null;
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
