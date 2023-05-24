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
			if (material.shader.name.Contains("TextMeshPro")) // seems to only work for TextMeshPro/Mobile/ right now (SDFAA_HINTED?)
			{
				var s = material.shader;
				// TODO figure out how to properly use the non-mobile shaders
				var newS = Shader.Find("TextMeshPro/Mobile/Distance Field");
#if UNITY_EDITOR
				if (!newS)
				{
					newS = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(UnityEditor.AssetDatabase.GUIDToAssetPath("fe393ace9b354375a9cb14cdbbc28be4")); // same as above
				}
#endif
				material.shader = newS;

				if (!tempMat) tempMat = new Material(Shader.Find("Unlit/Transparent Cutout"));

				var existingTex = material.mainTexture;
				if (rtCache == null) rtCache = new Dictionary<Texture, RenderTexture>();
				if (!rtCache.ContainsKey(existingTex))
				{
					var rt = new RenderTexture(existingTex.width, existingTex.height, 0, RenderTextureFormat.ARGB32);
					if (material.HasProperty("_OutlineSoftness"))
						material.SetFloat("_OutlineSoftness", 0);
					// TODO figure out how to get this more smooth
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

				material.shader = s;
#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
				UnityEditor.EditorUtility.ClearDirty(material);
#endif

				return true;
			}

			return false;
		}
	}
}
