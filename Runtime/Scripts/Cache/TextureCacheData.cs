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
		/// <summary>The filter mode the sampler of the glTF texture asks for.</summary>
		public FilterMode FilterMode = FilterMode.Bilinear;
		/// <summary>The wrap modes the sampler of the glTF texture asks for (the sampler's defaults are Repeat).</summary>
		public TextureWrapMode WrapModeU = TextureWrapMode.Repeat;
		public TextureWrapMode WrapModeV = TextureWrapMode.Repeat;

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
