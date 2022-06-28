using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityGLTF
{
	public class PBRGraphGUI : ShaderGUI
	{
#if UNITY_2021_1_OR_NEWER
		public override void ValidateMaterial(Material material) => MaterialExtensions.ValidateMaterialKeywords(material);
#endif

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			var targetMaterial = materialEditor.target as Material;
			if (!targetMaterial) return;

			// Strict Mode
			// - texture transforms only for baseColorTexture, is used for all others as well
			// - occlusion MUST use UV2 if present
			var renderer = ShaderGraphHelpers.GetRendererForMaterialEditor(materialEditor);
			var hasColor = true;
			var hasUV0 = true;
			var hasUV1 = true;
			var occlusionTextureTexCoord = Mathf.RoundToInt(targetMaterial.GetFloat("occlusionTextureTexCoord"));
			var baseColorTextureTexCoord = Mathf.RoundToInt(targetMaterial.GetFloat("baseColorTextureTexCoord"));

			if (renderer)
			{
				var meshFilter = renderer.GetComponent<MeshFilter>();
				var mesh = meshFilter.sharedMesh;

				hasColor = mesh.HasVertexAttribute(VertexAttribute.Color);
				hasUV0 = mesh.HasVertexAttribute(VertexAttribute.TexCoord0);
				hasUV1 = mesh.HasVertexAttribute(VertexAttribute.TexCoord1);

				EditorGUILayout.LabelField("Mesh Properties", EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				EditorGUILayout.ObjectField("Target Mesh", mesh, typeof(Mesh), true);
				EditorGUILayout.Toggle("Has Vertex Colors", hasColor);

				if (hasColor != targetMaterial.IsKeywordEnabled("_VERTEX_COLORS_ON"))
				{
					EditorGUI.indentLevel++;
					if (hasColor)
					{
						EditorGUILayout.HelpBox("Mesh has vertex colors but Enable Vertex Colors is disabled.", MessageType.Info);
					}
					else
					{
						EditorGUILayout.HelpBox("Mesh has no vertex colors but Enable Vertex Colors is enabled.", MessageType.Info);
					}
					EditorGUI.indentLevel--;
				}

				EditorGUILayout.Toggle("Has UV0", hasUV0);
				EditorGUILayout.Toggle("Has UV1", hasUV1);
				if (hasUV1 && targetMaterial.GetTexture("occlusionTexture"))
				{
					EditorGUI.indentLevel++;
					var texCoord =  Mathf.RoundToInt(targetMaterial.GetFloat("occlusionTextureTexCoord"));
					if (texCoord != 1)
					{
						EditorGUILayout.HelpBox("This mesh has an Occlusion Texture and UV1 vertex data.\nWhen exporting to three.js, UV1 will be used independent of TexCoord setting.", MessageType.Warning);
						if (GUILayout.Button("Fix", EditorStyles.miniButtonRight, GUILayout.Width(50)))
							targetMaterial.SetFloat("occlusionTextureTexCoord", 1);
					}
					EditorGUI.indentLevel--;
				}

				if (!hasUV1)
				{
					if (baseColorTextureTexCoord > 0)
					{
						EditorGUILayout.HelpBox("This mesh does not have UV1 vertex data but Base Texture is set to use UV1. This will lead to unexpected results.", MessageType.Warning);
						if (GUILayout.Button("Fix", EditorStyles.miniButtonRight, GUILayout.Width(50)))
							targetMaterial.SetFloat("baseColorTextureTexCoord", 0);
					}
					if (occlusionTextureTexCoord > 0)
					{
						EditorGUILayout.HelpBox("This mesh does not have UV1 vertex data but Occlusion Texture is set to use UV1. This will lead to unexpected results.", MessageType.Warning);
						if (GUILayout.Button("Fix", EditorStyles.miniButtonRight, GUILayout.Width(50)))
							targetMaterial.SetFloat("occlusionTextureTexCoord", 0);
					}
				}

				EditorGUI.indentLevel--;
			}

			materialEditor.SetDefaultGUIWidths();

			EditorGUI.BeginChangeCheck();

			// filter properties based on keywords
			var propertyList = properties.ToList();
			if (!targetMaterial.IsKeywordEnabled("_TEXTURE_TRANSFORM_ON"))
			{
				propertyList.RemoveAll(x => x.name.EndsWith("_ST", StringComparison.Ordinal) || x.name.EndsWith("Rotation", StringComparison.Ordinal));
			}
			if (!targetMaterial.IsKeywordEnabled("_VOLUME_TRANSMISSION_ON"))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("thickness", StringComparison.Ordinal) || x.name.StartsWith("attenuation", StringComparison.Ordinal) || x.name.StartsWith("transmission", StringComparison.Ordinal));
			}
			if (!targetMaterial.IsKeywordEnabled("_IRIDESCENCE_ON"))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("iridescence", StringComparison.Ordinal));
			}
			if (!targetMaterial.IsKeywordEnabled("_SPECULAR_ON"))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("specular", StringComparison.Ordinal));
			}
			if (!targetMaterial.IsKeywordEnabled("_CLEARCOAT_ON"))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("clearcoat", StringComparison.Ordinal));
			}
			if (!targetMaterial.GetTexture("occlusionTexture"))
			{
				propertyList.RemoveAll(x => x.name == "occlusionStrength" || (x.name.StartsWith("occlusionTexture", StringComparison.Ordinal) && x.name != "occlusionTexture"));
			}
			if (!targetMaterial.GetTexture("baseColorTexture") && !targetMaterial.GetTexture("metallicRoughnessTexture") && !targetMaterial.GetTexture("normalTexture") && !targetMaterial.GetTexture("emissiveTexture"))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("baseColorTexture", StringComparison.Ordinal) && x.name != "baseColorTexture");
			}
			if (!targetMaterial.GetTexture("normalTexture"))
			{
				propertyList.RemoveAll(x => x.name == "normalScale");
			}
			if (!hasUV0 && !hasUV1)
			{
				// hide all texture properties?
				propertyList.RemoveAll(x => x.name.Contains("texture", StringComparison.OrdinalIgnoreCase));
			}
			if (!hasUV1 && occlusionTextureTexCoord == 0 && baseColorTextureTexCoord == 0)
			{
				propertyList.RemoveAll(x => x.name.EndsWith("TextureTexCoord", StringComparison.Ordinal));
			}

			ShaderGraphHelpers.DrawShaderGraphGUI(materialEditor, propertyList);

			if (EditorGUI.EndChangeCheck())
			{
				foreach (var t in materialEditor.targets)
				{
					if (t is Material material)
						MaterialExtensions.ValidateMaterialKeywords(material);
				}
			}
		}

		public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
		{
			GLTFMaterialHelper.ConvertMaterialToGLTF(material, oldShader, newShader);
		}
	}
}
