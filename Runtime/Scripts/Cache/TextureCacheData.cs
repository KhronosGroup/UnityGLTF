using GLTF.Schema;
using System;
using UnityEngine;

namespace UnityGLTF.Cache
{
	public class TextureCacheData : IDisposable
	{
		public GLTFTexture TextureDefinition;
		public Texture2D Texture;
		public bool IsLinear;
		public bool IsNormal;
		
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
