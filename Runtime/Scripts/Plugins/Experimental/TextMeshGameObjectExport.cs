using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;
#if HAVE_TMPRO
using TMPro;
#endif

namespace UnityGLTF.Plugins
{
	public class TextMeshGameObjectExport : GLTFExportPlugin
	{
		public override string DisplayName => "Bake to Mesh: TextMeshPro GameObjects";
		public override string Description => "Bakes 3D TextMeshPro objects (not UI/Canvas) into meshes and attempts to faithfully apply their shader settings to generate the font texture.";
		public override GLTFExportPluginContext CreateInstance(ExportContext context)
		{
#if HAVE_TMPRO
			return new TextMeshExportContext();
#else
			return null;
#endif
		}

#if !HAVE_TMPRO
		public override string Warning => "TextMeshPro is not installed. Please install TextMeshPro from the Package Manager to use this plugin.";
#endif
	}
	
	public class TextMeshExportContext: GLTFExportPluginContext
	{
		public override void AfterSceneExport(GLTFSceneExporter _, GLTFRoot __)
		{
			RenderTexture.active = null;
			if (rtCache == null) return;
			foreach (var kvp in rtCache)
				kvp.Value.Release();
			rtCache.Clear();
		}
		
		private Dictionary<Texture, RenderTexture> rtCache;

		public override void BeforeNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform,
			Node node)
		{
#if HAVE_TMPRO
			var tmp = transform.GetComponent<TextMeshPro>();
			if (tmp == null) return;
			
			tmp.ForceMeshUpdate();
#endif
		}
		
		public override bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode)
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
				if (!newS)
				{
					Debug.LogError("TextMeshPro/Mobile/Distance Field shader not found. For exporting TextMeshPro GameObjects, please ensure this shader exist in build.");
					return false;
				}

				material.shader = newS;
				
				var existingTex = material.mainTexture;
				if (rtCache == null) rtCache = new Dictionary<Texture, RenderTexture>();
				if (!rtCache.ContainsKey(existingTex))
				{
					var rt = new RenderTexture(existingTex.width * 2, existingTex.height * 2, 0, RenderTextureFormat.ARGB32);
					rt.useMipMap = true;
					rt.autoGenerateMips = false;
					rt.filterMode = FilterMode.Bilinear;
					rt.anisoLevel = 9;
					
					float outlineSoftness = 0;
					if (material.HasProperty("_OutlineSoftness"))
					{
						outlineSoftness = material.GetFloat("_OutlineSoftness");
						material.SetFloat("_OutlineSoftness", 0);
					}
					// TODO figure out how to get this more smooth
					Graphics.Blit(existingTex, rt, material);
					rt.GenerateMips();
					rtCache[existingTex] = rt;
					
					if (material.HasProperty("_OutlineSoftness"))
					{
						material.SetFloat("_OutlineSoftness", outlineSoftness);
					}

				}
				
				const string extname = KHR_MaterialsUnlitExtensionFactory.EXTENSION_NAME;
				exporter.DeclareExtensionUsage( extname, true );
				materialNode.AddExtension( extname, new KHR_MaterialsUnlitExtension());
				materialNode.PbrMetallicRoughness = new PbrMetallicRoughness();

				// export material
				// alternative: double sided, alpha clipping, white RGB + TMPro mainTex R channel as alpha
				materialNode.DoubleSided = false;
				materialNode.PbrMetallicRoughness.BaseColorFactor = Color.white.ToNumericsColorLinear();
				materialNode.AlphaMode = AlphaMode.BLEND;

				materialNode.PbrMetallicRoughness.BaseColorTexture = exporter.ExportTextureInfo(rtCache[existingTex], GLTFSceneExporter.TextureMapType.BaseColor);
				
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
