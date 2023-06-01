using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTF;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
#if UNITY_DRACO
using Draco;
#endif

namespace UnityGLTF
{
	public partial class GLTFSceneImporter
	{
		/// <summary>
		/// Triggers loading, converting, and constructing of a UnityEngine.Mesh, and stores it in the asset cache
		/// </summary>
		/// <param name="mesh">The definition of the mesh to generate</param>
		/// <param name="meshIndex">The index of the mesh to generate</param>
		/// <param name="cancellationToken"></param>
		/// <returns>A task that completes when the mesh is attached to the given GameObject</returns>
		protected virtual async Task ConstructMesh(GLTFMesh mesh, int meshIndex, CancellationToken cancellationToken)
		{
			async Task CreateMaterials(MeshPrimitive primitive)
			{
				bool shouldUseDefaultMaterial = primitive.Material == null;
				GLTFMaterial materialToLoad = shouldUseDefaultMaterial ? DefaultMaterial : primitive.Material.Value;
				if ((shouldUseDefaultMaterial && _defaultLoadedMaterial == null) ||
				    (!shouldUseDefaultMaterial && _assetCache.MaterialCache[primitive.Material.Id] == null))
				{
					await ConstructMaterial(materialToLoad, shouldUseDefaultMaterial ? -1 : primitive.Material.Id);
				}
			}

			cancellationToken.ThrowIfCancellationRequested();

			if (_assetCache.MeshCache[meshIndex] == null)
			{
				throw new Exception("Cannot generate mesh before ConstructMeshAttributes is called!");
			}
			else if (_assetCache.MeshCache[meshIndex].LoadedMesh)
			{
				return;
			}

			var firstPrim = mesh.Primitives.Count > 0 ?  mesh.Primitives[0] : null;

#if UNITY_DRACO
			var anyHadDraco = false;
			Mesh[] meshes = new Mesh[mesh.Primitives.Count];
			var meshDataArray = Mesh.AllocateWritableMeshData(mesh.Primitives.Count);
			var dracoDecodeResults = new DracoMeshLoader.DecodeResult[mesh.Primitives.Count];
			for (int i = 0; i < mesh.Primitives.Count; i++)
			{
				var primitive = mesh.Primitives[i];
				if (primitive.Extensions == null || !primitive.Extensions.ContainsKey("KHR_draco_mesh_compression"))
					continue;

				anyHadDraco = true;

				if (primitive.Extensions.TryGetValue("KHR_draco_mesh_compression", out var extension))
				{

					var dracoExtension = (KHR_draco_mesh_compression) extension;
					if (_assetCache.BufferCache[dracoExtension.bufferView.Value.Buffer.Id] == null)
						await ConstructBuffer(dracoExtension.bufferView.Value.Buffer.Value, dracoExtension.bufferView.Value.Buffer.Id);

					BufferCacheData bufferContents = _assetCache.BufferCache[dracoExtension.bufferView.Value.Buffer.Id];


					GLTFHelpers.LoadBufferView(dracoExtension.bufferView.Value, bufferContents.ChunkOffset,
						bufferContents.Stream, out byte[] bufferViewData);

					int weightsAttributeId = -1;
					if (dracoExtension.attributes.ContainsKey("WEIGHTS_0"))
						weightsAttributeId = (int) dracoExtension.attributes["WEIGHTS_0"];

					int jointsAttributeId = -1;
					if (dracoExtension.attributes.ContainsKey("JOINTS_0"))
						jointsAttributeId = (int) dracoExtension.attributes["JOINTS_0"];

					// TODO: check if normals and tangents are needed
					bool needsNormals = true;
					bool needsTangents = true;

					var draco = new DracoMeshLoader();

#if UNITY_EDITOR
					dracoDecodeResults[i] =  await draco.ConvertDracoMeshToUnity(meshDataArray[i], bufferViewData, needsNormals, needsTangents,
						weightsAttributeId, jointsAttributeId,  firstPrim.Targets != null);
#endif

					if (!dracoDecodeResults[i].success)
					{
						Debug.LogError("Error decoding draco mesh", this);
						meshDataArray.Dispose();
						return;
					}

					Statistics.VertexCount += meshDataArray[i].vertexCount;

					await CreateMaterials(primitive);
				}
			}

			if (anyHadDraco)
			{
				// Combine sub meshes
				await ConstructUnityMesh(mesh, dracoDecodeResults, meshDataArray, meshIndex, mesh.Name, true);

				return;
			}
#else
			if (mesh.Primitives.Any(p => p.Extensions != null && p.Extensions.ContainsKey("KHR_draco_mesh_compression")))
			{
				throw new NotSupportedException("Can't import model! Draco extension is needed. Please add com.atteneder.draco package to your project!");
			}
#endif
			cancellationToken.ThrowIfCancellationRequested();

			var totalVertCount = mesh.Primitives.Aggregate((uint)0, (sum, p) => sum + p.Attributes[SemanticProperties.POSITION].Value.Count);
			var vertOffset = 0;
			var meshCache = _assetCache.MeshCache[meshIndex];
			UnityMeshData unityData = new UnityMeshData()
			{
				Vertices = new Vector3[totalVertCount],
				Normals = firstPrim.Attributes.ContainsKey(SemanticProperties.NORMAL) ? new Vector3[totalVertCount] : null,
				Tangents = firstPrim.Attributes.ContainsKey(SemanticProperties.TANGENT) ? new Vector4[totalVertCount] : null,
				Uv1 = firstPrim.Attributes.ContainsKey(SemanticProperties.TEXCOORD_0) ? new Vector2[totalVertCount] : null,
				Uv2 = firstPrim.Attributes.ContainsKey(SemanticProperties.TEXCOORD_1) ? new Vector2[totalVertCount] : null,
				Uv3 = firstPrim.Attributes.ContainsKey(SemanticProperties.TEXCOORD_2) ? new Vector2[totalVertCount] : null,
				Uv4 = firstPrim.Attributes.ContainsKey(SemanticProperties.TEXCOORD_3) ? new Vector2[totalVertCount] : null,
				Colors = firstPrim.Attributes.ContainsKey(SemanticProperties.COLOR_0) ? new Color[totalVertCount] : null,
				BoneWeights = firstPrim.Attributes.ContainsKey(SemanticProperties.WEIGHTS_0) ? new BoneWeight[totalVertCount] : null,

				MorphTargetVertices = firstPrim.Targets != null && firstPrim.Targets[0].ContainsKey(SemanticProperties.POSITION) ?
					Allocate2dArray<Vector3>((uint)firstPrim.Targets.Count, totalVertCount) : null,
				MorphTargetNormals = firstPrim.Targets != null && firstPrim.Targets[0].ContainsKey(SemanticProperties.NORMAL) ?
					Allocate2dArray<Vector3>((uint)firstPrim.Targets.Count, totalVertCount) : null,
				MorphTargetTangents = firstPrim.Targets != null && firstPrim.Targets[0].ContainsKey(SemanticProperties.TANGENT) ?
					Allocate2dArray<Vector3>((uint)firstPrim.Targets.Count, totalVertCount) : null,

				Topology = new MeshTopology[mesh.Primitives.Count],
				Indices = new int[mesh.Primitives.Count][]
			};

			for (int i = 0; i < mesh.Primitives.Count; ++i)
			{
				var primitive = mesh.Primitives[i];
				var primCache = meshCache.Primitives[i];
				unityData.Topology[i] = GetTopology(primitive.Mode);

				if (IsMultithreaded)
				{
					await Task.Run(() => ConvertAttributeAccessorsToUnityTypes(primCache, unityData, vertOffset, i));
				}
				else
				{
					ConvertAttributeAccessorsToUnityTypes(primCache, unityData, vertOffset, i);
				}

				await CreateMaterials(primitive);

				cancellationToken.ThrowIfCancellationRequested();

				var vertCount = primitive.Attributes[SemanticProperties.POSITION].Value.Count;
				vertOffset += (int)vertCount;

				if (unityData.Topology[i] == MeshTopology.Triangles && primitive.Indices != null && primitive.Indices.Value != null)
				{
					Statistics.TriangleCount += primitive.Indices.Value.Count / 3;
				}
			}

			Statistics.VertexCount += vertOffset;
			await ConstructUnityMesh(unityData, meshIndex, mesh.Name);
		}

