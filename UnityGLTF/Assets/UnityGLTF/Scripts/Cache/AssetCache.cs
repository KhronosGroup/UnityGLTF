using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace UnityGLTF.Cache
{
	/// <summary>
	/// Caches data in order to construct a unity object
	/// </summary>
	public class AssetCache : IDisposable
	{
		/// <summary>
		/// Streams to the images to be loaded
		/// </summary>
		public Stream[] ImageStreamCache { get; private set; }

		/// <summary>
		/// Loaded raw texture data
		/// </summary>
		public Texture2D[] ImageCache { get; private set; }
		
		/// <summary>
		/// Textures to be used for assets. Textures from image cache with samplers applied
		/// </summary>
		public TextureCacheData[] TextureCache { get; private set; }
		
		/// <summary>
		/// Cache for materials to be applied to the meshes
		/// </summary>
		public MaterialCacheData[] MaterialCache { get; private set; }

		/// <summary>
		/// Byte buffers that represent the binary contents that get parsed
		/// </summary>
		public BufferCacheData[] BufferCache { get; private set; }

		/// <summary>
		/// Cache of loaded meshes
		/// </summary>
		public List<MeshCacheData[]> MeshCache { get; private set; }

		/// <summary>
		/// Cache of loaded animations
		/// </summary>
		public AnimationCacheData[] AnimationCache { get; private set; }

		/// <summary>
		/// Cache of loaded node objects
		/// </summary>
		public GameObject[] NodeCache { get; private set; }

		/// <summary>
		/// Creates an asset cache which caches objects used in scene
		/// </summary>
		/// <param name="imageCacheSize"></param>
		/// <param name="textureCacheSize"></param>
		/// <param name="materialCacheSize"></param>
		/// <param name="bufferCacheSize"></param>
		/// <param name="meshCacheSize"></param>
		/// <param name="nodeCacheSize"></param>
		public AssetCache(int imageCacheSize, int textureCacheSize, int materialCacheSize, int bufferCacheSize,
			int meshCacheSize, int nodeCacheSize, int animationCacheSize)
		{
			ImageCache = new Texture2D[imageCacheSize];
			ImageStreamCache = new Stream[imageCacheSize];
			TextureCache = new TextureCacheData[textureCacheSize];
			MaterialCache = new MaterialCacheData[materialCacheSize];
			BufferCache = new BufferCacheData[bufferCacheSize];
			MeshCache = new List<MeshCacheData[]>(meshCacheSize);
			for (int i = 0; i < meshCacheSize; ++i)
			{
				MeshCache.Add(null);
			}

			NodeCache = new GameObject[nodeCacheSize];
			AnimationCache = new AnimationCacheData[animationCacheSize];
		}

		public void Dispose()
		{
			ImageCache = null;
			ImageStreamCache = null;
			TextureCache = null;
			MaterialCache = null;
			if (BufferCache != null)
			{
				foreach (BufferCacheData bufferCacheData in BufferCache)
				{
					if (bufferCacheData != null)
					{
						if (bufferCacheData.Stream != null)
						{
#if !WINDOWS_UWP
							bufferCacheData.Stream.Close();
#else
							bufferCacheData.Stream.Dispose();
#endif
                        }

                        bufferCacheData.Dispose();
					}
				}
				BufferCache = null;
			}

			MeshCache = null;
			AnimationCache = null;
		}
	}
}
