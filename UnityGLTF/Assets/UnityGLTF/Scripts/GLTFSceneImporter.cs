using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTF;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Loader;
using Matrix4x4 = GLTF.Math.Matrix4x4;
using Object = UnityEngine.Object;
using WrapMode = UnityEngine.WrapMode;

#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace UnityGLTF
{
	public struct MeshConstructionData
	{
		public MeshPrimitive Primitive { get; set; }
		public Dictionary<string, AttributeAccessor> MeshAttributes { get; set; }
	}

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

		protected struct GLBStream
		{
			public Stream Stream;
			public long StartPosition;
		}

		protected GameObject _lastLoadedScene;
		protected readonly GLTFMaterial DefaultMaterial = new GLTFMaterial();
		protected MaterialCacheData _defaultLoadedMaterial = null;

		protected string _gltfFileName;
		protected GLBStream _gltfStream;
		protected GLTFRoot _gltfRoot;
		protected AssetCache _assetCache;
		protected AsyncAction _asyncAction;
		protected ILoader _loader;
		private bool _isRunning = false;

		/// <summary>
		/// Creates a GLTFSceneBuilder object which will be able to construct a scene based off a url
		/// </summary>
		/// <param name="gltfFileName">glTF file relative to data loader path</param>
		/// <param name="externalDataLoader"></param>
		public GLTFSceneImporter(string gltfFileName, ILoader externalDataLoader) : this(externalDataLoader)
		{
			_gltfFileName = gltfFileName;
		}

		public GLTFSceneImporter(GLTFRoot rootNode, ILoader externalDataLoader, Stream glbStream = null) : this(externalDataLoader)
		{
			_gltfRoot = rootNode;
			if (glbStream != null)
			{
				_gltfStream = new GLBStream { Stream = glbStream, StartPosition = glbStream.Position };
			}
		}

		private GLTFSceneImporter(ILoader externalDataLoader)
		{
			_loader = externalDataLoader;
			_asyncAction = new AsyncAction();
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
		/// <param name="isMultithreaded">Whether all components should be loaded on a background thread</param>
		/// <param name="onLoadComplete">Callback function for when load is completed</param>
		/// <returns></returns>
		public IEnumerator LoadScene(int sceneIndex = -1, bool isMultithreaded = false, Action<GameObject> onLoadComplete = null)
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
					yield return LoadJson(_gltfFileName);
				}
				yield return _LoadScene(sceneIndex, isMultithreaded);

				Cleanup();
			}
			finally
			{
				lock (this)
				{
					_isRunning = false;
				}
			}

			if (onLoadComplete != null)
			{
				onLoadComplete(LastLoadedScene);
			}
		}

		/// <summary>
		/// Loads a node tree from a glTF file into the LastLoadedScene field
		/// </summary>
		/// <param name="nodeIndex">The node index to load from the glTF</param>
		/// <returns></returns>
		public IEnumerator LoadNode(int nodeIndex)
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
						throw new GLTFLoadException("Cannot call LoadNode while GLTFSceneImporter is already running");
					}

					_isRunning = true;
				}

				if (_assetCache == null)
				{
					InitializeAssetCache();
				}

				yield return _LoadNode(nodeIndex);
				CreatedObject = _assetCache.NodeCache[nodeIndex];
				InitializeGltfTopLevelObject();

				// todo: optimially the asset cache can be reused between nodes
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
		private IEnumerator ConstructBufferData(Node node)
		{
			MeshId mesh = node.Mesh;
			if (mesh != null)
			{
				if (mesh.Value.Primitives != null)
				{
					yield return ConstructMeshAttributes(mesh.Value, mesh);
				}
			}

			if (node.Children != null)
			{
				foreach (NodeId child in node.Children)
				{
					yield return ConstructBufferData(child.Value);
				}
			}
		}

		private IEnumerator ConstructMeshAttributes(GLTFMesh mesh, MeshId meshId)
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
					yield return ConstructMeshAttributes(primitive, meshIdIndex, i);
					if (primitive.Material != null)
					{
						yield return ConstructMaterialImageBuffers(primitive.Material.Value);
					}
				}
			}
		}

		protected IEnumerator ConstructImageBuffer(GLTFTexture texture, int textureIndex)
		{
			int sourceId = GetTextureSourceId(texture);
			if (_assetCache.ImageStreamCache[sourceId] == null)
			{
				GLTFImage image = _gltfRoot.Images[sourceId];

				// we only load the streams if not a base64 uri, meaning the data is in the uri
				if (image.Uri != null && !URIHelper.IsBase64Uri(image.Uri))
				{
					yield return _loader.LoadStream(image.Uri);
					_assetCache.ImageStreamCache[sourceId] = _loader.LoadedStream;
				}
			}

			_assetCache.TextureCache[textureIndex] = new TextureCacheData
			{
				TextureDefinition = texture
			};
		}

		private IEnumerator LoadJson(string jsonFilePath)
		{
			yield return _loader.LoadStream(jsonFilePath);

			_gltfStream.Stream = _loader.LoadedStream;
			_gltfStream.StartPosition = 0;
			_gltfRoot = GLTFParser.ParseJson(_gltfStream.Stream, _gltfStream.StartPosition);
		}

		private IEnumerator _LoadNode(int nodeIndex)
		{
			if (nodeIndex >= _gltfRoot.Nodes.Count)
			{
				throw new ArgumentException("nodeIndex is out of range");
			}

			Node nodeToLoad = _gltfRoot.Nodes[nodeIndex];
			yield return ConstructBufferData(nodeToLoad);
			yield return ConstructNode(nodeToLoad, nodeIndex);
		}

		protected void InitializeAssetCache()
		{
			_assetCache = new AssetCache(
				_gltfRoot.Images != null ? _gltfRoot.Images.Count : 0,
				_gltfRoot.Textures != null ? _gltfRoot.Textures.Count : 0,
				_gltfRoot.Materials != null ? _gltfRoot.Materials.Count : 0,
				_gltfRoot.Buffers != null ? _gltfRoot.Buffers.Count : 0,
				_gltfRoot.Meshes != null ? _gltfRoot.Meshes.Count : 0,
				_gltfRoot.Nodes != null ? _gltfRoot.Nodes.Count : 0,
				_gltfRoot.Animations != null ? _gltfRoot.Animations.Count : 0
				);
		}

		/// <summary>
		/// Creates a scene based off loaded JSON. Includes loading in binary and image data to construct the meshes required.
		/// </summary>
		/// <param name="sceneIndex">The bufferIndex of scene in gltf file to load</param>
		/// <param name="isMultithreaded">Whether to use a thread to do loading</param>
		/// <returns></returns>
		protected IEnumerator _LoadScene(int sceneIndex = -1, bool isMultithreaded = false)
		{
			GLTFScene scene;
			InitializeAssetCache(); // asset cache currently needs initialized every time due to cleanup logic

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

			if (_lastLoadedScene == null)
			{
				if (_gltfRoot.Buffers != null)
				{
					// todo add fuzzing to verify that buffers are before uri
					for (int i = 0; i < _gltfRoot.Buffers.Count; ++i)
					{
						GLTFBuffer buffer = _gltfRoot.Buffers[i];
						if (_assetCache.BufferCache[i] == null)
						{
							yield return ConstructBuffer(buffer, i);
						}
					}
				}

				if (_gltfRoot.Textures != null)
				{
					for (int i = 0; i < _gltfRoot.Textures.Count; ++i)
					{
						if (_assetCache.TextureCache[i] == null)
						{
							GLTFTexture texture = _gltfRoot.Textures[i];
							yield return ConstructImageBuffer(texture, i);
							yield return ConstructImage(texture.Source.Value, texture.Source.Id);
						}
					}
				}
				yield return ConstructAttributesForMeshes();
			}

			yield return ConstructScene(scene);

			if (SceneParent != null)
			{
				CreatedObject.transform.SetParent(SceneParent, false);
			}

			_lastLoadedScene = CreatedObject;
		}

		protected IEnumerator ConstructBuffer(GLTFBuffer buffer, int bufferIndex)
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
					yield return _loader.LoadStream(buffer.Uri);
					bufferDataStream = _loader.LoadedStream;
				}

				_assetCache.BufferCache[bufferIndex] = new BufferCacheData
				{
					Stream = bufferDataStream
				};
			}
		}

		protected IEnumerator ConstructImage(GLTFImage image, int imageCacheIndex, bool markGpuOnly = true)
		{
			if (_assetCache.ImageCache[imageCacheIndex] == null)
			{
				if (image.BufferView != null)
				{
					yield return ConstructImageFromGLB(image, imageCacheIndex);
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
						yield return ConstructUnityTexture(stream, markGpuOnly, image, imageCacheIndex);
					}
				}
			}
		}

		/// <summary>
		/// Loads texture from a stream. Is responsible for stream clean up
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="markGpuOnly">Non-readable textures are saved only on the GPU and take up half as much memory.</param>
		/// <param name="imageCacheIndex"></param>
		/// <returns></returns>
		protected virtual IEnumerator ConstructUnityTexture(Stream stream, bool markGpuOnly, GLTFImage image, int imageCacheIndex)
		{
			Texture2D texture = new Texture2D(0, 0);

			if (stream is MemoryStream)
			{
				using (MemoryStream memoryStream = stream as MemoryStream)
				{
					//	NOTE: the second parameter of LoadImage() marks non-readable, but we can't mark it until after we call Apply()
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

				//	NOTE: the second parameter of LoadImage() marks non-readable, but we can't mark it until after we call Apply()
				texture.LoadImage(buffer, false);
				yield return null;
			}

			// After we conduct the Apply(), then we can make the texture non-readable and never create a CPU copy
			texture.Apply(true, markGpuOnly);

			_assetCache.ImageCache[imageCacheIndex] = texture;
			yield return null;
		}

		protected virtual IEnumerator ConstructAttributesForMeshes()
		{
			for (int i = 0; i < _gltfRoot.Meshes.Count; ++i)
			{
				GLTFMesh mesh = _gltfRoot.Meshes[i];
				if (_assetCache.MeshCache[i] == null)
				{
					_assetCache.MeshCache[i] = new MeshCacheData[mesh.Primitives.Count];
				}

				for (int j = 0; j < mesh.Primitives.Count; ++j)
				{
					_assetCache.MeshCache[i][j] = new MeshCacheData();
					var primitive = mesh.Primitives[j];
					yield return ConstructMeshAttributes(primitive, i, j);
					if (primitive.Material != null)
					{
						yield return ConstructMaterialImageBuffers(primitive.Material.Value);
					}
				}
			}
		}

		protected virtual IEnumerator ConstructMeshAttributes(MeshPrimitive primitive, int meshID, int primitiveIndex)
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
						yield return ConstructBuffer(buffer, bufferId);
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
					AttributeAccessor indexBuilder = new AttributeAccessor
					{
						AccessorId = primitive.Indices,
						Stream = _assetCache.BufferCache[bufferId].Stream,
						Offset = _assetCache.BufferCache[bufferId].ChunkOffset
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
			for (var current = self; current != null; current = current.parent)
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

		AnimationClip ConstructClip(Transform root, Transform[] nodes, int animationId)
		{
			var animation = _gltfRoot.Animations[animationId];

			var animationCache = _assetCache.AnimationCache[animationId];
			if (animationCache == null)
			{
				animationCache = new AnimationCacheData(animation.Samplers.Count);
				_assetCache.AnimationCache[animationId] = animationCache;
			}
			else if (animationCache.LoadedAnimationClip != null)
				return animationCache.LoadedAnimationClip;

			// unpack accessors
			BuildAnimationSamplers(animation, animationId);

			// init clip
			var clip = new AnimationClip
			{
				name = animation.Name ?? String.Format("animation:{0}", animationId)
			};
			_assetCache.AnimationCache[animationId].LoadedAnimationClip = clip;

			// needed because Animator component is unavailable at runtime
			clip.legacy = true;

			foreach (var channel in animation.Channels)
			{
				var samplerCache = animationCache.Samplers[channel.Sampler.Id];
				var node = nodes[channel.Target.Node.Id];
				var relativePath = RelativePathFrom(node, root);
				AnimationCurve curveX = new AnimationCurve(),
					curveY = new AnimationCurve(),
					curveZ = new AnimationCurve(),
					curveW = new AnimationCurve();
				NumericArray input = samplerCache.Input.AccessorContent,
					output = samplerCache.Output.AccessorContent;

				switch (channel.Target.Path)
				{
					case GLTFAnimationChannelPath.translation:
						for (var i = 0; i < input.AsFloats.Length; ++i)
						{
							var time = input.AsFloats[i];
							Vector3 position = output.AsVec3s[i].ToUnityVector3Convert();
							curveX.AddKey(new Keyframe(time, position.x, 0, 0));
                            				curveY.AddKey(new Keyframe(time, position.y, 0, 0));
                            				curveZ.AddKey(new Keyframe(time, position.z, 0, 0));
						}

						clip.SetCurve(relativePath, typeof(Transform), "localPosition.x", curveX);
						clip.SetCurve(relativePath, typeof(Transform), "localPosition.y", curveY);
						clip.SetCurve(relativePath, typeof(Transform), "localPosition.z", curveZ);
						break;

					case GLTFAnimationChannelPath.rotation:
						var last = Quaternion.identity;
						for (int i = 0; i < input.AsFloats.Length; ++i)
						{
							var time = input.AsFloats[i];
							var rotation = output.AsVec4s[i];

							Quaternion rot = new GLTF.Math.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W).ToUnityQuaternionConvert();
							    if (i > 0)
							    {
								if (Quaternion.Dot(last, rot) < 0f)
								{
								    rot.x = -rot.x;
								    rot.y = -rot.y;
								    rot.z = -rot.z;
								    rot.w = -rot.w;
								}
							    }
							    last = rot;

							    curveX.AddKey(new Keyframe(time, rot.x, 0f, 0f));
							    curveY.AddKey(new Keyframe(time, rot.y, 0f, 0f));
							    curveZ.AddKey(new Keyframe(time, rot.z, 0f, 0f));
							    curveW.AddKey(new Keyframe(time, rot.w, 0f, 0f));
						}

						clip.SetCurve(relativePath, typeof(Transform), "localRotation.x", curveX);
						clip.SetCurve(relativePath, typeof(Transform), "localRotation.y", curveY);
						clip.SetCurve(relativePath, typeof(Transform), "localRotation.z", curveZ);
						clip.SetCurve(relativePath, typeof(Transform), "localRotation.w", curveW);
						break;

					case GLTFAnimationChannelPath.scale:
						for (var i = 0; i < input.AsFloats.Length; ++i)
						{
							var time = input.AsFloats[i];
							Vector3 scale = output.AsVec3s[i].ToUnityVector3Raw();
							    curveX.AddKey(new Keyframe(time, scale.x, 0, 0));
							    curveY.AddKey(new Keyframe(time, scale.y, 0, 0));
							    curveZ.AddKey(new Keyframe(time, scale.z, 0, 0));
						}

						clip.SetCurve(relativePath, typeof(Transform), "localScale.x", curveX);
						clip.SetCurve(relativePath, typeof(Transform), "localScale.y", curveY);
						clip.SetCurve(relativePath, typeof(Transform), "localScale.z", curveZ);
						break;

					case GLTFAnimationChannelPath.weights:
						var primitives = channel.Target.Node.Value.Mesh.Value.Primitives;
						var targetCount = primitives[0].Targets.Count;
						for (int primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
						{
							for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
							{
								// TODO: add support for blend shapes/morph targets
								//clip.SetCurve(primitiveObjPath, typeof(SkinnedMeshRenderer), "blendShape." + targetIndex, curves[targetIndex]);
							}
						}
						break;

					default:
						Debug.LogWarning("Cannot read GLTF animation path");
						break;
				} // switch target type
			} // foreach channel

			//clip.EnsureQuaternionContinuity();
			return clip;
		}
		#endregion

		protected virtual IEnumerator ConstructScene(GLTFScene scene)
		{
			var sceneObj = new GameObject(string.IsNullOrEmpty(scene.Name) ? ("GLTFScene") : scene.Name);

			Transform[] nodeTransforms = new Transform[scene.Nodes.Count];
			for (int i = 0; i < scene.Nodes.Count; ++i)
			{
				NodeId node = scene.Nodes[i];
				yield return ConstructNode(node.Value, node.Id);
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
					AnimationClip clip = ConstructClip(sceneObj.transform, _assetCache.NodeCache.Select(x => x.transform).ToArray(), i);

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

		protected virtual IEnumerator ConstructNode(Node node, int nodeIndex)
		{
			if (_assetCache.NodeCache[nodeIndex] != null)
			{
				yield break;
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
				yield return ConstructMesh(node.Mesh.Value, nodeObj.transform, node.Mesh.Id, node.Skin != null ? node.Skin.Value : null);
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
					yield return ConstructNode(child.Value, child.Id);
					GameObject childObj = _assetCache.NodeCache[child.Id];
					childObj.transform.SetParent(nodeObj.transform, false);
				}
			}

			nodeObj.SetActive(true);
			_assetCache.NodeCache[nodeIndex] = nodeObj;
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

		protected virtual IEnumerator SetupBones(Skin skin, MeshPrimitive primitive, SkinnedMeshRenderer renderer, GameObject primitiveObj, Mesh curMesh)
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
					yield return ConstructNode(_gltfRoot.Nodes[skin.Joints[i].Id], skin.Joints[i].Id);
				}
				bones[i] = _assetCache.NodeCache[skin.Joints[i].Id].transform;
				bindPoses[i] = gltfBindPoses[i].ToUnityMatrix4x4Convert();
			}

			renderer.rootBone = _assetCache.NodeCache[skin.Skeleton.Id].transform;
			curMesh.bindposes = bindPoses;
			renderer.bones = bones;

			yield return null;
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

		protected virtual IEnumerator ConstructMesh(GLTFMesh mesh, Transform parent, int meshId, Skin skin)
		{
			if (_assetCache.MeshCache[meshId] == null)
			{
				_assetCache.MeshCache[meshId] = new MeshCacheData[mesh.Primitives.Count];
			}

			for (int i = 0; i < mesh.Primitives.Count; ++i)
			{
				var primitive = mesh.Primitives[i];
				int materialIndex = primitive.Material != null ? primitive.Material.Id : -1;

				yield return ConstructMeshPrimitive(primitive, meshId, i, materialIndex);

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
						yield return SetupBones(skin, primitive, skinnedMeshRenderer, primitiveObj, curMesh);
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
			}
		}

		protected virtual IEnumerator ConstructMeshPrimitive(MeshPrimitive primitive, int meshID, int primitiveIndex, int materialIndex)
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

				yield return null;
				yield return ConstructUnityMesh(meshConstructionData, meshID, primitiveIndex);
			}

			bool shouldUseDefaultMaterial = primitive.Material == null;

			GLTFMaterial materialToLoad = shouldUseDefaultMaterial ? DefaultMaterial : primitive.Material.Value;
			if ((shouldUseDefaultMaterial && _defaultLoadedMaterial == null) ||
				(!shouldUseDefaultMaterial && _assetCache.MaterialCache[materialIndex] == null))
			{
				yield return ConstructMaterialTextures(materialToLoad);
				ConstructMaterial(materialToLoad, materialIndex);
			}
		}

		protected virtual IEnumerator ConstructMaterialImageBuffers(GLTFMaterial def)
		{
			if (def.PbrMetallicRoughness != null)
			{
				var pbr = def.PbrMetallicRoughness;

				if (pbr.BaseColorTexture != null)
				{
					var textureId = pbr.BaseColorTexture.Index;
					yield return ConstructImageBuffer(textureId.Value, textureId.Id);
				}
				if (pbr.MetallicRoughnessTexture != null)
				{
					var textureId = pbr.MetallicRoughnessTexture.Index;

					yield return ConstructImageBuffer(textureId.Value, textureId.Id);
				}
			}

			if (def.CommonConstant != null)
			{
				if (def.CommonConstant.LightmapTexture != null)
				{
					var textureId = def.CommonConstant.LightmapTexture.Index;

					yield return ConstructImageBuffer(textureId.Value, textureId.Id);
				}
			}

			if (def.NormalTexture != null)
			{
				var textureId = def.NormalTexture.Index;
				yield return ConstructImageBuffer(textureId.Value, textureId.Id);
			}

			if (def.OcclusionTexture != null)
			{
				var textureId = def.OcclusionTexture.Index;

				if (!(def.PbrMetallicRoughness != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id == textureId.Id))
				{
					yield return ConstructImageBuffer(textureId.Value, textureId.Id);
				}
			}

			if (def.EmissiveTexture != null)
			{
				var textureId = def.EmissiveTexture.Index;
				yield return ConstructImageBuffer(textureId.Value, textureId.Id);
			}
		}

		protected virtual IEnumerator ConstructMaterialTextures(GLTFMaterial def)
		{
			for (int i = 0; i < _assetCache.TextureCache.Length; ++i)
			{
				TextureCacheData textureCacheData = _assetCache.TextureCache[i];
				if (textureCacheData != null && textureCacheData.Texture == null)
				{
					yield return ConstructTexture(textureCacheData.TextureDefinition, i, true);
				}
			}
		}

		protected IEnumerator ConstructUnityMesh(MeshConstructionData meshConstructionData, int meshId, int primitiveIndex)
		{
			MeshPrimitive primitive = meshConstructionData.Primitive;
			var meshAttributes = meshConstructionData.MeshAttributes;
			var vertexCount = primitive.Attributes[SemanticProperties.POSITION].Value.Count;

			// todo optimize: There are multiple copies being performed to turn the buffer data into mesh data. Look into reducing them
			Mesh mesh = new Mesh
			{
#if UNITY_2017_3_OR_NEWER
				indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
#endif
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
					: null,

				boneWeights = meshAttributes.ContainsKey(SemanticProperties.Weight(0)) && meshAttributes.ContainsKey(SemanticProperties.Joint(0))
					? CreateBoneWeightArray(meshAttributes[SemanticProperties.Joint(0)].AccessorContent.AsVec4s.ToUnityVector4Raw(),
						meshAttributes[SemanticProperties.Weight(0)].AccessorContent.AsVec4s.ToUnityVector4Raw(), vertexCount)
					: null
			};

			_assetCache.MeshCache[meshId][primitiveIndex].LoadedMesh = mesh;

			yield return null;
		}

		protected virtual void ConstructMaterial(GLTFMaterial def, int materialIndex)
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

			mapper.AlphaMode = def.AlphaMode;
			mapper.DoubleSided = def.DoubleSided;

			var mrMapper = mapper as IMetalRoughUniformMap;
			if (def.PbrMetallicRoughness != null && mrMapper != null)
			{
				var pbr = def.PbrMetallicRoughness;

				mrMapper.BaseColorFactor = pbr.BaseColorFactor.ToUnityColorRaw();

				if (pbr.BaseColorTexture != null)
				{
					int textureId = pbr.BaseColorTexture.Index.Id;
					mrMapper.BaseColorTexture = _assetCache.TextureCache[textureId].Texture;
					mrMapper.BaseColorTexCoord = pbr.BaseColorTexture.TexCoord;

					//ApplyTextureTransform(pbr.BaseColorTexture, material, "_MainTex");
				}

				mrMapper.MetallicFactor = pbr.MetallicFactor;

				if (pbr.MetallicRoughnessTexture != null)
				{
					int textureId = pbr.MetallicRoughnessTexture.Index.Id;
					mrMapper.MetallicRoughnessTexture = _assetCache.TextureCache[textureId].Texture;
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
					int textureId = specGloss.DiffuseTexture.Index.Id;
					sgMapper.DiffuseTexture = _assetCache.TextureCache[textureId].Texture;
					sgMapper.DiffuseTexCoord = specGloss.DiffuseTexture.TexCoord;

					//ApplyTextureTransform(specGloss.DiffuseTexture, material, "_MainTex");
				}

				sgMapper.SpecularFactor = specGloss.SpecularFactor.ToUnityVector3Raw();
				sgMapper.GlossinessFactor = specGloss.GlossinessFactor;

				if (specGloss.SpecularGlossinessTexture != null)
				{
					int textureId = specGloss.SpecularGlossinessTexture.Index.Id;
					sgMapper.SpecularGlossinessTexture = _assetCache.TextureCache[textureId].Texture;
				}
			}

			if (def.NormalTexture != null)
			{
				int textureId = def.NormalTexture.Index.Id;
				mapper.NormalTexture = _assetCache.TextureCache[textureId].Texture;
				mapper.NormalTexCoord = def.NormalTexture.TexCoord;
				mapper.NormalTexScale = def.NormalTexture.Scale;
			}

			if (def.OcclusionTexture != null)
			{
				mapper.OcclusionTexStrength = def.OcclusionTexture.Strength;
				int textureId = def.OcclusionTexture.Index.Id;
				mapper.OcclusionTexture = _assetCache.TextureCache[textureId].Texture;
			}

			if (def.EmissiveTexture != null)
			{
				int textureId = def.EmissiveTexture.Index.Id;
				mapper.EmissiveTexture = _assetCache.TextureCache[textureId].Texture;
				mapper.EmissiveTexCoord = def.EmissiveTexture.TexCoord;
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
		/// <returns>The loaded unity texture</returns>
		public virtual IEnumerator LoadTexture(GLTFTexture texture, int textureIndex, bool markGpuOnly = true)
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

				if (_assetCache == null)
				{
					InitializeAssetCache();
				}
				yield return ConstructImageBuffer(texture, GetTextureSourceId(texture));
				yield return ConstructTexture(texture, textureIndex, markGpuOnly);
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

		protected virtual IEnumerator ConstructTexture(GLTFTexture texture, int textureIndex,
			bool markGpuOnly = true)
		{
			if (_assetCache.TextureCache[textureIndex].Texture == null)
			{
				int sourceId = GetTextureSourceId(texture);
				GLTFImage image = _gltfRoot.Images[sourceId];
				yield return ConstructImage(image, sourceId, markGpuOnly);

				var source = _assetCache.ImageCache[sourceId];
				var desiredFilterMode = FilterMode.Bilinear;
				var desiredWrapMode = TextureWrapMode.Repeat;

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
							Debug.LogWarning("Unsupported Sampler.MinFilter: " + sampler.MinFilter);
							break;
					}

					switch (sampler.WrapS)
					{
						case GLTF.Schema.WrapMode.ClampToEdge:
							desiredWrapMode = TextureWrapMode.Clamp;
							break;
						case GLTF.Schema.WrapMode.Repeat:
						default:
							desiredWrapMode = TextureWrapMode.Repeat;
							break;
					}
				}

				if (source.filterMode == desiredFilterMode && source.wrapMode == desiredWrapMode)
				{
					_assetCache.TextureCache[textureIndex].Texture = source;
				}
				else
				{
					var unityTexture = Object.Instantiate(source);
					unityTexture.filterMode = desiredFilterMode;
					unityTexture.wrapMode = desiredWrapMode;

					_assetCache.TextureCache[textureIndex].Texture = unityTexture;
				}

				yield return null;
			}
		}

		protected virtual IEnumerator ConstructImageFromGLB(GLTFImage image, int imageCacheIndex)
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

		protected virtual BufferCacheData ConstructBufferFromGLB(int bufferIndex)
		{
			GLTFParser.SeekToBinaryChunk(_gltfStream.Stream, bufferIndex, _gltfStream.StartPosition);  // sets stream to correct start position
			return new BufferCacheData
			{
				Stream = _gltfStream.Stream,
				ChunkOffset = _gltfStream.Stream.Position
			};
		}

		protected virtual void ApplyTextureTransform(TextureInfo def, Material mat, string texName)
		{
			IExtension extension;
			if (_gltfRoot.ExtensionsUsed != null &&
				_gltfRoot.ExtensionsUsed.Contains(ExtTextureTransformExtensionFactory.EXTENSION_NAME) &&
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