		/// <summary>
		/// Populate a UnityEngine.Mesh from Draco generated SubMeshes
		/// </summary>
		/// <returns></returns>
		protected async Task ConstructUnityMesh(GLTFMesh gltfMesh,DracoMeshLoader.DecodeResult[] decodeResults, Mesh.MeshDataArray meshes, int meshIndex, string meshName, bool requireTangents)
		{
			uint verticesLength = 0;
			for (int i = 0; i < meshes.Length; i++)
				verticesLength+= (uint)meshes[i].vertexCount;

			Mesh mesh = new Mesh
			{
				name = meshName,
#if UNITY_2017_3_OR_NEWER
				indexFormat = verticesLength > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
#endif
			};

			Mesh.ApplyAndDisposeWritableMeshData(meshes,mesh);
			foreach (var d in decodeResults)
			{
				if (d.boneWeightData != null)
				{
					d.boneWeightData.ApplyOnMesh(mesh);
					d.boneWeightData.Dispose();
				}
			}

			if (decodeResults[0].calculateNormals)
			{
				mesh.RecalculateNormals();
			}

			if (requireTangents)
			{
				mesh.RecalculateTangents();
			}

			await YieldOnTimeoutAndThrowOnLowMemory();

			verticesLength = (uint) mesh.vertexCount;

			var firstPrim = gltfMesh.Primitives[0];
			UnityMeshData unityMeshData = new UnityMeshData()
			{

				MorphTargetVertices = firstPrim.Targets != null && firstPrim.Targets[0].ContainsKey(SemanticProperties.POSITION) ?
					Allocate2dArray<Vector3>((uint)firstPrim.Targets.Count, verticesLength) : null,
				MorphTargetNormals = firstPrim.Targets != null && firstPrim.Targets[0].ContainsKey(SemanticProperties.NORMAL) ?
					Allocate2dArray<Vector3>((uint)firstPrim.Targets.Count, verticesLength) : null,
				MorphTargetTangents = firstPrim.Targets != null && firstPrim.Targets[0].ContainsKey(SemanticProperties.TANGENT) ?
					Allocate2dArray<Vector3>((uint)firstPrim.Targets.Count, verticesLength) : null,

				Topology = new MeshTopology[gltfMesh.Primitives.Count],
				Indices = new int[gltfMesh.Primitives.Count][]
			};

			int vertOffset = 0;
			var meshCache = _assetCache.MeshCache[meshIndex];

			unityMeshData.BoneWeights = mesh.boneWeights;
			var b = mesh.bindposes;

			for (int i = 0; i < gltfMesh.Primitives.Count; i++)
			{
				var primCache = meshCache.Primitives[i];
				if (IsMultithreaded)
				{
					await Task.Run(() => ConvertAttributeAccessorsToUnityTypes(primCache, unityMeshData, vertOffset, i));
				}
				else
				{
					ConvertAttributeAccessorsToUnityTypes(primCache, unityMeshData, vertOffset, i);
				}
				vertOffset += mesh.GetSubMesh(i).vertexCount;
			}

			mesh.RecalculateBounds();
			await YieldOnTimeoutAndThrowOnLowMemory();

			if (unityMeshData.MorphTargetVertices != null)
			{
				for (int i = 0; i < firstPrim.Targets.Count; i++)
				{
					var targetName = firstPrim.TargetNames != null ? firstPrim.TargetNames[i] : $"Morphtarget{i}";
					mesh.AddBlendShapeFrame(targetName, 1f,
						unityMeshData.MorphTargetVertices[i],
						unityMeshData.MorphTargetNormals != null ? unityMeshData.MorphTargetNormals[i] : null,
						unityMeshData.MorphTargetTangents != null ? unityMeshData.MorphTargetTangents[i] : null
					);
				}
			}
			await YieldOnTimeoutAndThrowOnLowMemory();

			if (mesh.normals != null && mesh.normals.Length > 0 && mesh.GetTopology(0) == MeshTopology.Triangles)
			{
				mesh.RecalculateNormals();
			}

			if (!KeepCPUCopyOfMesh)
			{
				mesh.UploadMeshData(true);
			}

			_assetCache.MeshCache[meshIndex].LoadedMesh = mesh;
		}

