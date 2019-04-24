using GLTF.Schema;
using System;
using UnityEngine;

namespace UnityGLTF.Cache
{
	public class TextureCacheData : IDisposable
	{
		public GLTFTexture TextureDefinition;
		public Texture Texture;

		/// <summary>
		/// Unloads the textures in this cache.
		/// </summary>
		public void Dispose()
		{
			if (Texture != null)
			{
				UnityEngine.Object.Destroy(Texture);
				Texture = null;
			}
		}
	}
}
