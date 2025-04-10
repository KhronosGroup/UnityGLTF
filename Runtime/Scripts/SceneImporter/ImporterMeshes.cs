using System;
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
#if HAVE_DRACO
		protected class DracoDecodeResult
		{
			public int meshIndex;
			public Task<DecodeResult>[] decodeResults = new Task<DecodeResult>[0];
		}		
#endif
		
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
				throw new Exception($"Cannot generate mesh before ConstructMeshAttributes is called! (File: {_gltfFileName})");
			}
			else if (_assetCache.MeshCache[meshIndex].LoadedMesh)
			{
				// Make sure we created all materials in case this mesh is shared because of deduplication
				await CreateMeshMaterials(mesh);
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
					await Task.WhenAll( dracoTask.decodeResults);
					for (int i = 0; i < dracoTask.decodeResults.Length; i++)
					{
						_assetCache.MeshCache[meshIndex].DracoMeshDecodeResult[i] = dracoTask.decodeResults[i].Result;
					}
					await BuildUnityDracoMesh(mesh, meshIndex);
					return;
				}
				else
				{
					throw new NotSupportedException($"Can't import model because it uses the KHR_draco_mesh_compression extension. Add the package \"com.unity.cloud.draco\" to your project to import this file. (File: {_gltfFileName})");
				}
			}
#else
			if (anyHadDraco)
			{
				throw new NotSupportedException($"Can't import model because it uses the KHR_draco_mesh_compression extension. Add the package \"com.unity.cloud.draco\" to your project to import this file. (File: {_gltfFileName})");
			}