		/// <summary>
		/// Populate a UnityEngine.Mesh from preloaded and preprocessed buffer data
		/// </summary>
		/// <param name="meshConstructionData"></param>
		/// <param name="meshId"></param>
		/// <param name="primitiveIndex"></param>
		/// <param name="unityMeshData"></param>
		/// <returns></returns>
		protected async Task ConstructUnityMesh(UnityMeshData unityMeshData, int meshIndex, string meshName)
		{
			await YieldOnTimeoutAndThrowOnLowMemory();
			Mesh mesh = new Mesh
			{
				name = meshName,
#if UNITY_2017_3_OR_NEWER
				indexFormat = unityMeshData.Vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
#endif
			};

			mesh.vertices = unityMeshData.Vertices;
			await YieldOnTimeoutAndThrowOnLowMemory();
			mesh.normals = unityMeshData.Normals;
			await YieldOnTimeoutAndThrowOnLowMemory();
			mesh.tangents = unityMeshData.Tangents;
			await YieldOnTimeoutAndThrowOnLowMemory();
			mesh.uv = unityMeshData.Uv1;
			await YieldOnTimeoutAndThrowOnLowMemory();
			mesh.uv2 = unityMeshData.Uv2;
			await YieldOnTimeoutAndThrowOnLowMemory();
			mesh.uv3 = unityMeshData.Uv3;
			await YieldOnTimeoutAndThrowOnLowMemory();
			mesh.uv4 = unityMeshData.Uv4;
			await YieldOnTimeoutAndThrowOnLowMemory();
			mesh.colors = unityMeshData.Colors;
			await YieldOnTimeoutAndThrowOnLowMemory();
			mesh.boneWeights = unityMeshData.BoneWeights;
			await YieldOnTimeoutAndThrowOnLowMemory();

			mesh.subMeshCount = unityMeshData.Indices.Length;
			uint baseVertex = 0;
			for (int i = 0; i < unityMeshData.Indices.Length; i++)
			{
				mesh.SetIndices(unityMeshData.Indices[i], unityMeshData.Topology[i], i, false, (int)baseVertex);
				baseVertex += _assetCache.MeshCache[meshIndex].Primitives[i].Attributes[SemanticProperties.POSITION].AccessorId.Value.Count;
			}
			mesh.RecalculateBounds();
			await YieldOnTimeoutAndThrowOnLowMemory();

			if (unityMeshData.MorphTargetVertices != null)
			{
				var firstPrim = _gltfRoot.Meshes[meshIndex].Primitives[0];
				for (int i = 0; i < firstPrim.Targets.Count; i++)
				{
					var targetName = firstPrim.TargetNames != null ? firstPrim.TargetNames[i] : $"Morphtarget{i}";
					mesh.AddBlendShapeFrame(targetName, 1f,
						unityMeshData.MorphTargetVertices[i],
						unityMeshData.MorphTargetNormals != null ? unityMeshData.MorphTargetNormals[i] : null,
						unityMeshData.MorphTargetTangents != null ? unityMeshData.MorphTargetTangents[i] : null
					);
				}
			}
			await YieldOnTimeoutAndThrowOnLowMemory();

			if (unityMeshData.Normals == null && unityMeshData.Topology[0] == MeshTopology.Triangles)
			{
				mesh.RecalculateNormals();
			}

			if (!KeepCPUCopyOfMesh)
			{
				mesh.UploadMeshData(true);
			}

			_assetCache.MeshCache[meshIndex].LoadedMesh = mesh;
		}

