using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Cache
{
	public class TextureCacheData
	{
		public GLTFTexture TextureDefinition;
		public Texture Texture;

		/// <summary>
		/// Unloads the textures in this cache.
		/// </summary>
		public void Unload()
		{
			Object.Destroy(Texture);
		}
	}
}
