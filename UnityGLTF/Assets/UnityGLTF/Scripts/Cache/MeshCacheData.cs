using GLTF;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Cache
{
	public class MeshCacheData
	{
		public Mesh LoadedMesh { get; set; }
		public Dictionary<string, AttributeAccessor> MeshAttributes { get; set; }
		public List<Dictionary<string, AttributeAccessor>> MeshTargets { get; set; }

		public MeshCacheData()
		{
			MeshAttributes = new Dictionary<string, AttributeAccessor>();
			MeshTargets = new List<Dictionary<string, AttributeAccessor>>();
		}

		/// <summary>
		/// Unloads the meshes in this cache.
		/// </summary>
		public void Unload()
		{
			Object.Destroy(LoadedMesh);
		}
	}
}
