using GLTF;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Cache
{
	public class MeshCacheData : IDisposable
	{
		public Mesh LoadedMesh { get; set; }
		public Dictionary<string, AttributeAccessor> MeshAttributes { get; set; }
		public GameObject PrimitiveGO { get; set; }

		public MeshCacheData()
		{
			MeshAttributes = new Dictionary<string, AttributeAccessor>();
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