		protected virtual async Task ConstructMeshTargets(MeshPrimitive primitive, int meshIndex, int primitiveIndex)
		{
			var newTargets = new List<Dictionary<string, AttributeAccessor>>(primitive.Targets.Count);
			_assetCache.MeshCache[meshIndex].Primitives[primitiveIndex].Targets = newTargets;

			for (int i = 0; i < primitive.Targets.Count; i++)
			{
				var target = primitive.Targets[i];
				newTargets.Add(new Dictionary<string, AttributeAccessor>());

				NumericArray[] sparseNormals = null;
				NumericArray[] sparsePositions = null;
				NumericArray[] sparseTangents = null;

				const string NormalKey = "NORMALS";
				const string PositionKey = "POSITIONS";
				const string TangentKey = "TANGENTS";

				// normals, positions, tangents
				foreach (var targetAttribute in target)
				{
					BufferId bufferIdPair = null;
					if (targetAttribute.Value.Value.Sparse == null)
					{
						bufferIdPair = targetAttribute.Value.Value.BufferView.Value.Buffer;
					}
					else
					{
						// When using Draco, it's possible the BufferView is null
						if (primitive.Attributes[targetAttribute.Key].Value.BufferView == null)
						{
							continue;
						}
						bufferIdPair = primitive.Attributes[targetAttribute.Key].Value.BufferView.Value.Buffer;
						targetAttribute.Value.Value.BufferView = primitive.Attributes[targetAttribute.Key].Value.BufferView;
					}
					GLTFBuffer buffer = bufferIdPair.Value;
					int bufferID = bufferIdPair.Id;

					if (_assetCache.BufferCache[bufferID] == null)
					{
						await ConstructBuffer(buffer, bufferID);
					}

					newTargets[i][targetAttribute.Key] = new AttributeAccessor
					{
						AccessorId = targetAttribute.Value,
						Stream = _assetCache.BufferCache[bufferID].Stream,
						Offset = (uint)_assetCache.BufferCache[bufferID].ChunkOffset
					};

					// if this buffer isn't sparse, we're done here
					if (targetAttribute.Value.Value.Sparse == null) continue;

					// Values
					var bufferId = targetAttribute.Value.Value.Sparse.Values.BufferView.Value.Buffer;
					var bufferData = await GetBufferData(bufferId);
					AttributeAccessor sparseValues = new AttributeAccessor
					{
						AccessorId = targetAttribute.Value,
						Stream = bufferData.Stream,
						Offset = (uint)bufferData.ChunkOffset
					};
					uint offset1 = GLTFHelpers.LoadBufferView(sparseValues.AccessorId.Value.Sparse.Values.BufferView.Value, sparseValues.Offset, sparseValues.Stream, out byte[] bufferViewCache1);

					// Indices
					bufferId = targetAttribute.Value.Value.Sparse.Indices.BufferView.Value.Buffer;
					bufferData = await GetBufferData(bufferId);
					AttributeAccessor sparseIndices = new AttributeAccessor
					{
						AccessorId = targetAttribute.Value,
						Stream = bufferData.Stream,
						Offset = (uint)bufferData.ChunkOffset
					};
					uint offset2 = GLTFHelpers.LoadBufferView(sparseIndices.AccessorId.Value.Sparse.Indices.BufferView.Value, sparseIndices.Offset, sparseIndices.Stream, out byte[] bufferViewCache2);

					switch (targetAttribute.Key)
					{
						case NormalKey:
							sparseNormals = new NumericArray[2];
							Accessor.AsSparseVector3Array(targetAttribute.Value.Value, ref sparseNormals[0], bufferViewCache1, offset1);
							Accessor.AsSparseUIntArray(targetAttribute.Value.Value, ref sparseNormals[1], bufferViewCache2, offset2);
							break;
						case PositionKey:
							sparsePositions = new NumericArray[2];
							Accessor.AsSparseVector3Array(targetAttribute.Value.Value, ref sparsePositions[0], bufferViewCache1, offset1);
							Accessor.AsSparseUIntArray(targetAttribute.Value.Value, ref sparsePositions[1], bufferViewCache2, offset2);
							break;
						case TangentKey:
							sparseTangents = new NumericArray[2];
							Accessor.AsSparseVector3Array(targetAttribute.Value.Value, ref sparseTangents[0], bufferViewCache1, offset1);
							Accessor.AsSparseUIntArray(targetAttribute.Value.Value, ref sparseTangents[1], bufferViewCache2, offset2);
							break;
					}
				}

				var att = newTargets[i];
				GLTFHelpers.BuildTargetAttributes(ref att);

				if (sparseNormals != null)
				{
					var current = att[NormalKey].AccessorContent;
					NumericArray before = new NumericArray();
					before.AsVec3s = new GLTF.Math.Vector3[current.AsVec3s.Length];
					Array.Copy(current.AsVec3s, before.AsVec3s, before.AsVec3s.Length);
					for (int j = 0; j < sparseNormals[1].AsUInts.Length; j++)
					{
						before.AsVec3s[sparseNormals[1].AsUInts[j]] = sparseNormals[0].AsVec3s[j];
					}
					att[NormalKey].AccessorContent = before;
				}

				if (sparsePositions != null)
				{
					var current = att[PositionKey].AccessorContent;
					NumericArray before = new NumericArray();
					before.AsVec3s = new GLTF.Math.Vector3[current.AsVec3s.Length];
					Array.Copy(current.AsVec3s, before.AsVec3s, before.AsVec3s.Length);
					for (int j = 0; j < sparsePositions[1].AsUInts.Length; j++)
					{
						before.AsVec3s[sparsePositions[1].AsUInts[j]] = sparsePositions[0].AsVec3s[j];
					}
					att[PositionKey].AccessorContent = before;
				}

				if (sparseTangents != null)
				{
					var current = att[TangentKey].AccessorContent;
					NumericArray before = new NumericArray();
					before.AsVec3s = new GLTF.Math.Vector3[current.AsVec3s.Length];
					Array.Copy(current.AsVec3s, before.AsVec3s, before.AsVec3s.Length);
					for (int j = 0; j < sparseTangents[1].AsUInts.Length; j++)
					{
						before.AsVec3s[sparseTangents[1].AsUInts[j]] = sparseTangents[0].AsVec3s[j];
					}
					att[TangentKey].AccessorContent = before;
				}

				TransformTargets(ref att);
			}
		}

