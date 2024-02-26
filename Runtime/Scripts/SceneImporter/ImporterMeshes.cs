﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTF;
using GLTF.Schema;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
#if HAVE_DRACO
using Draco;
using UnityGLTF.Plugins;
#if !HAVE_DRACO_VERSION_5
using DecodeResult = Draco.DracoMeshLoader.DecodeResult;
#endif
#endif

namespace UnityGLTF
{
	public partial class GLTFSceneImporter
	{
		private async Task CreateMaterials(MeshPrimitive primitive)
		{
			bool shouldUseDefaultMaterial = primitive.Material == null;
			GLTFMaterial materialToLoad = shouldUseDefaultMaterial ? DefaultMaterial : primitive.Material.Value;
			if ((shouldUseDefaultMaterial && _defaultLoadedMaterial == null) ||
			    (!shouldUseDefaultMaterial && _assetCache.MaterialCache[primitive.Material.Id] == null))
			{
				await ConstructMaterial(materialToLoad, shouldUseDefaultMaterial ? -1 : primitive.Material.Id);
			}
		}

		/// <summary>
		/// Triggers loading, converting, and constructing of a UnityEngine.Mesh, and stores it in the asset cache
		/// </summary>
		/// <param name="mesh">The definition of the mesh to generate</param>
		/// <param name="meshIndex">The index of the mesh to generate</param>
		/// <param name="cancellationToken"></param>
		/// <returns>A task that completes when the mesh is attached to the given GameObject</returns>
		protected virtual async Task ConstructMesh(GLTFMesh mesh, int meshIndex, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (_assetCache.MeshCache[meshIndex] == null)
			{
				throw new Exception("Cannot generate mesh before ConstructMeshAttributes is called!");
			}
			else if (_assetCache.MeshCache[meshIndex].LoadedMesh)
			{
				return;
			}

			var anyHadDraco = mesh.Primitives.Any(p => p.Extensions != null && p.Extensions.ContainsKey(KHR_draco_mesh_compression_Factory.EXTENSION_NAME));
#if HAVE_DRACO
			if (anyHadDraco)
			{
				if (Context.TryGetPlugin<DracoImportContext>(out _))
				{
					await PrepareDracoMesh(mesh, meshIndex);
					var dracoTask = ConstructDracoMesh(mesh, meshIndex, cancellationToken);
					await Task.WhenAll( dracoTask.Item2);
					for (int i = 0; i < dracoTask.Item2.Length; i++)
					{
						_assetCache.MeshCache[meshIndex].DracoMeshDecodeResult[i] = dracoTask.Item2[i].Result;
					}
					await BuildUnityDracoMesh(mesh, meshIndex);
					return;
				}
				else
				{
					throw new NotSupportedException("Can't import model because it uses the KHR_draco_mesh_compression extension. Add the package \"com.unity.cloud.draco\" to your project to import this file.");
				}
			}
#else
			if (anyHadDraco)
			{
				throw new NotSupportedException("Can't import model because it uses the KHR_draco_mesh_compression extension. Add the package \"com.unity.cloud.draco\" to your project to import this file.");
			}
#endif

			var firstPrim = mesh.Primitives.Count > 0 ?  mesh.Primitives[0] : null;
			cancellationToken.ThrowIfCancellationRequested();


			var meshCache = _assetCache.MeshCache[meshIndex];

			var unityData = CreateUnityMeshData(mesh, meshIndex, firstPrim);
			
			for (int i = 0; i < mesh.Primitives.Count; ++i)
			{
				var primitive = mesh.Primitives[i];
				var primCache = meshCache.Primitives[i];
				unityData.Topology[i] = GetTopology(primitive.Mode);

				ConvertAttributeAccessorsToUnityTypes(primCache, unityData, unityData.subMeshVertexOffset[i], i);

				await CreateMaterials(primitive);

				cancellationToken.ThrowIfCancellationRequested();
				
				if (unityData.Topology[i] == MeshTopology.Triangles && primitive.Indices != null && primitive.Indices.Value != null)
				{
					Statistics.TriangleCount += primitive.Indices.Value.Count / 3;
				}
			}

			Statistics.VertexCount += unityData.Vertices.Length;
			await ConstructUnityMesh(unityData, meshIndex, mesh.Name);
		}

		private static uint[] CalculateSubMeshVertexOffset(GLTFMesh mesh, out uint totalVertCount)
		{
			Dictionary<int, AccessorId> accessorIds = new Dictionary<int, AccessorId>();
			uint vOffset = 0;
			int primIndex = 0;
			uint[] vertOffsetBySubMesh = new uint[mesh.Primitives.Count];
			totalVertCount = 0;
			uint lastVertOffset = 0;
			foreach (var p in mesh.Primitives)
			{
				var acc = p.Attributes[SemanticProperties.POSITION];
				if (!accessorIds.ContainsKey(acc.Id))
				{
					accessorIds.Add(acc.Id, acc);
					totalVertCount += acc.Value.Count;
					vOffset = lastVertOffset;
					lastVertOffset += acc.Value.Count;
				}

				vertOffsetBySubMesh[primIndex] = vOffset;

				primIndex++;
			}

			return vertOffsetBySubMesh;
		}

