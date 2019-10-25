using GLTF.Schema;
using System;
using UnityEngine;

namespace UnityGLTF.Cache
{
	public class MaterialCacheData : IDisposable
	{
		public Material UnityMaterial { get; set; }
		public Material UnityMaterialWithVertexColor { get; set; }
		public GLTFMaterial GLTFMaterial { get; set; }

		public Material GetContents(bool useVertexColors)
		{
			return useVertexColors ? UnityMaterialWithVertexColor : UnityMaterial;
		}

		/// <summary>
		/// Unloads the materials in this cache.
		/// </summary>
		public void Dispose()
		{
			if (UnityMaterial != null)
			{
				UnityEngine.Object.Destroy(UnityMaterial);
				UnityMaterial = null;
			}

			if (UnityMaterialWithVertexColor != null)
			{
				UnityEngine.Object.Destroy(UnityMaterialWithVertexColor);
				UnityMaterialWithVertexColor = null;
			}
		}
	}
}
