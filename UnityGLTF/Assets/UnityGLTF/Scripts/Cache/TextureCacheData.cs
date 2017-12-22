using UnityEngine;

namespace UnityGLTF.Cache
{
	public class TextureCacheData
	{
		public GLTF.Schema.Texture TextureDefinition;
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