#endif

			cancellationToken.ThrowIfCancellationRequested();
			
			var meshCache = _assetCache.MeshCache[meshIndex];

			var unityData = CreateUnityMeshData(mesh, meshIndex);
			
			for (int i = 0; i < mesh.Primitives.Count; ++i)
			{
				var primitive = mesh.Primitives[i];
				var primCache = meshCache.Primitives[i];
				if (!unityData.subMeshDataCreated[i])
				{
					unityData.Topology[i] = GetTopology(primitive.Mode);
					unityData.DrawModes[i] = primitive.Mode;

					ConvertAttributeAccessorsToUnityTypes(primCache, unityData, unityData.subMeshVertexOffset[i], i);
					
					cancellationToken.ThrowIfCancellationRequested();

					if (unityData.Topology[i] == MeshTopology.Triangles && primitive.Indices != null &&
					    primitive.Indices.Value != null)
					{
						Statistics.TriangleCount += primitive.Indices.Value.Count / 3;
					}
				}

				await CreateMaterials(primitive);
			}
			
			if (unityData.Vertices != null)
				Statistics.VertexCount += unityData.Vertices.Length;
			await ConstructUnityMesh(unityData, meshIndex, mesh.Name);
		}

		private async Task CreateMeshMaterials(GLTFMesh mesh)
		{
			for (int i = 0; i < mesh.Primitives.Count; ++i)
			{
				var primitive = mesh.Primitives[i];
				await CreateMaterials(primitive);
			}
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
			if (_gltfRoot.Meshes == null)
				return;
			
			for (int i = 0; i < _gltfRoot.Meshes.Count(); i++)
			{
				int meshIndex = i;
				var mesh = _gltfRoot.Meshes[meshIndex];
				var meshCache = _assetCache.MeshCache[meshIndex];
				var unityData = CreateUnityMeshData(mesh, meshIndex);
				for (int primIndex = 0; primIndex < mesh.Primitives.Count; ++primIndex)
				{
					var primitive = mesh.Primitives[primIndex];
					var primCache = meshCache.Primitives[primIndex];
					unityData.Topology[primIndex] = GetTopology(primitive.Mode);
					unityData.DrawModes[primIndex] = primitive.Mode;

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
		
		protected virtual DracoDecodeResult ConstructDracoMesh(GLTFMesh mesh, int meshIndex, CancellationToken cancellationToken)
		{
			var firstPrim = mesh.Primitives.Count > 0 ?  mesh.Primitives[0] : null;

			DracoDecodeResult decodeResult = new DracoDecodeResult
			{
				meshIndex = meshIndex,
				decodeResults = new Task<DecodeResult>[_assetCache.MeshCache[meshIndex].DracoMeshDecodeResult.Length]
			};
			
			if (!_assetCache.MeshCache[meshIndex].DracoMeshDataPrepared)
			{
				Debug.Log(LogType.Error, $"Draco Mesh Data is not prepared! Call PrepareDracoMesh first (File: {_gltfFileName})");
				return new DracoDecodeResult();
			}

			if (_assetCache.MeshCache[meshIndex].HasDracoMeshData)
			{
				return new DracoDecodeResult();
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

					var attrMap = DracoDecoder.CreateAttributeIdMap(weightsAttributeId, jointsAttributeId);
					if (attrMap == null)
						attrMap = new Dictionary<VertexAttribute, int>();

					if (hasTangents)
						attrMap.Add( VertexAttribute.Tangent, dracoExtension.attributes[SemanticProperties.TANGENT]);
					
					if (dracoExtension.attributes.TryGetValue(SemanticProperties.COLOR_0, out var colorAttr))
						attrMap.Add( VertexAttribute.Color, colorAttr);
					
					if (dracoExtension.attributes.TryGetValue(SemanticProperties.TEXCOORD_0, out var uvAttr))
						attrMap.Add( VertexAttribute.TexCoord0, uvAttr);
					
					if (dracoExtension.attributes.TryGetValue(SemanticProperties.TEXCOORD_1, out var uv2Attr))
						attrMap.Add( VertexAttribute.TexCoord1, uv2Attr);
					
					if (dracoExtension.attributes.TryGetValue(SemanticProperties.NORMAL, out var normalAttr))
						attrMap.Add( VertexAttribute.Normal, normalAttr);
					
					if (dracoExtension.attributes.TryGetValue(SemanticProperties.POSITION, out var positionAttr))
						attrMap.Add( VertexAttribute.Position, positionAttr);

					decodeResult.decodeResults[i] = DracoDecoder.DecodeMesh( _assetCache.MeshCache[meshIndex].DracoMeshData[i], bufferViewData, decodeSettings, attrMap);
					
#else
					var draco = new DracoMeshLoader();

					decodeResult.decodeResults[i] = draco.ConvertDracoMeshToUnity(_assetCache.MeshCache[meshIndex].DracoMeshData[i], bufferViewData,
						needsNormals, needsTangents,
						weightsAttributeId, jointsAttributeId, firstPrim.Targets != null);
#endif
				}
			}

			return decodeResult;
		}

		private async Task BuildUnityDracoMesh(GLTFMesh mesh, int meshIndex)
		{
			if (!_assetCache.MeshCache[meshIndex].HasDracoMeshData)
			{
				Debug.Log(LogType.Error, $"Draco Mesh Data is not decoded! Call ConstructDracoMesh first (File: {_gltfFileName})");
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
			bool isTriangleTopology = mesh.GetTopology(0) == MeshTopology.Triangles;

			if (_options.ImportNormals == GLTFImporterNormals.None)
				mesh.normals = Array.Empty<Vector3>();
			else
			if (isTriangleTopology)
			{
				if (_options.ImportNormals == GLTFImporterNormals.Calculate)
					mesh.RecalculateNormals();
				else if (_options.ImportNormals == GLTFImporterNormals.Import && mesh.normals.Length == 0)
					mesh.RecalculateNormals();
				else if (_options.ImportTangents != GLTFImporterNormals.None && mesh.normals.Length == 0)
					mesh.RecalculateNormals();
			}
		
			if (_options.ImportTangents == GLTFImporterNormals.None)
				mesh.tangents = Array.Empty<Vector4>();
			else
			if (isTriangleTopology)
			{
				if (_options.ImportTangents == GLTFImporterNormals.Calculate)
					mesh.RecalculateTangents();
				else if (_options.ImportTangents == GLTFImporterNormals.Import && mesh.tangents.Length == 0)
					mesh.RecalculateTangents();
			}

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

			var unityMeshData = CreateUnityMeshData(gltfMesh, meshIndex, true);

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

		private UnityMeshData CreateUnityMeshData(GLTFMesh gltfMesh, int meshIndex, bool onlyMorphTargets = false)
		{
			if (_assetCache.UnityMeshDataCache[meshIndex] != null)
			{
				return _assetCache.UnityMeshDataCache[meshIndex];
			}
			var vertOffsetBySubMesh = CalculateSubMeshVertexOffset(gltfMesh, out var verticesLength);

			UnityMeshData unityMeshData = new UnityMeshData()
			{

				Topology = new MeshTopology[gltfMesh.Primitives.Count],
				DrawModes = new DrawMode[gltfMesh.Primitives.Count],
				Indices = new int[gltfMesh.Primitives.Count][],
				subMeshDataCreated = new bool[gltfMesh.Primitives.Count],
				subMeshVertexOffset = vertOffsetBySubMesh
			};

			for (int i = 0; i < unityMeshData.subMeshDataCreated.Length; i++)
				unityMeshData.subMeshDataCreated[i] = false;

			var attributes = new HashSet<string>();
			bool hasTargets = false;
			int targetCount = 0;
			foreach (var prim in gltfMesh.Primitives)
			{
				if (prim.Targets != null)
				{
					hasTargets = true;
					targetCount = prim.Targets.Count;
				}
				
				if (prim.Attributes == null)
					continue;
				
				foreach (var attribute in prim.Attributes)
					attributes.Add(attribute.Key);
			}
			
			if (hasTargets)
			{
				unityMeshData.MorphTargetVertices = new Vector3[targetCount][];
				unityMeshData.MorphTargetNormals = new Vector3[targetCount][];
				unityMeshData.MorphTargetTangents = new Vector3[targetCount][];

				foreach (var prim in gltfMesh.Primitives)
				{
					for (int i = 0; i < prim.Targets.Count; i++)
					{
						if (unityMeshData.MorphTargetVertices[i] == null && prim.Targets[i].ContainsKey(SemanticProperties.POSITION))
						{
							unityMeshData.MorphTargetVertices[i] = new Vector3[verticesLength];
							for (int j = 0; j < verticesLength; j++)
								unityMeshData.MorphTargetVertices[i][j] = Vector3.zero;
						}
						
						if (unityMeshData.MorphTargetNormals[i] == null && prim.Targets[i].ContainsKey(SemanticProperties.NORMAL))
						{
							unityMeshData.MorphTargetNormals[i] = new Vector3[verticesLength];
							for (int j = 0; j < verticesLength; j++)
								unityMeshData.MorphTargetNormals[i][j] = Vector3.zero;
						}
						
						if (unityMeshData.MorphTargetTangents[i] == null && prim.Targets[i].ContainsKey(SemanticProperties.TANGENT))
						{
							unityMeshData.MorphTargetTangents[i] = new Vector3[verticesLength];
							for (int j = 0; j < verticesLength; j++)
								unityMeshData.MorphTargetTangents[i][j] = Vector3.zero;
						}
					}
				}
			}
			
			_assetCache.UnityMeshDataCache[meshIndex] = unityMeshData;

			if (!onlyMorphTargets)
			{
				unityMeshData.Vertices = new Vector3[verticesLength];
				unityMeshData.Normals = attributes.Contains(SemanticProperties.NORMAL)
					? new Vector3[verticesLength]
					: null;
				unityMeshData.Tangents = attributes.Contains(SemanticProperties.TANGENT)
					? new Vector4[verticesLength]
					: null;
				unityMeshData.Uv1 = attributes.Contains(SemanticProperties.TEXCOORD_0)
					? new Vector2[verticesLength]
					: null;
				unityMeshData.Uv2 = attributes.Contains(SemanticProperties.TEXCOORD_1)
					? new Vector2[verticesLength]
					: null;
				unityMeshData.Uv3 = attributes.Contains(SemanticProperties.TEXCOORD_2)
					? new Vector2[verticesLength]
					: null;
				unityMeshData.Uv4 = attributes.Contains(SemanticProperties.TEXCOORD_3)
					? new Vector2[verticesLength]
					: null;
				unityMeshData.Colors = attributes.Contains(SemanticProperties.COLOR_0)
					? new Color[verticesLength]
					: null;
				unityMeshData.BoneWeights = attributes.Contains(SemanticProperties.WEIGHTS_0)
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
			if (_assetCache.MeshCache[meshIndex].LoadedMesh != null)
				return;

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

			// Assign the loaded mesh to all MeshCache entries that reference the same UnityMeshData
			for (int i = 0; i < _assetCache.UnityMeshDataCache.Length; i++)
				if (_assetCache.UnityMeshDataCache[i] == unityMeshData)
					_assetCache.MeshCache[i].LoadedMesh = mesh;
			
			// Free up some memory
			unityMeshData.Clear();
		}

		private void AddBlendShapesToMesh(UnityMeshData unityMeshData, int meshIndex, Mesh mesh)
		{
			if (unityMeshData.MorphTargetVertices != null && _gltfRoot.Meshes != null)
			{
				var gltfMesh = _gltfRoot.Meshes[meshIndex];
				var firstPrim = gltfMesh.Primitives[0];
				// TODO theoretically there could be multiple prims and only one of them has morph targets
				for (int i = 0; i < firstPrim.Targets.Count; i++)
				{
					var targetName = _options.ImportBlendShapeNames ? ((gltfMesh.TargetNames != null && gltfMesh.TargetNames.Count > i) ? gltfMesh.TargetNames[i] : $"Morphtarget{i}") : i.ToString();
					mesh.AddBlendShapeFrame(targetName, 1f * _options.BlendShapeFrameWeight,
						unityMeshData.MorphTargetVertices[i],
						unityMeshData.MorphTargetNormals != null && unityMeshData.MorphTargetNormals[i] != null ? unityMeshData.MorphTargetNormals[i] : null,
						unityMeshData.MorphTargetTangents != null && unityMeshData.MorphTargetTangents[i] != null ? unityMeshData.MorphTargetTangents[i] : null
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
			float scaleFactor = 0f;
			bool hasScale = false;
#if UNITY_EDITOR
			hasScale = Context != null && !Mathf.Approximately(Context.ImportScaleFactor, 1f);
			scaleFactor = hasScale ? Context.ImportScaleFactor : 1f;
#endif	
			
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
							Accessor.AsSparseFloat3Array(targetAttribute.Value.Value, ref sparseNormals[0],
								bufferViewCache1, 0, targetAttribute.Value.Value.Normalized);
							Accessor.AsSparseUIntArray(targetAttribute.Value.Value, ref sparseNormals[1],
								bufferViewCache2);
							break;
						case PositionKey:
							sparsePositions = new NumericArray[2];
							if (hasScale)
								Accessor.AsSparseFloat3ArrayConversion(targetAttribute.Value.Value, ref sparsePositions[0],
									bufferViewCache1, scaleFactor, 0, targetAttribute.Value.Value.Normalized);
							else
								Accessor.AsSparseFloat3Array(targetAttribute.Value.Value, ref sparsePositions[0],
									bufferViewCache1, 0, targetAttribute.Value.Value.Normalized);
							Accessor.AsSparseUIntArray(targetAttribute.Value.Value, ref sparsePositions[1],
								bufferViewCache2);
							break;
						case TangentKey:
							sparseTangents = new NumericArray[2];
							Accessor.AsSparseFloat3Array(targetAttribute.Value.Value, ref sparseTangents[0],
								bufferViewCache1, 0, targetAttribute.Value.Value.Normalized);
							Accessor.AsSparseUIntArray(targetAttribute.Value.Value, ref sparseTangents[1],
								bufferViewCache2);
							break;
					}
				}
				
				GLTFHelpers.BuildTargetAttributes(ref att, scaleFactor);

				if (sparseNormals != null)
				{
					var current = att[NormalKey].AccessorContent;
					NumericArray before = new NumericArray();
					before.AsFloat3s = new float3[current.AsFloat3s.Length];
					for (int j = 0; j < sparseNormals[1].AsUInts.Length; j++)
					{
						before.AsFloat3s[sparseNormals[1].AsUInts[j]] = sparseNormals[0].AsFloat3s[j];
					}

					att[NormalKey].AccessorContent = before;
				}

				if (sparsePositions != null)
				{
					var current = att[PositionKey].AccessorContent;
					NumericArray before = new NumericArray();
					before.AsFloat3s = new float3[current.AsFloat3s.Length];
					for (int j = 0; j < sparsePositions[1].AsUInts.Length; j++)
					{
						before.AsFloat3s[sparsePositions[1].AsUInts[j]] = sparsePositions[0].AsFloat3s[j];
					}

					att[PositionKey].AccessorContent = before;
				}

				if (sparseTangents != null)
				{
					var current = att[TangentKey].AccessorContent;
					NumericArray before = new NumericArray();
					before.AsFloat3s = new float3[current.AsFloat3s.Length];
					for (int j = 0; j < sparseTangents[1].AsUInts.Length; j++)
					{
						before.AsFloat3s[sparseTangents[1].AsUInts[j]] = sparseTangents[0].AsFloat3s[j];
					}

					att[TangentKey].AccessorContent = before;
				}

				TransformTargets(ref att);
			}
		}

		private void FreeUpAccessorContents()
		{
			if (_gltfRoot.Meshes == null)
				return;
			
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
			if (_gltfRoot.Meshes == null)
				return;
			
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

				List<DracoDecodeResult> dracoDecodeResults = new List<DracoDecodeResult>();
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

				await Task.WhenAll(dracoDecodeResults.Select(d => d.decodeResults).SelectMany(d => d));

				for (int i = 0; i < dracoDecodeResults.Count; i++)
				{
					int meshIndex = dracoDecodeResults[i].meshIndex;

					for (int j = 0; j < dracoDecodeResults[i].decodeResults.Length; j++)
					{
						var decodeResult = dracoDecodeResults[i].decodeResults[j].Result;
						_assetCache.MeshCache[dracoDecodeResults[i].meshIndex].DracoMeshDecodeResult[j] = decodeResult;

						if (!decodeResult.success)
						{
							Debug.Log(LogType.Error, $"Error decoding draco mesh (File: {_gltfFileName})", this);
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

			int[] indices = null;

			if (meshAttributes.TryGetValue(SemanticProperties.INDICES, out var indicesAccessor))
			{
				indices = indicesAccessor.AccessorContent.AsUInts.ToIntArrayRaw();
				switch (unityData.DrawModes[indexOffset])
				{
					case DrawMode.LineLoop:
						if (indices[indices.Length - 1] != indices[0])
						{
							Array.Resize(ref indices, indices.Length + 1);
							indices[indices.Length - 1] = indices[0];
						}
						break;
					case DrawMode.Triangles:
						SchemaExtensions.FlipTriangleFaces(indices);
						break;
					case DrawMode.TriangleStrip:
						indices = MeshPrimitive.ConvertTriangleStripsToTriangles(indices);
						SchemaExtensions.FlipTriangleFaces(indices);
						break;
					case DrawMode.TriangleFan:
						indices = MeshPrimitive.ConvertTriangleFanToTriangles(indices);
						SchemaExtensions.FlipTriangleFaces(indices);
						break;
				}				
			}
			else
			{
				switch (unityData.DrawModes[indexOffset])
				{
					case DrawMode.Points:
						indices = MeshPrimitive.GeneratePoints((int)vertexCount);
						break;
					case DrawMode.Lines:
						indices = MeshPrimitive.GenerateLines((int)vertexCount);
						break;
					case DrawMode.LineLoop:
						indices = MeshPrimitive.GenerateLineLoop((int)vertexCount);
						break;
					case DrawMode.LineStrip:
						indices = MeshPrimitive.GenerateLineStrip((int)vertexCount);
						break;
					case DrawMode.Triangles:
						indices = MeshPrimitive.GenerateTriangles((int)vertexCount);
						break;
					case DrawMode.TriangleStrip:
						indices = MeshPrimitive.GenerateTriangleStrips((int)vertexCount);
						break;
					case DrawMode.TriangleFan:
						indices = MeshPrimitive.GenerateTriangleFans((int)vertexCount);
						break;
				}
			}

			if (indices != null)
				unityData.Indices[indexOffset] = indices;

			// Only add weight/joint data when it's not already added to the unity mesh data !
			if (meshAttributes.ContainsKey(SemanticProperties.Weight[0]) && meshAttributes.ContainsKey(SemanticProperties.Joint[0])
			    && !unityData.alreadyAddedAccessors.Contains(meshAttributes[SemanticProperties.Weight[0]].AccessorId.Id))
			{
				unityData.alreadyAddedAccessors.Add(meshAttributes[SemanticProperties.Weight[0]].AccessorId.Id);
				
				CreateBoneWeightArray(
					meshAttributes[SemanticProperties.Joint[0]].AccessorContent.AsFloat4s.ToUnityVector4Raw(),
					meshAttributes[SemanticProperties.Weight[0]].AccessorContent.AsFloat4s.ToUnityVector4Raw(),
					ref unityData.BoneWeights,
					vertOffset);
			}

			// Only add vertex data when it's not already added to the unity mesh data !
			if (meshAttributes.ContainsKey(SemanticProperties.POSITION) && !unityData.alreadyAddedAccessors.Contains(meshAttributes[SemanticProperties.POSITION].AccessorId.Id))
			{
				
				if (meshAttributes.TryGetValue(SemanticProperties.POSITION, out var attrPos))
				{
					unityData.alreadyAddedAccessors.Add(attrPos.AccessorId.Id);
					attrPos.AccessorContent.AsFloat3s.ToUnityVector3Raw(unityData.Vertices, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.NORMAL, out var attrNorm))
				{
					attrNorm.AccessorContent.AsFloat3s.ToUnityVector3Raw(unityData.Normals, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.TANGENT, out var attrTang))
				{
					attrTang.AccessorContent.AsFloat4s.ToUnityVector4Raw(unityData.Tangents, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.TexCoord[0], out var attrTex0))
				{
					attrTex0.AccessorContent.AsFloat2s.ToUnityVector2Raw(unityData.Uv1, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.TexCoord[1], out var attrTex1))
				{
					attrTex1.AccessorContent.AsFloat2s.ToUnityVector2Raw(unityData.Uv2, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.TexCoord[2], out var attrTex2))
				{
					attrTex2.AccessorContent.AsFloat2s.ToUnityVector2Raw(unityData.Uv3, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.TexCoord[3], out var attrTex3))
				{
					attrTex3.AccessorContent.AsFloat2s.ToUnityVector2Raw(unityData.Uv4, (int)vertOffset);
				}
				if (meshAttributes.TryGetValue(SemanticProperties.Color[0], out var attrColor))
				{
					if (_activeColorSpace == ColorSpace.Gamma)
						attrColor.AccessorContent.AsFloat4s.ToUnityColorRaw(unityData.Colors, (int)vertOffset);
					else
						attrColor.AccessorContent.AsFloat4s.ToUnityColorLinear(unityData.Colors, (int)vertOffset);
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
						tarAttrPos.AccessorContent.AsFloat3s.ToUnityVector3Raw(unityData.MorphTargetVertices[i], (int)vertOffset);
					}
					if (targets[i].TryGetValue(SemanticProperties.NORMAL, out var tarAttrNorm) && !unityData.alreadyAddedAccessors.Contains(tarAttrNorm.AccessorId.Id))
					{
						unityData.alreadyAddedAccessors.Add(tarAttrNorm.AccessorId.Id);
						tarAttrNorm.AccessorContent.AsFloat3s.ToUnityVector3Raw(unityData.MorphTargetNormals[i], (int)vertOffset);
					}
					if (targets[i].TryGetValue(SemanticProperties.TANGENT, out var tarAttrTang) && !unityData.alreadyAddedAccessors.Contains(tarAttrTang.AccessorId.Id))
					{
						unityData.alreadyAddedAccessors.Add(tarAttrTang.AccessorId.Id);
						tarAttrTang.AccessorContent.AsFloat3s.ToUnityVector3Raw(unityData.MorphTargetTangents[i], (int)vertOffset);
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
			if (_gltfRoot.Buffers == null)
				_gltfRoot.Buffers = new List<GLTFBuffer>();
			if (_gltfRoot.BufferViews == null)
				_gltfRoot.BufferViews = new List<BufferView>();
			_gltfRoot.Buffers.Add(new GLTFBuffer() { ByteLength = (uint) data.Length });
			_gltfRoot.BufferViews.Add(new BufferView() { ByteLength = (uint) data.Length, ByteOffset = 0, Buffer = new BufferId() { Id = _gltfRoot.Buffers.Count, Root = _gltfRoot } });
			accessor.BufferView = new BufferViewId() { Id = _gltfRoot.BufferViews.Count - 1, Root = _gltfRoot };
		}

		protected MeshTopology GetTopology(DrawMode mode)
		{
			switch (mode)
			{
				case DrawMode.Points: return MeshTopology.Points;
				case DrawMode.Lines: return MeshTopology.Lines;
				case DrawMode.LineStrip: return MeshTopology.LineStrip;
				case DrawMode.Triangles: return MeshTopology.Triangles;
				case DrawMode.LineLoop: return MeshTopology.LineStrip;
				case DrawMode.TriangleStrip: return MeshTopology.Triangles;
				case DrawMode.TriangleFan: return MeshTopology.Triangles;
			}

			throw new Exception("Unity does not support glTF draw mode: " + mode + $" (File: {_gltfFileName})");
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

		private void CheckForMeshDuplicates()
		{
			if (_gltfRoot.Meshes == null)
				return;
			
			Dictionary<int, int> meshDuplicates = new Dictionary<int, int>();

			for (int meshIndex = 0; meshIndex < _gltfRoot.Meshes.Count; meshIndex++)
			{
				if (meshDuplicates.ContainsKey(meshIndex))
				    continue;
				
				for (int i = meshIndex+1; i < _gltfRoot.Meshes.Count; i++)
				{
					
					if (i == meshIndex)
						continue;
					if (_assetCache.MeshCache[i] == null)
						continue;

					if (_assetCache.UnityMeshDataCache[i] == null
					    || _assetCache.UnityMeshDataCache[meshIndex] == null)
						continue;

					if (_assetCache.UnityMeshDataCache[i] == _assetCache.UnityMeshDataCache[meshIndex])
						continue;
					
					var meshIsEqual = _assetCache.UnityMeshDataCache[i]
						.IsEqual(_assetCache.UnityMeshDataCache[meshIndex]);
					
					if (meshIsEqual)
						meshDuplicates[i] = meshIndex;
				}
			}

			foreach (var dm in meshDuplicates)
			{
				_assetCache.UnityMeshDataCache[dm.Key] = _assetCache.UnityMeshDataCache[dm.Value];
				
				// if (_gltfRoot.Nodes == null) continue;
				// for (int i = 0; i < _gltfRoot.Nodes.Count; i++)
				// {
				// 	if (_gltfRoot.Nodes[i].Mesh != null && _gltfRoot.Nodes[i].Mesh.Id == dm.Key)
				// 	{
				// 		if (_gltfRoot.Nodes[i].Weights == null && _gltfRoot.Meshes[dm.Value].Weights != null)
				// 			_gltfRoot.Nodes[i].Weights = _gltfRoot.Meshes[_gltfRoot.Nodes[i].Mesh.Id].Weights;
				// 		
				// 		
				// 		_gltfRoot.Nodes[i].Mesh.Id = dm.Value;
				// 	}
				// }
			}
		}
	}
}