		private async Task ConstructMeshAttributes(GLTFMesh mesh, MeshId meshId)
		{
			int meshIndex = meshId.Id;

			if (_assetCache.MeshCache[meshIndex] == null)
				_assetCache.MeshCache[meshIndex] = new MeshCacheData();
			else if (_assetCache.MeshCache[meshIndex].Primitives.Count > 0)
				return;

			for (int i = 0; i < mesh.Primitives.Count; ++i)
			{
				MeshPrimitive primitive = mesh.Primitives[i];

				await ConstructPrimitiveAttributes(primitive, meshIndex, i);

				if (primitive.Material != null)
				{
					await ConstructMaterialImageBuffers(primitive.Material.Value);
				}

				if (primitive.Targets != null)
				{
					// read mesh primitive targets into assetcache
					await ConstructMeshTargets(primitive, meshIndex, i);
				}
			}
		}

		protected virtual async Task ConstructPrimitiveAttributes(MeshPrimitive primitive, int meshIndex, int primitiveIndex)
		{
			var primData = new MeshCacheData.PrimitiveCacheData();
			_assetCache.MeshCache[meshIndex].Primitives.Add(primData);

			var attributeAccessors = primData.Attributes;
			var sparseAccessors = new Dictionary<string, (AttributeAccessor sparseIndices, AttributeAccessor sparseValues)>();
			foreach (var attributePair in primitive.Attributes)
			{
				if (attributePair.Value.Value.BufferView == null) // When Draco Compression is used, the bufferView is null
					continue;

				var bufferId = attributePair.Value.Value.BufferView.Value.Buffer;
				var bufferData = await GetBufferData(bufferId);

				attributeAccessors[attributePair.Key] = new AttributeAccessor
				{
					AccessorId = attributePair.Value,
					Stream = bufferData.Stream,
					Offset = (uint)bufferData.ChunkOffset
				};

				var sparse = attributePair.Value.Value.Sparse;
				if (sparse != null)
				{
					var sparseBufferId = sparse.Values.BufferView.Value.Buffer;
					var sparseBufferData = await GetBufferData(sparseBufferId);
					AttributeAccessor sparseValues = new AttributeAccessor
					{
						AccessorId = attributePair.Value,
						Stream = sparseBufferData.Stream,
						Offset = (uint)sparseBufferData.ChunkOffset
					};

					var sparseIndicesBufferId = sparse.Indices.BufferView.Value.Buffer;
					var sparseIndicesBufferData = await GetBufferData(sparseIndicesBufferId);
					AttributeAccessor sparseIndices = new AttributeAccessor
					{
						AccessorId = attributePair.Value,
						Stream = sparseIndicesBufferData.Stream,
						Offset = (uint)sparseIndicesBufferData.ChunkOffset
					};

					sparseAccessors[attributePair.Key] = (sparseIndices, sparseValues);
				}
			}

			if (primitive.Indices != null && primitive.Indices.Value.BufferView != null)
			{
				var bufferId = primitive.Indices.Value.BufferView.Value.Buffer;
				var bufferData = await GetBufferData(bufferId);

				attributeAccessors[SemanticProperties.INDICES] = new AttributeAccessor
				{
					AccessorId = primitive.Indices,
					Stream = bufferData.Stream,
					Offset = (uint)bufferData.ChunkOffset
				};
			}
			try
			{
				GLTFHelpers.BuildMeshAttributes(ref attributeAccessors, ref sparseAccessors);
			}
			catch (GLTFLoadException e)
			{
				Debug.Log(LogType.Warning, e.ToString());
			}
			TransformAttributes(ref attributeAccessors);
		}

