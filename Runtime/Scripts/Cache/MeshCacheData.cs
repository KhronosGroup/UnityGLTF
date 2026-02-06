using GLTF;
using System;
using System.Collections.Generic;
using UnityEngine;
#if HAVE_DRACO
using Draco;
#if !HAVE_DRACO_VERSION_5
using DecodeResult = Draco.DracoMeshLoader.DecodeResult;
#endif
#endif

namespace UnityGLTF.Cache
{
	public class MeshCacheData : IDisposable
	{
		public class PrimitiveCacheData
		{
			public bool meshAttributesCreated = false;
			public Dictionary<string, AttributeAccessor> Attributes = new Dictionary<string, AttributeAccessor>(4);
			public List<Dictionary<string, AttributeAccessor>> Targets = new List<Dictionary<string, AttributeAccessor>>(4);
			public Dictionary<string, (AttributeAccessor sparseIndices, AttributeAccessor sparseValues)> SparseAccessors = new Dictionary<string, (AttributeAccessor sparseIndices, AttributeAccessor sparseValues)>(4);
		}

		public List<PrimitiveCacheData> Primitives = new List<PrimitiveCacheData>(5);
		public Mesh LoadedMesh { get; set; }

#if HAVE_DRACO
		public bool DracoMeshDataPrepared { get; set; } = false;
		public bool HasDracoMeshData { get; set; } = false;
		public Mesh.MeshDataArray DracoMeshData { get; set; }
		public DecodeResult[] DracoMeshDecodeResult { get; set; }
#endif

		public int GetHashFromPrimitiveAttributes()
		{
			if (Primitives == null)
				return 0;
			var hash = new Hash128();
			foreach (var primitive in Primitives)
			{
				if (primitive.Attributes != null)
				{
					foreach (var attribute in primitive.Attributes)
					{
						hash.Append(attribute.Key);
						hash.Append(attribute.Value.AccessorId.Id);
					}
				}

				if (primitive.Targets != null)
				{
					foreach (var target in primitive.Targets)
					{
						foreach (var attribute in target)
						{
							hash.Append(attribute.Key);
							hash.Append(attribute.Value.AccessorId.Id);
						}
					}
				}

				if (primitive.SparseAccessors != null)
				{
					foreach (var sparseAccessor in primitive.SparseAccessors)
					{
						hash.Append(sparseAccessor.Key);
						hash.Append(sparseAccessor.Value.sparseIndices.AccessorId.Id);
						hash.Append(sparseAccessor.Value.sparseValues.AccessorId.Id);
					}
				}
			}
			return hash.GetHashCode();
		}
		
		/// <summary>
		/// Unloads the meshes in this cache.
		/// </summary>
		public void Dispose()
		{
			if (LoadedMesh != null)
			{
				UnityEngine.Object.Destroy(LoadedMesh);
				LoadedMesh = null;
			}
		}
	}
}
