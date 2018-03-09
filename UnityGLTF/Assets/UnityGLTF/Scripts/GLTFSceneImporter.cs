using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GLTF;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;

namespace UnityGLTF
{
	public class GLTFSceneImporter
	{
		public const string ClipName = "GLTFANIM";

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

		protected Dictionary<Node, Transform> transformDictionary = new Dictionary<Node, Transform>();

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
				var www = UnityWebRequest.Get(_gltfUrl);

				yield return www.SendWebRequest();

				if (www.responseCode >= 400 || www.responseCode == 0)
				{
					throw new WebRequestException(www);
				}

				byte[] gltfData = www.downloadHandler.data;
				_gltfStream.Stream = new MemoryStream(gltfData, 0, gltfData .Length, false, true);
			}
			else if (_loadType == LoadType.Stream)
			{
				// Do nothing, since the stream was passed in via the constructor.
			}
			else
			{
				throw new Exception("Invalid load type specified: " + _loadType);
			}

			_root = GLTFParser.ParseJson(_gltfStream.Stream, _gltfStream.StartPosition);

#if THROWEXCEPTIONS_NOT_WORKING//see https://jacksondunstan.com/articles/3718
			yield return ImportScene(sceneIndex, isMultithreaded);
#else
			IEnumerator importScene = ImportScene(sceneIndex, isMultithreaded);
			while (true)
			{
				if (importScene.MoveNext() == false)
				{
					break;
				}
				yield return importScene.Current;
			}
#endif
		}

		public int NumMaterials
		{
			get
			{
				return _root.Materials != null ? _root.Materials.Count : 0;
			}
		}

		public int NumMeshes
		{
			get
			{
				return _root.Meshes != null ? _root.Meshes.Count : 0;
			}
		}

		public int NumTextures
		{
			get
			{
				return _root.Textures != null ? _root.Textures.Count : 0;
			}
		}

		public int NumVertices
		{
			get
			{
				if (_root.Meshes != null)
				{
					int numVerices = 0;

					foreach (var mesh in _root.Meshes)
					{
						foreach (var primitive in mesh.Primitives)
						{
							numVerices += primitive.Attributes[SemanticProperties.POSITION].Value.Count;
						}
					}

					return numVerices;
				}
				return 0;
			}
		}

		public UnityEngine.Animation Animation
		{
			get
			{
				return LastLoadedScene.GetComponentInChildren<UnityEngine.Animation>();
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
#if THROWEXCEPTIONS_NOT_WORKING//see https://jacksondunstan.com/articles/3718
                            yield return LoadBuffer(_gltfDirectoryPath, buffer, i);
#else
							IEnumerator loadBuffer = LoadBuffer(_gltfDirectoryPath, buffer, i);
							while (true)
							{
								if (loadBuffer.MoveNext() == false)
								{
									break;
								}
								yield return loadBuffer.Current;
							}
#endif
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
#if THROWEXCEPTIONS_NOT_WORKING//see https://jacksondunstan.com/articles/3718
                        yield return LoadImage(_gltfDirectoryPath, image, i);
#else
						IEnumerator loadImage = LoadImage(_gltfDirectoryPath, image, i);
						while (true)
						{
							if (loadImage.MoveNext() == false)
							{
								break;
							}
							yield return loadImage.Current;
						}
#endif
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

			if ((_root.Animations != null) && (_root.Animations.Count > 0))
			{
				sceneObj.AddComponent<UnityEngine.Animation>();
				UnityEngine.Animation animation = sceneObj.GetComponent<UnityEngine.Animation>();
				Assert.IsNotNull(animation);

				for (int i = 0; i < _root.Animations.Count; i++)
				{
					AnimationClip clip = BuildAnimation(sceneObj, _root.Animations[i], true);
					clip.wrapMode = UnityEngine.WrapMode.Loop;
					animation.AddClip(clip, ClipName + i);
				}

				animation.Play(ClipName + "0");
			}

			_lastLoadedScene = sceneObj;
		}

		float[] GetAsFloats(AccessorId accessorId)
		{
			BufferCacheData bufferCacheData = _assetCache.BufferCache[accessorId.Value.BufferView.Value.Buffer.Id];
			AttributeAccessor attributeAccessor = new AttributeAccessor()
			{
				AccessorId = accessorId,
				Stream = bufferCacheData.Stream,
				Offset = bufferCacheData.ChunkOffset
			};

			NumericArray resultArray = attributeAccessor.AccessorContent;
			byte[] bufferViewCache;
			int offset = (int)GLTFHelpers.LoadBufferView(attributeAccessor, out bufferViewCache);
			attributeAccessor.AccessorId.Value.AsFloatArray(ref resultArray, bufferViewCache, offset);
			attributeAccessor.AccessorContent = resultArray;

			return resultArray.AsFloats;
		}

		GLTF.Math.Vector3[] GetAsVector3(AccessorId accessorId)
		{
			BufferCacheData bufferCacheData = _assetCache.BufferCache[accessorId.Value.BufferView.Value.Buffer.Id];
			AttributeAccessor attributeAccessor = new AttributeAccessor()
			{
				AccessorId = accessorId,
				Stream = bufferCacheData.Stream,
				Offset = bufferCacheData.ChunkOffset
			};

			NumericArray resultArray = attributeAccessor.AccessorContent;
			byte[] bufferViewCache;
			int offset = (int)GLTFHelpers.LoadBufferView(attributeAccessor, out bufferViewCache);
			attributeAccessor.AccessorId.Value.AsVector3Array(ref resultArray, bufferViewCache, offset);
			attributeAccessor.AccessorContent = resultArray;
						
			return resultArray.AsVec3s;
		}

		GLTF.Math.Vector4[] GetAsVector4(AccessorId accessorId)
		{
			BufferCacheData bufferCacheData = _assetCache.BufferCache[accessorId.Value.BufferView.Value.Buffer.Id];
			AttributeAccessor attributeAccessor = new AttributeAccessor()
			{
				AccessorId = accessorId,
				Stream = bufferCacheData.Stream,
				Offset = bufferCacheData.ChunkOffset
			};

			NumericArray resultArray = attributeAccessor.AccessorContent;
			byte[] bufferViewCache;
			int offset = (int)GLTFHelpers.LoadBufferView(attributeAccessor, out bufferViewCache);
			attributeAccessor.AccessorId.Value.AsVector4Array(ref resultArray, bufferViewCache, offset);
			attributeAccessor.AccessorContent = resultArray;

			return resultArray.AsVec4s;
		}

		GLTF.Math.Matrix4x4[] GetAsMatrix4x4(AccessorId accessorId)
		{
			BufferCacheData bufferCacheData = _assetCache.BufferCache[accessorId.Value.BufferView.Value.Buffer.Id];
			AttributeAccessor attributeAccessor = new AttributeAccessor()
			{
				AccessorId = accessorId,
				Stream = bufferCacheData.Stream,
				Offset = bufferCacheData.ChunkOffset
			};

			NumericArray resultArray = attributeAccessor.AccessorContent;
			byte[] bufferViewCache;
			int offset = (int)GLTFHelpers.LoadBufferView(attributeAccessor, out bufferViewCache);
			attributeAccessor.AccessorId.Value.AsMatrix4x4Array(ref resultArray, bufferViewCache, offset);
			attributeAccessor.AccessorContent = resultArray;

			return resultArray.AsMatrices;  //.ToUnityMatrix4x4()
		}
		
		protected virtual AnimationClip BuildAnimation(GameObject sceneObj, GLTF.Schema.Animation gltfAnimation, bool fixTangents)
		{
			Debug.Log("BuildAnimation " + sceneObj.transform.GetFullPath(null));

			AnimationClip clip = new AnimationClip();
			clip.legacy = true;

			List<float> allTimes = new List<float>();

			if (fixTangents)
			{
				foreach (AnimationChannel animationChannel in gltfAnimation.Channels)
				{
					AnimationSampler sampler = gltfAnimation.Samplers[animationChannel.SamplerId];
					float[] time = GetAsFloats(sampler.Input);

					for (int i = 0; i < time.Length; i++)
					{
						if (!allTimes.Contains(time[i]))
						{
							allTimes.Add(time[i]);
						}
					}
				}
			}

			allTimes.Sort();

			foreach (AnimationChannel animationChannel in gltfAnimation.Channels)
			{
				AnimationSampler sampler = gltfAnimation.Samplers[animationChannel.SamplerId];
				string targetName = transformDictionary[animationChannel.Target.Node.Value].GetFullPath(sceneObj.transform);

				//Debug.Log("animationChannel " + animationChannel.Target.Node.Value.Name + ", " + targetName +
				//	", " + animationChannel.Target.Path + ": "
				//	+ "Input " + sampler.Input.Value.Type + " " + sampler.Input.Value.ComponentType
				//	+ "; Output " + sampler.Output.Value.Type + " " + sampler.Output.Value.ComponentType);

				float[] time = GetAsFloats(sampler.Input);
				//StringBuilder str = new StringBuilder();

				//for (int i = 0; i < time.Length; i++)
				//{
				//	str.AppendFormat("{0}, ", time[i]);
				//}
				//Debug.Log(str.ToString());

				switch (animationChannel.Target.Path)
				{
					case GLTFAnimationChannelPath.translation:
						GLTF.Math.Vector3[] positions = GetAsVector3(sampler.Output);
						
						//localPosition 3
						AnimationCurve posX = new AnimationCurve();
						AnimationCurve posY = new AnimationCurve();
						AnimationCurve posZ = new AnimationCurve();

						for (int i = 0; i < time.Length; i++)
						{
							Vector3 pos = positions[i].ToUnityVector3Convert();
							posX.AddKey(time[i], pos.x);
							posY.AddKey(time[i], pos.y);
							posZ.AddKey(time[i], pos.z);
						}

						if (fixTangents)
						{
							posX = posX.fixTangents(allTimes);
							posY = posY.fixTangents(allTimes);
							posZ = posZ.fixTangents(allTimes);
						}

						clip.SetCurve(targetName, typeof(Transform), "m_LocalPosition.x", posX);
						clip.SetCurve(targetName, typeof(Transform), "m_LocalPosition.y", posY);
						clip.SetCurve(targetName, typeof(Transform), "m_LocalPosition.z", posZ);
						break;
					case GLTFAnimationChannelPath.scale:
						GLTF.Math.Vector3[] scales = GetAsVector3(sampler.Output);

						//localPosition 3
						AnimationCurve scaX = new AnimationCurve();
						AnimationCurve scaY = new AnimationCurve();
						AnimationCurve scaZ = new AnimationCurve();

						for (int i = 0; i < time.Length; i++)
						{
							scaX.AddKey(time[i], scales[i].X);
							scaY.AddKey(time[i], scales[i].Y);
							scaZ.AddKey(time[i], scales[i].Z);
						}

						if (fixTangents)
						{
							scaX = scaX.fixTangents(allTimes);
							scaY = scaY.fixTangents(allTimes);
							scaZ = scaZ.fixTangents(allTimes);
						}

						clip.SetCurve(targetName, typeof(Transform), "m_LocalScale.x", scaX);
						clip.SetCurve(targetName, typeof(Transform), "m_LocalScale.y", scaY);
						clip.SetCurve(targetName, typeof(Transform), "m_LocalScale.z", scaZ);
						//localScale 3
						break;
					case GLTFAnimationChannelPath.rotation:
						GLTF.Math.Vector4[] rotations = GetAsVector4(sampler.Output);

						//localPosition 3
						AnimationCurve rotX = new AnimationCurve();
						AnimationCurve rotY = new AnimationCurve();
						AnimationCurve rotZ = new AnimationCurve();
						AnimationCurve rotW = new AnimationCurve();

						for (int i = 0; i < time.Length; i++)
						{
							Quaternion rot = new GLTF.Math.Quaternion(rotations[i].X, rotations[i].Y, rotations[i].Z, rotations[i].W).ToUnityQuaternionConvert();
							rotX.AddKey(time[i], rot.x);
							rotY.AddKey(time[i], rot.y);
							rotZ.AddKey(time[i], rot.z);
							rotW.AddKey(time[i], rot.w);
						}

						if (fixTangents)
						{
							rotX = rotX.fixTangents(allTimes);
							rotY = rotY.fixTangents(allTimes);
							rotZ = rotZ.fixTangents(allTimes);
							rotW = rotW.fixTangents(allTimes);
						}

						clip.SetCurve(targetName, typeof(Transform), "m_LocalRotation.x", rotX);
						clip.SetCurve(targetName, typeof(Transform), "m_LocalRotation.y", rotY);
						clip.SetCurve(targetName, typeof(Transform), "m_LocalRotation.z", rotZ);
						clip.SetCurve(targetName, typeof(Transform), "m_LocalRotation.w", rotW);
						clip.EnsureQuaternionContinuity();
						break;
						//localRotation 4
						//break;
				}
			}

			return clip;
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
			var nodeObj = new GameObject((!string.IsNullOrEmpty(node.Name)) ? node.Name : "GLTFNode");

			Vector3 position;
			Quaternion rotation;
			Vector3 scale;
			node.GetUnityTRSProperties(out position, out rotation, out scale);
			nodeObj.transform.localPosition = position;
			nodeObj.transform.localRotation = rotation;
			nodeObj.transform.localScale = scale;

			// DONE: support for skin
			// TODO: Add support for morph targets
			if (node.Mesh != null)
			{
				CreateMeshObject(node.Mesh.Value, (node.Skin != null) ? node.Skin.Value : null, nodeObj.transform, node.Mesh.Id);
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

			transformDictionary[node] = nodeObj.transform;

			return nodeObj;
		}

		protected virtual void CreateMeshObject(GLTF.Schema.Mesh mesh, GLTF.Schema.Skin skin, Transform parent, int meshId)
		{
			if(_assetCache.MeshCache[meshId] == null)
			{
				_assetCache.MeshCache[meshId] = new MeshCacheData[mesh.Primitives.Count];
			}

			for(int i = 0; i < mesh.Primitives.Count; ++i)
			{
				var primitive = mesh.Primitives[i];
				var primitiveObj = CreateMeshPrimitive(primitive, skin, meshId, i);
				primitiveObj.transform.SetParent(parent, false);
				primitiveObj.SetActive(true);
			}
		}

		protected virtual GameObject CreateMeshPrimitive(MeshPrimitive primitive, GLTF.Schema.Skin skin, int meshID, int primitiveIndex)
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

				if (skin != null)
				{
					Assert.IsTrue(primitive.Attributes.ContainsKey(SemanticProperties.WEIGHT));
					Assert.IsTrue(primitive.Attributes.ContainsKey(SemanticProperties.JOINT));
					GLTF.Math.Vector4[] weights = meshAttributes[SemanticProperties.WEIGHT].AccessorContent.AsVec4s;
					GLTF.Math.Vector4[] bones = meshAttributes[SemanticProperties.JOINT].AccessorContent.AsVec4s;
					int len = mesh.vertices.Length;
					Assert.IsTrue(weights != null);
					Assert.IsTrue(bones != null);
					Assert.IsTrue(weights.Length == len);
					Assert.IsTrue(bones.Length == len);

					BoneWeight[] boneWeights = new BoneWeight[len];

					for (int i = 0; i < len; i++)
					{
						BoneWeight boneWeight = new BoneWeight();
						boneWeight.boneIndex0 = (int)bones[i].X;
						boneWeight.boneIndex1 = (int)bones[i].Y;
						boneWeight.boneIndex2 = (int)bones[i].Z;
						boneWeight.boneIndex3 = (int)bones[i].W;

						boneWeight.weight0 = weights[i].X;
						boneWeight.weight1 = weights[i].Y;
						boneWeight.weight2 = weights[i].Z;
						boneWeight.weight3 = weights[i].W;
						boneWeights[i] = boneWeight;
					}

					mesh.boneWeights = boneWeights;
				}

				_assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh = mesh;
			}

			meshFilter.sharedMesh = _assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh;

			var materialWrapper = CreateMaterial(
				primitive.Material != null ? primitive.Material.Value : DefaultMaterial,
				primitive.Material != null ? primitive.Material.Id : -1
			);

			Renderer meshRenderer = null;
			if (skin != null)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = primitiveObj.AddComponent<SkinnedMeshRenderer>();
				Transform[] bones = new Transform[skin.Joints.Count];
				GLTF.Math.Matrix4x4[] gltfBindPoses = GetAsMatrix4x4(skin.InverseBindMatrices);
				Matrix4x4[] bindPoses = new Matrix4x4[skin.Joints.Count];

				for (int i = 0; i < skin.Joints.Count; i++)
				{
					bones[i] = transformDictionary[skin.Joints[i].Value];
					bindPoses[i] = gltfBindPoses[i].ToUnityMatrix4x4Convert();
				}

				UnityEngine.Mesh mesh = _assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh;
				mesh.bindposes = bindPoses;
				skinnedMeshRenderer.bones = bones;
				skinnedMeshRenderer.sharedMesh = mesh;
				Bounds localBounds = skinnedMeshRenderer.localBounds;
				localBounds.size *= 2f;
				skinnedMeshRenderer.localBounds = localBounds;
				meshRenderer = skinnedMeshRenderer;
			}
			else
			{
				meshRenderer = primitiveObj.AddComponent<MeshRenderer>();
			}

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
						var www = UnityWebRequest.Get(Path.Combine(rootPath, uri));
						www.downloadHandler = new DownloadHandlerTexture();

						yield return www.SendWebRequest();

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
					else if (_loadType == LoadType.Stream)
					{
						var pathToLoad = Path.Combine(rootPath, uri);
						var file = File.OpenRead(pathToLoad);
						byte[] bufferData = new byte[file.Length];
						file.Read(bufferData, 0, (int) file.Length);
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
					var www = UnityWebRequest.Get(Path.Combine(sourceUri, uri));

					yield return www.SendWebRequest();

					bufferStream = new MemoryStream(www.downloadHandler.data, 0, www.downloadHandler.data.Length, false, true);
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
	}
}