		protected void PrepareUnityMeshData()
		{
			for (int i = 0; i < _gltfRoot.Meshes.Count(); i++)
			{
				int meshIndex = i;
				var mesh = _gltfRoot.Meshes[meshIndex];
				var meshCache = _assetCache.MeshCache[meshIndex];
				var unityData = CreateUnityMeshData(mesh, meshIndex,
					mesh.Primitives.Count > 0 ? mesh.Primitives[0] : null);
				for (int primIndex = 0; primIndex < mesh.Primitives.Count; ++primIndex)
				{
					var primitive = mesh.Primitives[primIndex];
					var primCache = meshCache.Primitives[primIndex];
					unityData.Topology[primIndex] = GetTopology(primitive.Mode);

					ConvertAttributeAccessorsToUnityTypes(primCache, unityData,
						unityData.subMeshVertexOffset[primIndex], primIndex);
				}
			}
		}
		
#if HAVE_DRACO
		protected virtual async Task PrepareDracoMesh(GLTFMesh mesh, int meshIndex)
		{
			if (_assetCache.MeshCache[meshIndex].DracoMeshDataPrepared)
				return;
			
			Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(mesh.Primitives.Count);
			_assetCache.MeshCache[meshIndex].DracoMeshData = meshDataArray;

			_assetCache.MeshCache[meshIndex].DracoMeshDecodeResult = new DecodeResult[mesh.Primitives.Count];

			_assetCache.MeshCache[meshIndex].DracoMeshDataPrepared = true;
			for (int i = 0; i < mesh.Primitives.Count; i++)
			{
				var primitive = mesh.Primitives[i];
				if (primitive.Extensions == null || !primitive.Extensions.ContainsKey("KHR_draco_mesh_compression"))
					continue;

				if (primitive.Extensions.TryGetValue("KHR_draco_mesh_compression", out var extension))
				{
					var dracoExtension = (KHR_draco_mesh_compression)extension;
					if (_assetCache.BufferCache[dracoExtension.bufferView.Value.Buffer.Id] == null)
						await ConstructBuffer(dracoExtension.bufferView.Value.Buffer.Value,
							dracoExtension.bufferView.Value.Buffer.Id);
				}
			}
		}
		
		protected virtual (int,Task<DecodeResult>[]) ConstructDracoMesh(GLTFMesh mesh, int meshIndex, CancellationToken cancellationToken)
		{
			var firstPrim = mesh.Primitives.Count > 0 ?  mesh.Primitives[0] : null;

			(int, Task<DecodeResult>[]) taskResult = new (meshIndex, new Task<DecodeResult>[_assetCache.MeshCache[meshIndex].DracoMeshDecodeResult.Length]);
			
			if (!_assetCache.MeshCache[meshIndex].DracoMeshDataPrepared)
			{
				Debug.LogError("Draco Mesh Data is not prepared! Call PrepareDracoMesh first", this);
				return new (meshIndex, new Task<DecodeResult>[0]);
			}

			if (_assetCache.MeshCache[meshIndex].HasDracoMeshData)
			{
				return new (meshIndex, new Task<DecodeResult>[0]);
			}

			_assetCache.MeshCache[meshIndex].HasDracoMeshData = true;

			for (int i = 0; i < mesh.Primitives.Count; i++)
			{
				var primitive = mesh.Primitives[i];
				if (primitive.Extensions == null || !primitive.Extensions.ContainsKey("KHR_draco_mesh_compression"))
					continue;
				
				if (primitive.Extensions.TryGetValue("KHR_draco_mesh_compression", out var extension))
				{
					var dracoExtension = (KHR_draco_mesh_compression)extension;
	
					BufferCacheData bufferContents =
						_assetCache.BufferCache[dracoExtension.bufferView.Value.Buffer.Id];

					GLTFHelpers.LoadBufferView(dracoExtension.bufferView.Value, bufferContents.ChunkOffset,
						bufferContents.bufferData, out NativeArray<byte> bufferViewData);

					int weightsAttributeId = -1;
					if (dracoExtension.attributes.TryGetValue("WEIGHTS_0", out var attribute))
						weightsAttributeId = (int)attribute;

					int jointsAttributeId = -1;
					if (dracoExtension.attributes.TryGetValue("JOINTS_0", out var extensionAttribute))
						jointsAttributeId = (int)extensionAttribute;

					// TODO: check if normals and tangents are needed
#pragma warning disable 0219
					bool hasTangents = dracoExtension.attributes.ContainsKey(SemanticProperties.TANGENT);
					
					bool needsTangents = _options.ImportTangents != GLTFImporterNormals.None && hasTangents;
					bool needsNormals = _options.ImportNormals != GLTFImporterNormals.None || needsTangents;
#pragma warning restore 0219

#if HAVE_DRACO_VERSION_5
					DecodeSettings decodeSettings = DecodeSettings.ConvertSpace;
					if (needsNormals) 
						decodeSettings |= DecodeSettings.RequireNormals;
					if (needsTangents)
						decodeSettings |= DecodeSettings.RequireTangents;
					if (firstPrim != null && firstPrim.Targets != null)
						decodeSettings |= DecodeSettings.ForceUnityVertexLayout;
					
					taskResult.Item2[i] = DracoDecoder.DecodeMesh( _assetCache.MeshCache[meshIndex].DracoMeshData[i], bufferViewData, decodeSettings, DracoDecoder.CreateAttributeIdMap(weightsAttributeId, jointsAttributeId));
					
#else
					var draco = new DracoMeshLoader();

					taskResult.Item2[i] = draco.ConvertDracoMeshToUnity(_assetCache.MeshCache[meshIndex].DracoMeshData[i], bufferViewData,
						needsNormals, needsTangents,
						weightsAttributeId, jointsAttributeId, firstPrim.Targets != null);
#endif
				}
			}

			return taskResult;
		}