		protected void ConvertAttributeAccessorsToUnityTypes(
			MeshCacheData.PrimitiveCacheData primData,
			UnityMeshData unityData,
			int vertOffset,
			int indexOffset)
		{
			// todo optimize: There are multiple copies being performed to turn the buffer data into mesh data. Look into reducing them
			var meshAttributes = primData.Attributes;
			int vertexCount = 0;
			if (meshAttributes.ContainsKey(SemanticProperties.POSITION))
			{
				vertexCount = (int)meshAttributes[SemanticProperties.POSITION].AccessorId.Value.Count;
			}

			var indices = meshAttributes.ContainsKey(SemanticProperties.INDICES)
				? meshAttributes[SemanticProperties.INDICES].AccessorContent.AsUInts.ToIntArrayRaw()
				: MeshPrimitive.GenerateTriangles(vertexCount);
			if (unityData.Topology[indexOffset] == MeshTopology.Triangles)
				SchemaExtensions.FlipTriangleFaces(indices);
			unityData.Indices[indexOffset] = indices;

			if (meshAttributes.ContainsKey(SemanticProperties.Weight[0]) && meshAttributes.ContainsKey(SemanticProperties.Joint[0]))
			{
				CreateBoneWeightArray(
					meshAttributes[SemanticProperties.Joint[0]].AccessorContent.AsVec4s.ToUnityVector4Raw(),
					meshAttributes[SemanticProperties.Weight[0]].AccessorContent.AsVec4s.ToUnityVector4Raw(),
					ref unityData.BoneWeights,
					vertOffset);
			}

			if (meshAttributes.ContainsKey(SemanticProperties.POSITION))
			{
				meshAttributes[SemanticProperties.POSITION].AccessorContent.AsVertices.ToUnityVector3Raw(unityData.Vertices, vertOffset);
			}
			if (meshAttributes.ContainsKey(SemanticProperties.NORMAL))
			{
				meshAttributes[SemanticProperties.NORMAL].AccessorContent.AsNormals.ToUnityVector3Raw(unityData.Normals, vertOffset);
			}
			if (meshAttributes.ContainsKey(SemanticProperties.TANGENT))
			{
				meshAttributes[SemanticProperties.TANGENT].AccessorContent.AsTangents.ToUnityVector4Raw(unityData.Tangents, vertOffset);
			}
			if (meshAttributes.ContainsKey(SemanticProperties.TexCoord[0]))
			{
				meshAttributes[SemanticProperties.TexCoord[0]].AccessorContent.AsTexcoords.ToUnityVector2Raw(unityData.Uv1, vertOffset);
			}
			if (meshAttributes.ContainsKey(SemanticProperties.TexCoord[1]))
			{
				meshAttributes[SemanticProperties.TexCoord[1]].AccessorContent.AsTexcoords.ToUnityVector2Raw(unityData.Uv2, vertOffset);
			}
			if (meshAttributes.ContainsKey(SemanticProperties.TexCoord[2]))
			{
				meshAttributes[SemanticProperties.TexCoord[2]].AccessorContent.AsTexcoords.ToUnityVector2Raw(unityData.Uv3, vertOffset);
			}
			if (meshAttributes.ContainsKey(SemanticProperties.TexCoord[3]))
			{
				meshAttributes[SemanticProperties.TexCoord[3]].AccessorContent.AsTexcoords.ToUnityVector2Raw(unityData.Uv4, vertOffset);
			}
			if (meshAttributes.ContainsKey(SemanticProperties.Color[0]))
			{
				if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
					meshAttributes[SemanticProperties.Color[0]].AccessorContent.AsColors.ToUnityColorRaw(unityData.Colors, vertOffset);
				else
					meshAttributes[SemanticProperties.Color[0]].AccessorContent.AsColors.ToUnityColorLinear(unityData.Colors, vertOffset);
			}

			var targets = primData.Targets;
			if (targets != null)
			{
				for (int i = 0; i < targets.Count; ++i)
				{
					if (targets[i].ContainsKey(SemanticProperties.POSITION))
					{
						targets[i][SemanticProperties.POSITION].AccessorContent.AsVec3s.ToUnityVector3Raw(unityData.MorphTargetVertices[i], vertOffset);
					}
					if (targets[i].ContainsKey(SemanticProperties.NORMAL))
					{
						targets[i][SemanticProperties.NORMAL].AccessorContent.AsVec3s.ToUnityVector3Raw(unityData.MorphTargetNormals[i], vertOffset);
					}
					if (targets[i].ContainsKey(SemanticProperties.TANGENT))
					{
						targets[i][SemanticProperties.TANGENT].AccessorContent.AsVec3s.ToUnityVector3Raw(unityData.MorphTargetTangents[i], vertOffset);
					}
				}
			}
		}

