using GLTF;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Cache
{
	public class MeshCacheData : IDisposable
	{
		public class PrimitiveCacheData
		{
			public Dictionary<string, AttributeAccessor> Attributes = new Dictionary<string, AttributeAccessor>(4);
			public List<Dictionary<string, AttributeAccessor>> Targets = new List<Dictionary<string, AttributeAccessor>>(4);
		}

		public List<PrimitiveCacheData> Primitives = new List<PrimitiveCacheData>(5);
		public Mesh LoadedMesh { get; set; }

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