		private async Task BuildUnityDracoMesh(GLTFMesh mesh, int meshIndex)
		{
			if (!_assetCache.MeshCache[meshIndex].HasDracoMeshData)
			{
				Debug.LogError("Draco Mesh Data is not decoded! Call ConstructDracoMesh first", this);
				return;
			}
			
			foreach (var primitive in mesh.Primitives)
				await CreateMaterials(primitive);
			
			await ConstructUnityMesh(mesh, _assetCache.MeshCache[meshIndex].DracoMeshDecodeResult, _assetCache.MeshCache[meshIndex].DracoMeshData, meshIndex, mesh.Name);
		}
#endif

#if HAVE_MESHOPT_DECOMPRESS
		private async Task MeshOptDecodeBuffer(GLTFRoot root)
		{
			if (root.BufferViews == null)
				return;

			int bufferViewIndex = 0;
			var jobHandlesList = new List<JobHandle>(root.BufferViews.Count);
			var meshOptBufferViews = new Dictionary<int, NativeArray<byte>>();
			var meshOptReturnValues = new NativeArray<int>( root.BufferViews.Count, Allocator.TempJob);

			foreach (var bView in root.BufferViews)
			{
				if (bView.Extensions != null && bView.Extensions.ContainsKey(EXT_meshopt_compression_Factory.EXTENSION_NAME))
				{
					var meshOpt = bView.Extensions[EXT_meshopt_compression_Factory.EXTENSION_NAME] as EXT_meshopt_compression;

					var arr = new NativeArray<byte>((int)(meshOpt.count * meshOpt.bufferView.ByteStride), Allocator.TempJob);

					if (_assetCache.BufferCache[meshOpt.bufferView.Buffer.Id] == null)
						await ConstructBuffer(meshOpt.bufferView.Buffer.Value, meshOpt.bufferView.Buffer.Id);

					BufferCacheData bufferContents = _assetCache.BufferCache[meshOpt.bufferView.Buffer.Id];

					GLTFHelpers.LoadBufferView(meshOpt.bufferView, bufferContents.ChunkOffset, bufferContents.bufferData, out NativeArray<byte> bufferViewData);

					var jobHandle = Meshoptimizer.Decode.DecodeGltfBuffer(
						new NativeSlice<int>(meshOptReturnValues,bufferViewIndex,1),
							arr,
							meshOpt.count,
							(int)meshOpt.bufferView.ByteStride,
							bufferViewData,
							meshOpt.mode,
							meshOpt.filter
						);

					jobHandlesList.Add(jobHandle);
					meshOptBufferViews[bufferViewIndex] = arr;
				}

				bufferViewIndex++;
			}

			if (jobHandlesList.Count > 0)
			{
				JobHandle meshoptJobHandle;
				using (var jobHandles = new NativeArray<JobHandle>(jobHandlesList.ToArray(), Allocator.TempJob))
				{
					 meshoptJobHandle = JobHandle.CombineDependencies(jobHandles);
				}
				while (!meshoptJobHandle.IsCompleted)
				{
					await Task.Yield();
				}
				meshoptJobHandle.Complete();
			}

			foreach (var m in meshOptBufferViews)
			{
				var bufferView = root.BufferViews[m.Key];
				var bufferData = await GetBufferData(bufferView.Buffer);
				NativeArray<byte>.Copy(m.Value, 0, bufferData.bufferData, (int)bufferView.ByteOffset, m.Value.Length);
				m.Value.Dispose();
			}
			
			meshOptReturnValues.Dispose();
		}
#endif

		protected void ApplyImportOptionsOnMesh(Mesh mesh)
		{
			if (_options.ImportNormals == GLTFImporterNormals.None)
				mesh.normals = new Vector3[0];
			else if (_options.ImportNormals == GLTFImporterNormals.Calculate && mesh.GetTopology(0) == MeshTopology.Triangles)
				mesh.RecalculateNormals();
			else if (_options.ImportNormals == GLTFImporterNormals.Import && mesh.normals.Length == 0 && mesh.GetTopology(0) == MeshTopology.Triangles)
				mesh.RecalculateNormals();
			else if (_options.ImportTangents != GLTFImporterNormals.None && mesh.normals.Length == 0)
				mesh.RecalculateNormals();

			if (_options.ImportTangents == GLTFImporterNormals.None)
				mesh.tangents = new Vector4[0];
			else if (_options.ImportTangents == GLTFImporterNormals.Calculate && mesh.GetTopology(0) == MeshTopology.Triangles)
				mesh.RecalculateTangents();
			else if (_options.ImportTangents == GLTFImporterNormals.Import && mesh.tangents.Length == 0 && mesh.GetTopology(0) == MeshTopology.Triangles)
				mesh.RecalculateTangents();

			if (_options.SwapUVs)
			{
				var uv = mesh.uv;
				var uv2 = mesh.uv2;
				mesh.uv = uv2;
				if(uv.Length > 0)
					mesh.uv2 = uv;
			}
		}

#if HAVE_DRACO
		/// <summary>
		/// Populate a UnityEngine.Mesh from Draco generated SubMeshes
		/// </summary>
		/// <returns></returns>
		protected async Task ConstructUnityMesh(GLTFMesh gltfMesh, DecodeResult[] decodeResults, Mesh.MeshDataArray meshes, int meshIndex, string meshName)
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

			Mesh[] subMeshes = new Mesh[meshes.Length];
			for (int i = 0; i < subMeshes.Length; i++)
				subMeshes[i] = new Mesh();

			Mesh.ApplyAndDisposeWritableMeshData(meshes, subMeshes);

			CombineInstance[] combineInstances = new CombineInstance[subMeshes.Length];
			for (int i = 0; i < combineInstances.Length; i++)
			{
				combineInstances[i] = new CombineInstance();
				combineInstances[i].mesh = subMeshes[i];
			}

			int boneWeightCount = 0;
			int bonesPerVertexCount = 0;
			for (int i = 0; i < subMeshes.Length; i++)
			{
				if (decodeResults[i].boneWeightData == null) continue;
				decodeResults[i].boneWeightData.ApplyOnMesh(subMeshes[i]);
				decodeResults[i].boneWeightData.Dispose();
				
				boneWeightCount += subMeshes[i].GetAllBoneWeights().Length;
				bonesPerVertexCount += subMeshes[i].GetBonesPerVertex().Length;
			}
			
