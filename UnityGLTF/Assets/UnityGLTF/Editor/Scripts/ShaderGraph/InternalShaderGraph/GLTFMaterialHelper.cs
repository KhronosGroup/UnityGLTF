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
