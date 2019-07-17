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
		public MeshCacheData[] MeshCache { get; private set; }

		/// <summary>
		/// Materials used by this GLTF node.
		/// </summary>
		public MaterialCacheData[] MaterialCache { get; private set; }

		/// <summary>
		/// Textures used by this GLTF node.
		/// </summary>
		public TextureCacheData[] TextureCache { get; private set; }

		/// <summary>
		/// Textures from the AssetCache that might need to be cleaned up
		/// </summary>
		public Texture2D[] ImageCache { get; private set; }

		public RefCountedCacheData(MaterialCacheData[] materialCache, MeshCacheData[] meshCache, TextureCacheData[] textureCache, Texture2D[] imageCache)
		{
			MaterialCache = materialCache;
			MeshCache = meshCache;
			TextureCache = textureCache;
			ImageCache = imageCache;
		}

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
			for (int i = 0; i < MeshCache.Length; i++)
			{
				MeshCache[i]?.Dispose();
				MeshCache[i] = null;
			}

			// Destroy the cached textures
			for (int i = 0; i < TextureCache.Length; i++)
			{
				TextureCache[i]?.Dispose();
				TextureCache[i] = null;
			}

			// Destroy the cached materials
			for (int i = 0; i < MaterialCache.Length; i++)
			{
				MaterialCache[i]?.Dispose();
				MaterialCache[i] = null;
			}

			// Destroy the cached images
			for (int i = 0; i < ImageCache.Length; i++)
			{
				if (ImageCache[i] != null)
				{
					UnityEngine.Object.Destroy(ImageCache[i]);
					ImageCache[i] = null;
				}
			}

			_isDisposed = true;
		}
	}
}