			// Custom combine all boneweights and bonePerVertex of sub meshes and apply to final combined mesh 
			// >> Bug(?) in CombineMeshes that does not proper copy bone weights and bones per vertex  
			NativeArray<BoneWeight1> allBoneWeights = new NativeArray<BoneWeight1>(boneWeightCount, Allocator.TempJob);
			NativeArray<byte> allBonesPerVertex = new NativeArray<byte>(bonesPerVertexCount, Allocator.TempJob);
			int currentArrayPositionBoneWeights = 0;
			int currentArrayPositionBpV = 0;
			for (int i = 0; i < subMeshes.Length; i++)
			{
				var subMeshBoneWeights = subMeshes[i].GetAllBoneWeights();
				var subMeshBonesPerVertex = subMeshes[i].GetBonesPerVertex();
				
				NativeArray<BoneWeight1>.Copy(subMeshBoneWeights, 0, allBoneWeights, currentArrayPositionBoneWeights, subMeshBoneWeights.Length);
				NativeArray<byte>.Copy(subMeshBonesPerVertex, 0, allBonesPerVertex, currentArrayPositionBpV, subMeshBonesPerVertex.Length);
				
				currentArrayPositionBoneWeights += subMeshBoneWeights.Length;
				currentArrayPositionBpV += subMeshBonesPerVertex.Length;
			}
			
			mesh.CombineMeshes(combineInstances, false, false);

			mesh.SetBoneWeights(allBonesPerVertex, allBoneWeights);
			allBoneWeights.Dispose();
			allBonesPerVertex.Dispose();
			
			foreach (var m in subMeshes)
			{
#if UNITY_EDITOR
				UnityEngine.Object.DestroyImmediate(m);
#else
				UnityEngine.Object.Destroy(m);
#endif
			}

			await YieldOnTimeoutAndThrowOnLowMemory();

			var firstPrim = gltfMesh.Primitives[0];
			var unityMeshData = CreateUnityMeshData(gltfMesh, meshIndex, firstPrim, true);

			uint vertOffset = 0;
			var meshCache = _assetCache.MeshCache[meshIndex];

			unityMeshData.BoneWeights = mesh.boneWeights;

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
				vertOffset += (uint)mesh.GetSubMesh(i).vertexCount;
			}

			mesh.RecalculateBounds();
			await YieldOnTimeoutAndThrowOnLowMemory();

			AddBlendShapesToMesh(unityMeshData, meshIndex, mesh);
			await YieldOnTimeoutAndThrowOnLowMemory();

			ApplyImportOptionsOnMesh(mesh);

			if (!KeepCPUCopyOfMesh)
			{
				mesh.UploadMeshData(true);
			}

			_assetCache.MeshCache[meshIndex].LoadedMesh = mesh;
		}

