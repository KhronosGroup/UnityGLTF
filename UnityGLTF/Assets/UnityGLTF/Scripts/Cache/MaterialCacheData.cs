using UnityEngine;

namespace UnityGLTF.Cache
{
	public class MaterialCacheData
	{
		public Material UnityMaterial { get; set; }
		public Material UnityMaterialWithVertexColor { get; set; }
		public GLTF.Schema.Material GLTFMaterial { get; set; }

		public Material GetContents(bool useVertexColors)
		{
			return useVertexColors ? UnityMaterialWithVertexColor : UnityMaterial;
		}

		/// <summary>
		/// Unloads the materials in this cache.
		/// </summary>
		public void Unload()
		{
			if (UnityMaterial != null)
			{
				Object.Destroy(UnityMaterial);
			}

			if (UnityMaterialWithVertexColor != null)
			{
				Object.Destroy(UnityMaterialWithVertexColor);
			}
		}
	}
}
