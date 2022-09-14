#if !NO_INTERNALS_ACCESS

using System.Linq;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
#endif

namespace UnityGLTF
{
	public static class GLTFMaterialHelper
	{
		/// <summary>
		/// Return false if other delegates should take over. Only return true if you did work and assigned material.shader = newShader.
		/// </summary>
		public delegate bool ConvertMaterialToGLTFDelegate(Material material, Shader oldShader, Shader newShader);
		private static event ConvertMaterialToGLTFDelegate ConvertMaterialDelegates;

		public static void RegisterMaterialConversionToGLTF(ConvertMaterialToGLTFDelegate converter) => ConvertMaterialDelegates += converter;
		public static void UnregisterMaterialConversionToGLTF(ConvertMaterialToGLTFDelegate converter) => ConvertMaterialDelegates -= converter;

		static GLTFMaterialHelper()
		{
			RegisterMaterialConversionToGLTF(ConvertStandardAndURPLit);
			RegisterMaterialConversionToGLTF(ConvertUnityGLTFGraphs);
		}

		private static bool ConvertUnityGLTFGraphs(Material material, Shader oldShader, Shader newShader)
		{
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

			material.shader = newShader;

			material.SetColor(baseColorFactor, color);
			material.SetTexture(baseColorTexture, albedo);
			material.SetTextureOffset(baseColorTexture, albedoOffset);
			material.SetTextureScale(baseColorTexture, albedoTiling);
			if (albedoOffset != Vector2.zero || albedoTiling != Vector2.one)
				material.SetKeyword("_TRANSMISSION_VOLUME", true);

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

			material.SetColor(emissiveFactor, needsEmissiveColorSpaceConversion ? emissionColor.linear : emissionColor);

			// set the flags on conversion, otherwise it's confusing why they're not on - can't easily replicate the magic that Unity does in their inspectors when changing emissive on/off
			if (material.globalIlluminationFlags == MaterialGlobalIlluminationFlags.None || material.globalIlluminationFlags == MaterialGlobalIlluminationFlags.EmissiveIsBlack)
				material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

			// ensure keywords are correctly set after conversion
			ShaderGraphHelpers.ValidateMaterialKeywords(material);

			return true;
		}

		public static void ConvertMaterialToGLTF(Material material, Shader oldShader, Shader newShader)
		{
			if (ConvertMaterialDelegates != null)
			{
				var list = ConvertMaterialDelegates.GetInvocationList();
				foreach (var entry in list)
				{
					var cb = (ConvertMaterialToGLTFDelegate) entry;
					if (cb != null && cb.Invoke(material, oldShader, newShader))
					{
						return;
					}
				}
			}

			// IDEA ideally this would use the same code path as material export/import - would reduce the amount of code duplication considerably.
			// E.g. calling something like
			// var glTFMaterial = ExportMaterial(material);
			// ImportAndOverrideMaterial(material, glTFMaterial);
			// that uses all the same heuristics, texture conversions, ...

			var msg = "No automatic conversion\nfrom " + oldShader.name + "\nto " + newShader.name + "\nfound.\n\nYou can create a conversion script, adjust which old properties map to which new properties, and switch the shader again.";

#if UNITY_EDITOR
			var choice = EditorUtility.DisplayDialogComplex("Shader Conversion", msg, "Just set shader", "Cancel", "Create and open conversion script");
			switch (choice)
			{
				case 0: // OK
					material.shader = newShader;
					break;
				case 1: // Cancel
					break;
				case 2: // Alt
					var path = ShaderConversion.CreateConversionScript(oldShader, newShader);
					InternalEditorUtility.OpenFileAtLineExternal(path, 0);
					break;
			}
#else
			Debug.Log(msg + " Make sure your material properties match the new shader. You can add your own conversion callbacks via `RegisterMaterialConversionToGLTF`. If you think this should have been converted automatically: please open a feature request!");
			material.shader = newShader;
#endif
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

#if UNITY_EDITOR
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
#endif
	}

#if UNITY_EDITOR
	internal static class ShaderConversion
	{
		public static string CreateConversionScript(Shader oldShader, Shader newShader)
		{
			var classShaderName = oldShader.name
				.Replace("/", "_")
				.Replace(" ", "_")
				.Replace("\\", "_");

			var scriptFile = ShaderConversionScriptTemplate;
			scriptFile = scriptFile.Replace("<OldShader>", classShaderName);
			scriptFile = scriptFile.Replace("<OldShaderName>", oldShader.name);

			var sb = new StringBuilder();
			foreach(var (propName, propDisplayName, type) in GetShaderProperties(oldShader))
			{
				sb.AppendLine($"\t\tvar {propName} = material.{MethodFromType("Get", type)}(\"{propName}\"); // {propDisplayName}");
			}

			var sb2 = new StringBuilder();
			foreach(var (propName, propDisplayName, type) in GetShaderProperties(newShader))
			{
				sb2.AppendLine($"\t\t// material.{MethodFromType("Set", type)}(\"{propName}\", insert_value_here); // {propDisplayName}");
			}

			scriptFile = scriptFile.Replace("\t\t<OldProperties>", sb.ToString());
			scriptFile = scriptFile.Replace("\t\t<NewProperties>", sb2.ToString());

			const string dir = "Assets/Editor/ShaderConversions";
			Directory.CreateDirectory(dir);
			var fileName = dir + "/" + classShaderName + ".cs";
			if (!File.Exists(fileName) || EditorUtility.DisplayDialog("File already exists", $"The file \"{fileName}\" already exists. Replace?", "Replace", "Cancel"))
				File.WriteAllText(fileName, scriptFile);

			AssetDatabase.Refresh();

			return fileName;
		}

