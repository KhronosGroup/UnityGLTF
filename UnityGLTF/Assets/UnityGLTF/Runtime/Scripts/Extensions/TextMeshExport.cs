using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;

namespace UnityGLTF
{
	public static class TextMeshExport
	{
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
		[RuntimeInitializeOnLoadMethod]
#endif
		static void Init()
		{
			GLTFSceneExporter.BeforeMaterialExport += BeforeMaterialExport;
			GLTFSceneExporter.AfterSceneExport += CleanUpRenderTextureCache;
		}

		private static void CleanUpRenderTextureCache(GLTFSceneExporter _, GLTFRoot __)
		{
			if (rtCache == null) return;
			foreach (var kvp in rtCache)
				kvp.Value.Release();
			rtCache.Clear();
		}

		private static Material tempMat;
		private static Dictionary<Texture, RenderTexture> rtCache;

		private static bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode)
		{
			if (material.shader.name.Contains("TextMeshPro"))
			{
				if (!tempMat) tempMat = new Material(Shader.Find("Unlit/Transparent Cutout"));

				var existingTex = material.mainTexture;
				if (rtCache == null) rtCache = new Dictionary<Texture, RenderTexture>();
				if (!rtCache.ContainsKey(existingTex))
				{
					var rt = new RenderTexture(existingTex.width, existingTex.height, 0, RenderTextureFormat.ARGB32);
					Graphics.Blit(existingTex, rt, material);
					rtCache[existingTex] = rt;
					rt.anisoLevel = 9;
					rt.filterMode = FilterMode.Bilinear;
				}

				tempMat.mainTexture = rtCache[existingTex];

				exporter.ExportUnlit(materialNode, tempMat);

				// export material
				// alternative: double sided, alpha clipping, white RGB + TMPro mainTex R channel as alpha
				materialNode.DoubleSided = false;
				materialNode.PbrMetallicRoughness.BaseColorFactor = Color.white.ToNumericsColorLinear();
				materialNode.AlphaMode = AlphaMode.BLEND;

				return true;
			}

			return false;
		}
	}
}