#endif

		private UnityMeshData CreateUnityMeshData(GLTFMesh gltfMesh, int meshIndex, MeshPrimitive firstPrim, bool onlyMorphTargets = false)
		{
			if (_assetCache.UnityMeshDataCache[meshIndex] != null)
			{
				return _assetCache.UnityMeshDataCache[meshIndex];
			}
			var vertOffsetBySubMesh = CalculateSubMeshVertexOffset(gltfMesh, out var verticesLength);
			
			UnityMeshData unityMeshData = new UnityMeshData()
			{
				MorphTargetVertices = firstPrim.Targets != null && firstPrim.Targets[0].ContainsKey(SemanticProperties.POSITION)
					? Allocate2dArray<Vector3>((uint)firstPrim.Targets.Count, verticesLength)
					: null,
				MorphTargetNormals = firstPrim.Targets != null && firstPrim.Targets[0].ContainsKey(SemanticProperties.NORMAL)
					? Allocate2dArray<Vector3>((uint)firstPrim.Targets.Count, verticesLength)
					: null,
				MorphTargetTangents = firstPrim.Targets != null && firstPrim.Targets[0].ContainsKey(SemanticProperties.TANGENT)
					? Allocate2dArray<Vector3>((uint)firstPrim.Targets.Count, verticesLength)
					: null,

				Topology = new MeshTopology[gltfMesh.Primitives.Count],
				Indices = new int[gltfMesh.Primitives.Count][],
				subMeshDataCreated = new bool[gltfMesh.Primitives.Count],
				subMeshVertexOffset = vertOffsetBySubMesh
			};

			for (int i = 0; i < unityMeshData.subMeshDataCreated.Length; i++)
				unityMeshData.subMeshDataCreated[i] = false;
			_assetCache.UnityMeshDataCache[meshIndex] = unityMeshData;

			if (!onlyMorphTargets)
			{
				unityMeshData.Vertices = new Vector3[verticesLength];
				unityMeshData.Normals = firstPrim.Attributes.ContainsKey(SemanticProperties.NORMAL)
					? new Vector3[verticesLength]
					: null;
				unityMeshData.Tangents = firstPrim.Attributes.ContainsKey(SemanticProperties.TANGENT)
					? new Vector4[verticesLength]
					: null;
				unityMeshData.Uv1 = firstPrim.Attributes.ContainsKey(SemanticProperties.TEXCOORD_0)
					? new Vector2[verticesLength]
					: null;
				unityMeshData.Uv2 = firstPrim.Attributes.ContainsKey(SemanticProperties.TEXCOORD_1)
					? new Vector2[verticesLength]
					: null;
				unityMeshData.Uv3 = firstPrim.Attributes.ContainsKey(SemanticProperties.TEXCOORD_2)
					? new Vector2[verticesLength]
					: null;
				unityMeshData.Uv4 = firstPrim.Attributes.ContainsKey(SemanticProperties.TEXCOORD_3)
					? new Vector2[verticesLength]
					: null;
				unityMeshData.Colors = firstPrim.Attributes.ContainsKey(SemanticProperties.COLOR_0)
					? new Color[verticesLength]
					: null;
				unityMeshData.BoneWeights = firstPrim.Attributes.ContainsKey(SemanticProperties.WEIGHTS_0)
					? new BoneWeight[verticesLength]
					: null;
			}

			return unityMeshData;
		}

		/// <summary>
		/// Populate a UnityEngine.Mesh from preloaded and preprocessed buffer data
		/// </summary>
		/// <param name="unityMeshData"></param>
		/// <param name="meshIndex"></param>
		/// <param name="meshName"></param>
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
			for (int i = 0; i < unityMeshData.Indices.Length; i++)
			{
				mesh.SetIndices(unityMeshData.Indices[i], unityMeshData.Topology[i], i, false,
					(int)unityMeshData.subMeshVertexOffset[i]);
			}
			mesh.RecalculateBounds();
			await YieldOnTimeoutAndThrowOnLowMemory();

			AddBlendShapesToMesh(unityMeshData, meshIndex, mesh);
			await YieldOnTimeoutAndThrowOnLowMemory();

			ApplyImportOptionsOnMesh(mesh);

			if (!KeepCPUCopyOfMesh)
			{
				mesh.UploadMeshData(true);
			}

			_assetCache.MeshCache[meshIndex].LoadedMesh = mesh;
			
			// Free up some memory
			unityMeshData.Clear();
		}

		private void AddBlendShapesToMesh(UnityMeshData unityMeshData, int meshIndex, Mesh mesh)
		{
			if (unityMeshData.MorphTargetVertices != null)
			{
				var gltfMesh = _gltfRoot.Meshes[meshIndex];
				var firstPrim = gltfMesh.Primitives[0];
				// TODO theoretically there could be multiple prims and only one of them has morph targets
				for (int i = 0; i < firstPrim.Targets.Count; i++)
				{
					var targetName = _options.ImportBlendShapeNames ? ((gltfMesh.TargetNames != null && gltfMesh.TargetNames.Count > i) ? gltfMesh.TargetNames[i] : $"Morphtarget{i}") : i.ToString();
					mesh.AddBlendShapeFrame(targetName, 1f,
						unityMeshData.MorphTargetVertices[i],
						unityMeshData.MorphTargetNormals != null ? unityMeshData.MorphTargetNormals[i] : null,
						unityMeshData.MorphTargetTangents != null ? unityMeshData.MorphTargetTangents[i] : null
					);
				}
			}
		}

		protected virtual async Task ConstructMeshTargetsPrepareBuffers(MeshPrimitive primitive, int meshIndex, int primitiveIndex)
		{
			var newTargets = new List<Dictionary<string, AttributeAccessor>>(primitive.Targets.Count);
			_assetCache.MeshCache[meshIndex].Primitives[primitiveIndex].Targets = newTargets;

			// Prepare Buffer Data
			for (int i = 0; i < primitive.Targets.Count; i++)
			{
				var target = primitive.Targets[i];
				newTargets.Add(new Dictionary<string, AttributeAccessor>());

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
						targetAttribute.Value.Value.BufferView =
							primitive.Attributes[targetAttribute.Key].Value.BufferView;
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
						bufferData = _assetCache.BufferCache[bufferID].bufferData,
						Offset = (uint)_assetCache.BufferCache[bufferID].ChunkOffset
					};

					// if this buffer isn't sparse, we're done here
					if (targetAttribute.Value.Value.Sparse == null) continue;

					var bufferId = targetAttribute.Value.Value.Sparse.Values.BufferView.Value.Buffer;
					await GetBufferData(bufferId);

					bufferId = targetAttribute.Value.Value.Sparse.Indices.BufferView.Value.Buffer;
					await GetBufferData(bufferId);
				}
			}
		}

		protected virtual void ConstructMeshTargets(MeshPrimitive primitive, int meshIndex, int primitiveIndex)
		{
			var newTargets = _assetCache.MeshCache[meshIndex].Primitives[primitiveIndex].Targets;
			for (int i = 0; i < primitive.Targets.Count; i++)
			{
				var target = primitive.Targets[i];
				var att = newTargets[i];

				NumericArray[] sparseNormals = null;
				NumericArray[] sparsePositions = null;
				NumericArray[] sparseTangents = null;

				const string NormalKey = "NORMAL";
				const string PositionKey = "POSITION";
				const string TangentKey = "TANGENT";

				// normals, positions, tangents
				foreach (var targetAttribute in target)
				{
					if (targetAttribute.Value.Value.Sparse != null)
					{
						// When using Draco, it's possible the BufferView is null
						if (primitive.Attributes[targetAttribute.Key].Value.BufferView == null)
						{
							continue;
						}
					}

					// if this buffer isn't sparse, we're done here
					if (targetAttribute.Value.Value.Sparse == null) continue;

					// Values
					var bufferId = targetAttribute.Value.Value.Sparse.Values.BufferView.Value.Buffer;
					var bufferData = _assetCache.BufferCache[bufferId.Id];
					AttributeAccessor sparseValues = new AttributeAccessor
					{
						AccessorId = targetAttribute.Value,
						bufferData = bufferData.bufferData,
						Offset = (uint)bufferData.ChunkOffset
					};
					GLTFHelpers.LoadBufferView(sparseValues.AccessorId.Value.Sparse.Values.BufferView.Value,
						sparseValues.Offset, sparseValues.bufferData, out NativeArray<byte> bufferViewCache1);

					// Indices
					bufferId = targetAttribute.Value.Value.Sparse.Indices.BufferView.Value.Buffer;
					bufferData = _assetCache.BufferCache[bufferId.Id];
					AttributeAccessor sparseIndices = new AttributeAccessor
					{
						AccessorId = targetAttribute.Value,
						bufferData = bufferData.bufferData,
						Offset = (uint)bufferData.ChunkOffset,
					};
					GLTFHelpers.LoadBufferView(sparseIndices.AccessorId.Value.Sparse.Indices.BufferView.Value,
						sparseIndices.Offset, sparseIndices.bufferData, out NativeArray<byte> bufferViewCache2);

					switch (targetAttribute.Key)
					{
						case NormalKey:
							sparseNormals = new NumericArray[2];
							Accessor.AsSparseVector3Array(targetAttribute.Value.Value, ref sparseNormals[0],
								bufferViewCache1);
							Accessor.AsSparseUIntArray(targetAttribute.Value.Value, ref sparseNormals[1],
								bufferViewCache2);
							break;
						case PositionKey:
							sparsePositions = new NumericArray[2];
							Accessor.AsSparseVector3Array(targetAttribute.Value.Value, ref sparsePositions[0],
								bufferViewCache1);
							Accessor.AsSparseUIntArray(targetAttribute.Value.Value, ref sparsePositions[1],
								bufferViewCache2);
							break;
						case TangentKey:
							sparseTangents = new NumericArray[2];
							Accessor.AsSparseVector3Array(targetAttribute.Value.Value, ref sparseTangents[0],
								bufferViewCache1);
							Accessor.AsSparseUIntArray(targetAttribute.Value.Value, ref sparseTangents[1],
								bufferViewCache2);
							break;
					}
				}

				GLTFHelpers.BuildTargetAttributes(ref att);

				if (sparseNormals != null)
				{
					var current = att[NormalKey].AccessorContent;
					NumericArray before = new NumericArray();
					before.AsFloats3 = new float3[current.AsFloats3.Length];
					for (int j = 0; j < sparseNormals[1].AsUInts.Length; j++)
					{
						before.AsFloats3[sparseNormals[1].AsUInts[j]] = sparseNormals[0].AsFloats3[j];
					}

					att[NormalKey].AccessorContent = before;
				}

				if (sparsePositions != null)
				{
					var current = att[PositionKey].AccessorContent;
					NumericArray before = new NumericArray();
					before.AsFloats3 = new float3[current.AsFloats3.Length];
					for (int j = 0; j < sparsePositions[1].AsUInts.Length; j++)
					{
						before.AsFloats3[sparsePositions[1].AsUInts[j]] = sparsePositions[0].AsFloats3[j];
					}

					att[PositionKey].AccessorContent = before;
				}

				if (sparseTangents != null)
				{
					var current = att[TangentKey].AccessorContent;
					NumericArray before = new NumericArray();
					before.AsFloats3 = new float3[current.AsFloats3.Length];
					for (int j = 0; j < sparseTangents[1].AsUInts.Length; j++)
					{
						before.AsFloats3[sparseTangents[1].AsUInts[j]] = sparseTangents[0].AsFloats3[j];
					}

					att[TangentKey].AccessorContent = before;
				}

				TransformTargets(ref att);
			}
		}

		private void FreeUpAccessorContents()
		{
			for (int meshIndex = 0; meshIndex < _gltfRoot.Meshes.Count; meshIndex++)
			{
				var gltfMesh = _gltfRoot.Meshes[meshIndex];
				for (int primIndex = 0; primIndex < gltfMesh.Primitives.Count; primIndex++)
				{
					var primCache = _assetCache.MeshCache[meshIndex].Primitives[primIndex];
					if (primCache.meshAttributesCreated)
					{
						foreach (var att in primCache.Attributes)
						{
							att.Value.AccessorContent = new NumericArray();
						}

						foreach (var t in primCache.SparseAccessors)
						{
							t.Value.sparseValues.AccessorContent = new NumericArray();
							t.Value.sparseIndices.AccessorContent = new NumericArray();
						}
						
						foreach (var t in primCache.Targets)
						{
							foreach (var att in t)
							{
								att.Value.AccessorContent = new NumericArray();
							}
						}
					}
				}
			}
		}

		private async Task PreparePrimitiveAttributes()
		{
			for (int meshIndex = 0; meshIndex < _gltfRoot.Meshes.Count; meshIndex++)
			{
				if (_assetCache.MeshCache[meshIndex] == null)
					_assetCache.MeshCache[meshIndex] = new MeshCacheData();

				var gltfMesh = _gltfRoot.Meshes[meshIndex];
				for (int i = 0; i < gltfMesh.Primitives.Count; i++)
				{
					await ConstructPrimitiveAttributes(gltfMesh.Primitives[i], meshIndex, i);
					if (gltfMesh.Primitives[i].Targets != null)
						await ConstructMeshTargetsPrepareBuffers(gltfMesh.Primitives[i], meshIndex, i);
				}
				
#if HAVE_DRACO
				if (Context.TryGetPlugin<DracoImportContext>(out _))
				{
					var anyHadDraco = gltfMesh.Primitives.Any(p =>
						p.Extensions != null &&
						p.Extensions.ContainsKey(KHR_draco_mesh_compression_Factory.EXTENSION_NAME));
					if (anyHadDraco)
					{
						await PrepareDracoMesh(gltfMesh, meshIndex);
					}
				}
#endif
			}

#if HAVE_DRACO
			if (Context.TryGetPlugin<DracoImportContext>(out _))
			{

				List<(int, Task<DecodeResult>[])> dracoDecodeResults = new List<(int, Task<DecodeResult>[])>();
				for (int meshIndex = 0; meshIndex < _gltfRoot.Meshes.Count; meshIndex++)
				{
					var gltfMesh = _gltfRoot.Meshes[meshIndex];
					var anyHadDraco = gltfMesh.Primitives.Any(p =>
						p.Extensions != null &&
						p.Extensions.ContainsKey(KHR_draco_mesh_compression_Factory.EXTENSION_NAME));

					if (anyHadDraco)
					{
						dracoDecodeResults.Add(ConstructDracoMesh(gltfMesh, meshIndex, CancellationToken.None));
					}
				}

				await Task.WhenAll(dracoDecodeResults.Select(d => d.Item2).SelectMany(d => d));

				for (int i = 0; i < dracoDecodeResults.Count; i++)
				{
					int meshIndex = dracoDecodeResults[i].Item1;

					for (int j = 0; j < dracoDecodeResults[i].Item2.Length; j++)
					{
						var decodeResult = dracoDecodeResults[i].Item2[j].Result;
						_assetCache.MeshCache[dracoDecodeResults[i].Item1].DracoMeshDecodeResult[j] = decodeResult;

						if (!decodeResult.success)
						{
							Debug.LogError("Error decoding draco mesh", this);
							_assetCache.MeshCache[meshIndex].DracoMeshData.Dispose();
						}

						Statistics.VertexCount += _assetCache.MeshCache[meshIndex].DracoMeshData[j].vertexCount;
					}
				}

			}
#endif
			void BuildMeshesAttributes()
			{
				for (int meshIndex = 0; meshIndex < _gltfRoot.Meshes.Count; meshIndex++)
				{
					var gltfMesh = _gltfRoot.Meshes[meshIndex];
					for (int primIndex = 0; primIndex < gltfMesh.Primitives.Count; primIndex++)
					{
						var primCache = _assetCache.MeshCache[meshIndex].Primitives[primIndex];
						if (!primCache.meshAttributesCreated)
						{
							primCache.meshAttributesCreated = true;
							GLTFHelpers.BuildMeshAttributes(ref primCache.Attributes,ref primCache.SparseAccessors);
							if (gltfMesh.Primitives[primIndex].Targets != null)
								ConstructMeshTargets(gltfMesh.Primitives[primIndex], meshIndex, primIndex);
						}
						
					}
					
				}
			}
			
			if (IsMultithreaded)
				await Task.Run(BuildMeshesAttributes);
			else
				BuildMeshesAttributes();
		}

		private async Task ConstructMeshAttributes(GLTFMesh mesh, MeshId meshId)
		{
			int meshIndex = meshId.Id;

			if (_assetCache.MeshCache[meshIndex] == null)
				_assetCache.MeshCache[meshIndex] = new MeshCacheData();

			for (int i = 0; i < mesh.Primitives.Count; ++i)
			{
				MeshPrimitive primitive = mesh.Primitives[i];

				await ConstructPrimitiveAttributes(primitive, meshIndex, i);
				var primCache = _assetCache.MeshCache[meshIndex].Primitives[i];
				if (!primCache.meshAttributesCreated)
				{
					primCache.meshAttributesCreated = true;
					GLTFHelpers.BuildMeshAttributes(ref primCache.Attributes, ref primCache.SparseAccessors);

					if (primitive.Targets != null)
					{
						// read mesh primitive targets into assetcache
						await ConstructMeshTargetsPrepareBuffers(primitive, meshIndex, i);
						ConstructMeshTargets(primitive, meshIndex, i);
					}
				}
				
				if (primitive.Material != null)
				{
					await ConstructMaterialImageBuffers(primitive.Material.Value);
				}
	
			}
		}
		
		protected virtual async Task ConstructPrimitiveAttributes(MeshPrimitive primitive, int meshIndex, int primitiveIndex)
		{
			if (_assetCache.MeshCache[meshIndex].Primitives.Count-1 >= primitiveIndex)
				return;
			
			var primData = new MeshCacheData.PrimitiveCacheData();
			_assetCache.MeshCache[meshIndex].Primitives.Add(primData);
				
			foreach (var attributePair in primitive.Attributes)
			{
				if (attributePair.Value.Value.BufferView == null) // When Draco Compression is used, the bufferView is null
					continue;

				if (!primData.Attributes.ContainsKey(attributePair.Key))
				{
					var bufferId = attributePair.Value.Value.BufferView.Value.Buffer;
					var bufferData = await GetBufferData(bufferId);
					
					primData.Attributes[attributePair.Key] = new AttributeAccessor
					{
						AccessorId = attributePair.Value,
						bufferData = bufferData.bufferData,
						Offset = (uint)bufferData.ChunkOffset
					};
				}
				
				var sparse = attributePair.Value.Value.Sparse;
				if (sparse != null)
				{
					if (!primData.Attributes.ContainsKey(attributePair.Key))
					{
						var sparseBufferId = sparse.Values.BufferView.Value.Buffer;
						var sparseBufferData = await GetBufferData(sparseBufferId);
						AttributeAccessor sparseValues = new AttributeAccessor
						{
							AccessorId = attributePair.Value,
							bufferData = sparseBufferData.bufferData,
							Offset = (uint)sparseBufferData.ChunkOffset
						};

						var sparseIndicesBufferId = sparse.Indices.BufferView.Value.Buffer;
						var sparseIndicesBufferData = await GetBufferData(sparseIndicesBufferId);
						AttributeAccessor sparseIndices = new AttributeAccessor
						{
							AccessorId = attributePair.Value,
							bufferData = sparseIndicesBufferData.bufferData,
							Offset = (uint)sparseIndicesBufferData.ChunkOffset
						};

						primData.SparseAccessors[attributePair.Key] = (sparseIndices, sparseValues);
					}
				}
			}

			if (primitive.Indices != null && primitive.Indices.Value.BufferView != null)
			{
				if (!primData.Attributes.ContainsKey(SemanticProperties.INDICES))
				{
					var bufferId = primitive.Indices.Value.BufferView.Value.Buffer;
					var bufferData = await GetBufferData(bufferId);

					primData.Attributes[SemanticProperties.INDICES] = new AttributeAccessor
					{
						AccessorId = primitive.Indices,
						bufferData = bufferData.bufferData,
						Offset = (uint)bufferData.ChunkOffset
					};
				}
			}
		}

		protected void ConvertAttributeAccessorsToUnityTypes(
			MeshCacheData.PrimitiveCacheData primData,
			UnityMeshData unityData,
			uint vertOffset,
			int indexOffset)
		{
			if (unityData.subMeshDataCreated[indexOffset])
				return;
			unityData.subMeshDataCreated[indexOffset] = true;

			var meshAttributes = primData.Attributes;
			uint vertexCount = 0;
			if (meshAttributes.TryGetValue(SemanticProperties.POSITION, out var attribute))
			{
				vertexCount = attribute.AccessorId.Value.Count;
			}

			int[] indices;

			if (meshAttributes.TryGetValue(SemanticProperties.INDICES, out var indicesAccessor))
			{
				indices = indicesAccessor.AccessorContent.AsUInts.ToIntArrayRaw();
				if (unityData.Topology[indexOffset] == MeshTopology.Triangles)
					SchemaExtensions.FlipTriangleFaces(indices);
			}
			else
			{
				indices = MeshPrimitive.GenerateTriangles((int)vertexCount);
			}

			unityData.Indices[indexOffset] = indices;

			// Only add weight/joint data when it's not already added to the unity mesh data !
			if (meshAttributes.ContainsKey(SemanticProperties.Weight[0]) && meshAttributes.ContainsKey(SemanticProperties.Joint[0])
			    && !unityData.alreadyAddedAccessors.Contains(meshAttributes[SemanticProperties.Weight[0]].AccessorId.Id))
			{
				unityData.alreadyAddedAccessors.Add(meshAttributes[SemanticProperties.Weight[0]].AccessorId.Id);
				
				CreateBoneWeightArray(
					meshAttributes[SemanticProperties.Joint[0]].AccessorContent.AsFloats4.ToUnityVector4Raw(),
					meshAttributes[SemanticProperties.Weight[0]].AccessorContent.AsFloats4.ToUnityVector4Raw(),
					ref unityData.BoneWeights,
					vertOffset);
			}

			// Only add vertex data when it's not already added to the unity mesh data !
			if (meshAttributes.ContainsKey(SemanticProperties.POSITION) && !unityData.alreadyAddedAccessors.Contains(meshAttributes[SemanticProperties.POSITION].AccessorId.Id))
			{
				
				if (meshAttributes.TryGetValue(SemanticProperties.POSITION, out var attrPos))
				{
					unityData.alreadyAddedAccessors.Add(attrPos.AccessorId.Id);
					attrPos.AccessorContent.AsFloats3.ToUnityVector3Raw(unityData.Vertices, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.NORMAL, out var attrNorm))
				{
					attrNorm.AccessorContent.AsFloats3.ToUnityVector3Raw(unityData.Normals, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.TANGENT, out var attrTang))
				{
					attrTang.AccessorContent.AsFloats4.ToUnityVector4Raw(unityData.Tangents, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.TexCoord[0], out var attrTex0))
				{
					attrTex0.AccessorContent.AsFloats2.ToUnityVector2Raw(unityData.Uv1, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.TexCoord[1], out var attrTex1))
				{
					attrTex1.AccessorContent.AsFloats2.ToUnityVector2Raw(unityData.Uv2, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.TexCoord[2], out var attrTex2))
				{
					attrTex2.AccessorContent.AsFloats2.ToUnityVector2Raw(unityData.Uv3, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.TexCoord[3], out var attrTex3))
				{
					attrTex3.AccessorContent.AsFloats2.ToUnityVector2Raw(unityData.Uv4, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.Color[0], out var attrColor))
				{
					if (_activeColorSpace == ColorSpace.Gamma)
						attrColor.AccessorContent.AsFloats4.ToUnityColorRaw(unityData.Colors, (int)vertOffset);
					else
						attrColor.AccessorContent.AsFloats4.ToUnityColorLinear(unityData.Colors, (int)vertOffset);
				}

			}
			var targets = primData.Targets;
			if (targets != null)
			{
				for (int i = 0; i < targets.Count; ++i)
				{
					if (targets[i].TryGetValue(SemanticProperties.POSITION, out var tarAttrPos) && !unityData.alreadyAddedAccessors.Contains(tarAttrPos.AccessorId.Id))
					{
						unityData.alreadyAddedAccessors.Add(tarAttrPos.AccessorId.Id);
						tarAttrPos.AccessorContent.AsFloats3.ToUnityVector3Raw(unityData.MorphTargetVertices[i], (int)vertOffset);
					}
					if (targets[i].TryGetValue(SemanticProperties.NORMAL, out var tarAttrNorm) && !unityData.alreadyAddedAccessors.Contains(tarAttrNorm.AccessorId.Id))
					{
						unityData.alreadyAddedAccessors.Add(tarAttrNorm.AccessorId.Id);
						tarAttrNorm.AccessorContent.AsFloats3.ToUnityVector3Raw(unityData.MorphTargetNormals[i], (int)vertOffset);
					}
					if (targets[i].TryGetValue(SemanticProperties.TANGENT, out var tarAttrTang) && !unityData.alreadyAddedAccessors.Contains(tarAttrTang.AccessorId.Id))
					{
						unityData.alreadyAddedAccessors.Add(tarAttrTang.AccessorId.Id);
						tarAttrTang.AccessorContent.AsFloats3.ToUnityVector3Raw(unityData.MorphTargetTangents[i], (int)vertOffset);
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