		// Flip vectors to Unity coordinate system
		private void TransformTargets(ref Dictionary<string, AttributeAccessor> attributeAccessors)
		{
			if (attributeAccessors.ContainsKey(SemanticProperties.POSITION))
			{
				AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.POSITION];
				SchemaExtensions.ConvertVector3CoordinateSpace(ref attributeAccessor, SchemaExtensions.CoordinateSpaceConversionScale);
			}

			if (attributeAccessors.ContainsKey(SemanticProperties.NORMAL))
			{
				AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.NORMAL];
				SchemaExtensions.ConvertVector3CoordinateSpace(ref attributeAccessor, SchemaExtensions.CoordinateSpaceConversionScale);
			}

			if (attributeAccessors.ContainsKey(SemanticProperties.TANGENT))
			{
				AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.TANGENT];
				SchemaExtensions.ConvertVector3CoordinateSpace(ref attributeAccessor, SchemaExtensions.CoordinateSpaceConversionScale);
			}
		}

		protected void TransformAttributes(ref Dictionary<string, AttributeAccessor> attributeAccessors)
		{
			foreach (var name in attributeAccessors.Keys)
			{
				var aa = attributeAccessors[name];
				switch (name)
				{
					case SemanticProperties.POSITION:
					case SemanticProperties.NORMAL:
						SchemaExtensions.ConvertVector3CoordinateSpace(ref aa, SchemaExtensions.CoordinateSpaceConversionScale);
						break;
					case SemanticProperties.TANGENT:
						SchemaExtensions.ConvertVector4CoordinateSpace(ref aa, SchemaExtensions.TangentSpaceConversionScale);
						break;
					case SemanticProperties.TEXCOORD_0:
					case SemanticProperties.TEXCOORD_1:
					case SemanticProperties.TEXCOORD_2:
					case SemanticProperties.TEXCOORD_3:
						SchemaExtensions.FlipTexCoordArrayV(ref aa);
						break;
				}
			}
		}

		private static void AddNewBufferAndViewToAccessor(byte[] data, Accessor accessor, GLTFRoot _gltfRoot)
		{
			_gltfRoot.Buffers.Add(new GLTFBuffer() { ByteLength = (uint) data.Length });
			_gltfRoot.BufferViews.Add(new BufferView() { ByteLength = (uint) data.Length, ByteOffset = 0, Buffer = new BufferId() { Id = _gltfRoot.Buffers.Count, Root = _gltfRoot } });
			accessor.BufferView = new BufferViewId() { Id = _gltfRoot.BufferViews.Count - 1, Root = _gltfRoot };
		}

		protected static MeshTopology GetTopology(DrawMode mode)
		{
			switch (mode)
			{
				case DrawMode.Points: return MeshTopology.Points;
				case DrawMode.Lines: return MeshTopology.Lines;
				case DrawMode.LineStrip: return MeshTopology.LineStrip;
				case DrawMode.Triangles: return MeshTopology.Triangles;
			}

			throw new Exception("Unity does not support glTF draw mode: " + mode);
		}

		/// <summary>
		/// Allocate a generic type 2D array. The size is depending on the given parameters.
		/// </summary>
		/// <param name="x">Defines the depth of the arrays first dimension</param>
		/// <param name="y">>Defines the depth of the arrays second dimension</param>
		/// <returns></returns>
		private static T[][] Allocate2dArray<T>(uint x, uint y)
		{
			var result = new T[x][];
			for (var i = 0; i < x; i++) result[i] = new T[y];
			return result;
		}
	}
}
