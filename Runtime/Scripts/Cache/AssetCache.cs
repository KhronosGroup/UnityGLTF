using System;
using UnityEngine;
using System.IO;
using GLTF.Schema;

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
		/// Invalid/missing textures. These are cached since we can still remap them after import, but don't
		/// want to apply them to materials.
		/// </summary>
		public Texture2D[] InvalidImageCache { get; private set; }

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
		public MeshCacheData[] MeshCache { get; private set; }

		/// <summary>
		/// Cache of loaded unity meshes data
		/// </summary>
		public UnityMeshData[] UnityMeshDataCache { get; private set; }
#if UNITY_ANIMATION || !UNITY_2019_1_OR_NEWER
		/// <summary>
		/// Cache of loaded animations
		/// </summary>
		public AnimationCacheData[] AnimationCache { get; private set; }
#endif

		/// <summary>
		/// Cache of loaded node objects
		/// </summary>
		public GameObject[] NodeCache { get; private set; }

		/// <summary>
		/// Creates an asset cache which caches objects used in scene
		/// </summary>
		/// <param name="root">A glTF root whose assets will eventually be cached here</param>
		public AssetCache(GLTFRoot root)
		{
			ImageCache = new Texture2D[root.Images?.Count ?? 0];
			InvalidImageCache = new Texture2D[ImageCache.Length];
			ImageStreamCache = new Stream[ImageCache.Length];
			TextureCache = new TextureCacheData[root.Textures?.Count ?? 0];
			MaterialCache = new MaterialCacheData[root.Materials?.Count ?? 0];
			BufferCache = new BufferCacheData[root.Buffers?.Count ?? 0];
			MeshCache = new MeshCacheData[root.Meshes?.Count ?? 0];
			UnityMeshDataCache = new UnityMeshData[root.Meshes?.Count ?? 0];
			NodeCache = new GameObject[root.Nodes?.Count ?? 0];
#if UNITY_ANIMATION || !UNITY_2019_1_OR_NEWER
			AnimationCache = new AnimationCacheData[root.Animations?.Count ?? 0];
#endif
		}

		public void Dispose()
		{
			if (ImageStreamCache != null)
			{
				foreach (var stream in ImageStreamCache)
				{
					stream?.Dispose();
				}

				ImageStreamCache = null;
			}

			ImageCache = null;
			TextureCache = null;
			MaterialCache = null;
			if (BufferCache != null)
			{
				foreach (BufferCacheData bufferCacheData in BufferCache)
				{
					if (bufferCacheData != null)
					{
                        bufferCacheData.Dispose();
					}
				}
				BufferCache = null;
			}

			MeshCache = null;
#if UNITY_ANIMATION || !UNITY_2019_1_OR_NEWER
			AnimationCache = null;
#endif
		}
	}
}
