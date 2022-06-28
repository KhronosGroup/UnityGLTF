using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF
{
	public static class GLTFMaterialHelper
	{
		public static void ConvertMaterialToGLTF(Material material, Shader oldShader, Shader newShader)
		{
			// TODO ideally this would use the same code path as material export/import - would reduce the amount of code duplication considerably.
			// E.g. calling something like
			// var glTFMaterial = ExportMaterial(material);
			// ImportAndOverrideMaterial(material, glTFMaterial);
			// that uses all the same heuristics, texture conversions, ...

			// Debug.Log("New shader: " + newShader + ", old shader: " + oldShader);

			// Conversion time!
			// convert from
			// - "Standard"
			// - "URP/Lit"

			if (oldShader.name == StandardShader || oldShader.name == URPLitShader)
			{
				var isStandard = oldShader.name == StandardShader;
				var needsEmissiveColorSpaceConversion = isStandard && QualitySettings.activeColorSpace == ColorSpace.Linear;
				var colorProp = oldShader.name == StandardShader ? _Color : _BaseColor;
				var colorTexProp = oldShader.name == StandardShader ? _MainTex : _BaseMap;

				var color = material.GetColor(colorProp); // "_BaseColor"
				var albedo = material.GetTexture(colorTexProp); // "_BaseMap"
				var albedoOffset = material.GetTextureOffset(colorTexProp);
				var albedoTiling = material.GetTextureScale(colorTexProp);

				var metallic = material.GetFloat(_Metallic);
				var smoothness = material.GetFloat(_Glossiness);
				var metallicGloss = material.GetTexture(_MetallicGlossMap);
				var normal = material.GetTexture(_BumpMap);
				var normalStrength = material.GetFloat(_BumpScale);
				var occlusion = material.GetTexture(_OcclusionMap);
				var occlusionStrength = material.GetFloat(_Strength);
				var emission = material.GetTexture(_EmissionMap);
				var emissionColor = material.GetColor(_EmissionColor);
				var cutoff = material.GetFloat(_Cutoff);

				material.shader = newShader;

				material.SetColor(baseColorFactor, color);
				material.SetTexture(baseColorTexture, albedo);
				material.SetTextureOffset(baseColorTexture, albedoOffset);
				material.SetTextureScale(baseColorTexture, albedoTiling);
				if (albedoOffset != Vector2.zero || albedoTiling != Vector2.one)
					material.SetKeyword("_TRANSMISSION_VOLUME", true);

				material.SetFloat(metallicFactor, metallic);
				material.SetFloat(roughnessFactor, 1 - smoothness);
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

				material.SetColor(emissiveFactor, needsEmissiveColorSpaceConversion ? emissionColor.linear : emissionColor);

				// ensure keywords are correctly set after conversion
				ShaderGraphHelpers.ValidateMaterialKeywords(material);
			}
			else
			{
				Debug.Log("No automatic conversion from " + oldShader.name + " to " + newShader.name + " found. Make sure your material properties match the new shader. If you think this should have been converted automatically: please report a bug!");
			}
		}

		private static void SetKeyword(this Material material, string keyword, bool state)
		{
			if (state)
				material.EnableKeyword(keyword + "_ON");
			else
				material.DisableKeyword(keyword + "_ON");

			if (material.HasProperty(keyword))
				material.SetFloat(keyword, state ? 1 : 0);
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

#if UNITY_EDITOR
		private static readonly string[] emissivePropNames = new[] { "emissiveFactor", "_EmissionColor" };

		[MenuItem("CONTEXT/Material/Material Fixups/Convert Emissive Colors > sRGB - weaker, darker")]
		private static void ConvertToSRGB(MenuCommand command)
		{
			if (!(command.context is Material mat)) return;
			Undo.RegisterCompleteObjectUndo(mat, "Convert emissive colors to sRGB");
			foreach(var propName in emissivePropNames)
				if (mat.HasProperty(propName)) mat.SetColor(propName, mat.GetColor(propName).gamma);
		}

		[MenuItem("CONTEXT/Material/Material Fixups/Convert Emissive Colors > Linear - brighter, stronger")]
		private static void ConvertToLinear(MenuCommand command)
		{
			if (!(command.context is Material mat)) return;
			Undo.RegisterCompleteObjectUndo(mat, "Convert emissive colors to sRGB");
			foreach(var propName in emissivePropNames)
				if (mat.HasProperty(propName)) mat.SetColor(propName, mat.GetColor(propName).linear);
		}
#endif
	}
}
