using System.Linq;
using GLTF.Schema;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF
{
	public static class ShaderConverters
	{
		[InitializeOnLoadMethod]
		static void InitShaderConverters()
		{
			GLTFMaterialHelper.RegisterMaterialConversionToGLTF(ConvertStandardAndURPLit);
			GLTFMaterialHelper.RegisterMaterialConversionToGLTF(ConvertUnityGLTFGraphs);
		}

		private static bool ConvertUnityGLTFGraphs(Material material, Shader oldShader, Shader newShader)
		{
			// update legacy shaders that didn't have material overrides available
			if (oldShader.name.StartsWith("Hidden/UnityGLTF/PBRGraph") || oldShader.name.StartsWith("Hidden/UnityGLTF/UnlitGraph"))
			{
				material.shader = newShader;

				var meta = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.shader)) as IUnityGltfShaderUpgradeMeta;
				if (meta != null)
				{
					// Debug.Log("Updating shader from  " + material.shader + " to " + meta.SourceShader +
					//           " (transparent: " + meta.IsTransparent + ", double sided: " + meta.IsDoublesided + ")");

					var isUnlit = meta.SourceShader.name.Contains("Unlit");
					material.shader = meta.SourceShader;

					var mapper = isUnlit ? (IUniformMap) new UnlitMap(material) : new PBRGraphMap(material);
					if (meta.IsTransparent)
						mapper.AlphaMode = AlphaMode.BLEND;
					if (meta.IsDoublesided)
						mapper.DoubleSided = true;

					EditorUtility.SetDirty(material);
				}

				return true;
			}

			if (oldShader.name != "UnityGLTF/UnlitGraph" && oldShader.name != "UnityGLTF/PBRGraph") return false;

			material.shader = newShader;
			return true;
		}

		private static bool ConvertStandardAndURPLit(Material material, Shader oldShader, Shader newShader)
		{
			// Conversion time!
			// convert from
			// - "Standard"
			// - "URP/Lit"

			if (oldShader.name != StandardShader && oldShader.name != URPLitShader) return false;

			var isStandard = oldShader.name == StandardShader;
			var needsEmissiveColorSpaceConversion = isStandard && QualitySettings.activeColorSpace == ColorSpace.Linear;
			var colorProp = oldShader.name == StandardShader ? _Color : _BaseColor;
			var colorTexProp = oldShader.name == StandardShader ? _MainTex : _BaseMap;

			var color = material.GetColor(colorProp);
			var albedo = material.GetTexture(colorTexProp);
			var albedoOffset = material.GetTextureOffset(colorTexProp);
			var albedoTiling = material.GetTextureScale(colorTexProp);

			var metallic = material.GetFloat(_Metallic);
			var smoothness = material.HasProperty(_Smoothness) ? material.GetFloat(_Smoothness) : material.GetFloat(_Glossiness);
			var metallicGloss = material.GetTexture(_MetallicGlossMap);
			var normal = material.GetTexture(_BumpMap);
			var normalStrength = material.GetFloat(_BumpScale);
			var occlusion = material.GetTexture(_OcclusionMap);
			var occlusionStrength = material.GetFloat(_Strength);
			var emission = material.GetTexture(_EmissionMap);
			var emissionColor = material.GetColor(_EmissionColor);
			var cutoff = material.GetFloat(_Cutoff);

			var isCutoff = material.IsKeywordEnabled("_ALPHATEST_ON") || material.IsKeywordEnabled("_BUILTIN_ALPHATEST_ON");

			material.shader = newShader;

			material.SetColor(baseColorFactor, color);
			material.SetTexture(baseColorTexture, albedo);
			material.SetTextureOffset(baseColorTexture, albedoOffset);
			material.SetTextureScale(baseColorTexture, albedoTiling);
			if (albedoOffset != Vector2.zero || albedoTiling != Vector2.one)
				GLTFMaterialHelper.SetKeyword(material, "_TEXTURE_TRANSFORM", true);

			material.SetFloat(metallicFactor, metallic);
			material.SetFloat(roughnessFactor, 1 - smoothness);
			const string ConversionWarning = "The Metallic (R) Smoothness (A) texture needs to be converted to Roughness (B) Metallic (G). ";
#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
			var importerPath = AssetDatabase.GetAssetPath(metallicGloss);
			var importer = AssetImporter.GetAtPath(importerPath) as TextureImporter;
			if (importer && importer.swizzleG != TextureImporterSwizzle.OneMinusR) // can't really detect if this has been done on the texture already, this is just a heuristic...
			{
				if (EditorUtility.DisplayDialog("Texture Conversion",
					    ConversionWarning + "This is done by swizzling texture channels in the importer. Do you want to proceed?",
					    "OK", DialogOptOutDecisionType.ForThisSession, nameof(GLTFMaterialHelper) + "_texture_conversion"))
				{
					importer.swizzleR = TextureImporterSwizzle.B;
					importer.swizzleG = TextureImporterSwizzle.OneMinusR;
					importer.swizzleB = TextureImporterSwizzle.G;
					Undo.RegisterImporterUndo(importerPath, "Texture Swizzles (M__G > _RM_)");
					importer.SaveAndReimport();
				}
			}
			else
			{
				Debug.LogWarning(ConversionWarning + "This currently needs to be done manually. Please swap channels in an external software.", material);
			}
#else
			if (metallicGloss)
				Debug.LogWarning(ConversionWarning + "This currently needs to be done manually. Please swap channels in an external software.", material);
#endif
			// TODO: convert metallicGloss to metallicRoughnessTexture format: Metallic (R) + Smoothness (A) → Roughness (G) + Metallic (B)
			// TODO: when smoothness is not 0 or 1 and there's a texture, need to convert to a texture and set roughness = 1, otherwise the math doesn't match
			// TODO: figure out where to put the newly created texture, and how to avoid re-creating it several times when multiple materials may use it.
			material.SetTexture(metallicRoughnessTexture, metallicGloss);
			material.SetTexture(normalTexture, normal);
			material.SetFloat(normalScale, normalStrength);
			material.SetTexture(occlusionTexture, occlusion);
			material.SetFloat(occlusionStrength1, occlusionStrength);
			material.SetTexture(emissiveTexture, emission);
			material.SetFloat(alphaCutoff, cutoff);
			if (isCutoff)
			{
				GLTFMaterialHelper.SetKeyword(material, "_ALPHATEST", true);
				GLTFMaterialHelper.SetKeyword(material, "_BUILTIN_ALPHATEST", true);
				material.EnableKeyword("_BUILTIN_AlphaClip");
			}

			material.SetColor(emissiveFactor, needsEmissiveColorSpaceConversion ? emissionColor.linear : emissionColor);

			// set the flags on conversion, otherwise it's confusing why they're not on - can't easily replicate the magic that Unity does in their inspectors when changing emissive on/off
			if (material.globalIlluminationFlags == MaterialGlobalIlluminationFlags.None || material.globalIlluminationFlags == MaterialGlobalIlluminationFlags.EmissiveIsBlack)
				material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

			// ensure keywords are correctly set after conversion
			GLTFMaterialHelper.ValidateMaterialKeywords(material);

			return true;
		}

		// ReSharper disable InconsistentNaming
		private const string StandardShader = "Standard";
		private const string URPLitShader = "Universal Render Pipeline/Lit";

		// Standard and URP-Lit property names
		private static readonly int _Color = Shader.PropertyToID("_Color");
		private static readonly int _BaseColor = Shader.PropertyToID("_BaseColor");
		private static readonly int _MainTex = Shader.PropertyToID("_MainTex");
		private static readonly int _BaseMap = Shader.PropertyToID("_BaseMap");
		private static readonly int _Metallic = Shader.PropertyToID("_Metallic");
		private static readonly int _Glossiness = Shader.PropertyToID("_Glossiness");
		private static readonly int _Smoothness = Shader.PropertyToID("_Smoothness");
		private static readonly int _MetallicGlossMap = Shader.PropertyToID("_MetallicGlossMap");
		private static readonly int _BumpMap = Shader.PropertyToID("_BumpMap");
		private static readonly int _BumpScale = Shader.PropertyToID("_BumpScale");
		private static readonly int _OcclusionMap = Shader.PropertyToID("_OcclusionMap");
		private static readonly int _Strength = Shader.PropertyToID("_OcclusionStrength");
		private static readonly int _EmissionMap = Shader.PropertyToID("_EmissionMap");
		private static readonly int _EmissionColor = Shader.PropertyToID("_EmissionColor");
		private static readonly int _Cutoff = Shader.PropertyToID("_Cutoff");

		// glTF property names
		private static readonly int baseColorFactor = Shader.PropertyToID("baseColorFactor");
		private static readonly int baseColorTexture = Shader.PropertyToID("baseColorTexture");
		private static readonly int metallicFactor = Shader.PropertyToID("metallicFactor");
		private static readonly int roughnessFactor = Shader.PropertyToID("roughnessFactor");
		private static readonly int metallicRoughnessTexture = Shader.PropertyToID("metallicRoughnessTexture");
		private static readonly int normalTexture = Shader.PropertyToID("normalTexture");
		private static readonly int normalScale = Shader.PropertyToID("normalScale");
		private static readonly int occlusionTexture = Shader.PropertyToID("occlusionTexture");
		private static readonly int occlusionStrength1 = Shader.PropertyToID("occlusionStrength");
		private static readonly int emissiveTexture = Shader.PropertyToID("emissiveTexture");
		private static readonly int emissiveFactor = Shader.PropertyToID("emissiveFactor");
		private static readonly int alphaCutoff = Shader.PropertyToID("alphaCutoff");
		// ReSharper restore InconsistentNaming

		private static readonly string[] emissivePropNames = new[] { "emissiveFactor", "_EmissionColor" };

		[MenuItem("CONTEXT/Material/UnityGLTF Material Helpers/Convert Emissive Colors > sRGB - weaker, darker")]
		private static void ConvertToSRGB(MenuCommand command)
		{
			if (!(command.context is Material mat)) return;
			Undo.RegisterCompleteObjectUndo(mat, "Convert emissive colors to sRGB");
			foreach(var propName in emissivePropNames)
				if (mat.HasProperty(propName)) mat.SetColor(propName, mat.GetColor(propName).gamma);
		}

		[MenuItem("CONTEXT/Material/UnityGLTF Material Helpers/Convert Emissive Colors > Linear - brighter, stronger")]
		private static void ConvertToLinear(MenuCommand command)
		{
			if (!(command.context is Material mat)) return;
			Undo.RegisterCompleteObjectUndo(mat, "Convert emissive colors to sRGB");
			foreach(var propName in emissivePropNames)
				if (mat.HasProperty(propName)) mat.SetColor(propName, mat.GetColor(propName).linear);
		}

		[MenuItem("CONTEXT/Material/UnityGLTF Material Helpers/ Select all materials with this shader", false, -1000)]
		private static void SelectAllMaterialsWithShader(MenuCommand command)
		{
			if (!(command.context is Material mat)) return;
			var allMaterials = AssetDatabase.FindAssets("t:Material")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<Material>)
				.Where(x => !AssetDatabase.IsSubAsset(x))
				.Where(x => x.shader == mat.shader)
				.Cast<Object>()
				.ToArray();
			foreach (var obj in allMaterials)
				EditorGUIUtility.PingObject(obj);
			Selection.objects = allMaterials;
		}

		private static bool TryGetMetadataOfType<T>(Shader shader, out T obj) where T : ScriptableObject
		{
			obj = null;

			var path = AssetDatabase.GetAssetPath(shader);
			foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
			{
				if (asset is T metadataAsset)
				{
					obj = metadataAsset;
					return true;
				}
			}

			return false;
		}
	}
}