		private static IEnumerable<(string propName, string propDisplayName, ShaderUtil.ShaderPropertyType type)> GetShaderProperties(Shader shader)
		{
			var c = ShaderUtil.GetPropertyCount(shader);
			for (var i = 0; i < c; i++)
			{
				if (ShaderUtil.IsShaderPropertyHidden(shader, i)) continue;
				if (ShaderUtil.IsShaderPropertyNonModifiableTexureProperty(shader, i)) continue;

				var propName = ShaderUtil.GetPropertyName(shader, i);
				if(propName.StartsWith("unity_")) continue;

				var propDisplayName = ShaderUtil.GetPropertyDescription(shader, i);
				var type = ShaderUtil.GetPropertyType(shader, i);
				yield return (propName, propDisplayName, type);
			}
		}

		private static string MethodFromType(string prefix, ShaderUtil.ShaderPropertyType propertyType)
		{
			switch (propertyType)
			{
				case ShaderUtil.ShaderPropertyType.Color:  return prefix + "Color";
				case ShaderUtil.ShaderPropertyType.Float:  return prefix + "Float";
#if UNITY_2021_1_OR_NEWER
				case ShaderUtil.ShaderPropertyType.Int:    return prefix + "Int";
#endif
				case ShaderUtil.ShaderPropertyType.Range:  return prefix + "Float";
				case ShaderUtil.ShaderPropertyType.Vector: return prefix + "Vector";
				case ShaderUtil.ShaderPropertyType.TexEnv: return prefix + "Texture";
			}

			return prefix + "UnknownPropertyType"; // compiler error
		}

		private const string ShaderConversionScriptTemplate =
@"using UnityEditor;
using UnityEngine;
using UnityGLTF;

class Convert_<OldShader>_to_GLTF
{
	const string shaderName = ""<OldShaderName>"";

	[InitializeOnLoadMethod]
	private static void Register()
	{
		GLTFMaterialHelper.RegisterMaterialConversionToGLTF(ConvertMaterialProperties);
	}

	private static bool ConvertMaterialProperties(Material material, Shader oldShader, Shader newShader)
	{
		if (oldShader.name != shaderName) return false;

		// Reading old shader properties.

		<OldProperties>
		material.shader = newShader;

		// Assigning new shader properties.
		// Uncomment lines you need, and set properties from values from the section above.

		<NewProperties>

		// Ensure keywords are correctly set after conversion.
		// Example:
		// if (material.GetFloat(""_VERTEX_COLORS"") > 0.5f) material.EnableKeyword(""_VERTEX_COLORS_ON"");

		ShaderGraphHelpers.ValidateMaterialKeywords(material);
		return true;
	}
}
";
	}
#endif
}

#endif
