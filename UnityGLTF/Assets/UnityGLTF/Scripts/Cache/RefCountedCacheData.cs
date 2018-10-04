using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Cache
{
	/// <summary>
	/// A ref-counted cache data object containing lists of Unity objects that were created for the sake of a GLTF scene/node.
	/// This supports counting the amount of refcounts that will dispose of itself
	/// </summary>
	public class RefCountedCacheData
	{
		private bool _isDisposed = false;

		/// <summary>
		/// Ref count for this cache data.
		/// </summary>
		/// <remarks>
		/// Initialized to 0. When assigning the cache data to an instantiated GLTF
		/// object the count will increase.
		/// </remarks>
		private int _refCount = 0;
		private readonly object _refCountLock = new object();

		/// <summary>
		/// Meshes used by this GLTF node.
		/// </summary>
		public List<MeshCacheData[]> MeshCache { get; set; }

		/// <summary>
		/// Materials used by this GLTF node.
		/// </summary>
		public MaterialCacheData[] MaterialCache { get; set; }

		/// <summary>
		/// Textures used by this GLTF node.
		/// </summary>
		public TextureCacheData[] TextureCache { get; set; }

		public void IncreaseRefCount()
		{
			if (_isDisposed)
			{
				throw new InvalidOperationException("Cannot inscrease the ref count on disposed cache data.");
			}

			lock (_refCountLock)
			{
				_refCount++;
			}
		}

		public void DecreaseRefCount()
		{
			if (_isDisposed)
			{
				throw new InvalidOperationException("Cannot decrease the ref count on disposed cache data.");
			}

			lock (_refCountLock)
			{
				if (_refCount <= 0)
				{
					throw new InvalidOperationException("Cannot decrease the cache data ref count below zero.");
				}

				_refCount--;
			}

			if (_refCount <= 0)
			{
				DestroyCachedData();
			}
		}

		private void DestroyCachedData()
		{
			// Destroy the cached meshes
			for (int i = 0; i < MeshCache.Count; i++)
			{
				if (MeshCache[i] != null)
				{
					for (int j = 0; j < MeshCache[i].Length; j++)
					{
						if (MeshCache[i][j] != null)
						{
							MeshCache[i][j].Unload();
						}
					}
				}
			}

			// Destroy the cached textures
			for (int i = 0; i < TextureCache.Length; i++)
			{
				if (TextureCache[i] != null)
				{
					TextureCache[i].Unload();
				}
			}

			// Destroy the cached materials
			for (int i = 0; i < MaterialCache.Length; i++)
			{
				if (MaterialCache[i] != null)
				{
					MaterialCache[i].Unload();
				}
			}

			_isDisposed = true;
		}
	}
}
