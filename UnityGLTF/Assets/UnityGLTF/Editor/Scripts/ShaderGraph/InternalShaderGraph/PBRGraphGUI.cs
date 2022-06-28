using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityGLTF
{
	public class PBRGraphGUI : BuiltInBaseShaderGUI
	{
#if UNITY_2021_1_OR_NEWER
		public override void ValidateMaterial(Material material) => ShaderGraphHelpers.ValidateMaterialKeywords(material);
#endif

		protected MaterialEditor materialEditor;
		protected MaterialProperty[] properties;
		private readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue & ~(uint)Expandable.Advanced);
		private static readonly GUIContent MeshProperties = new GUIContent("Mesh Properties");
		private const int ExpandableMeshProperties = 1 << 8;
		private MaterialInfo currentMaterialInfo;

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			this.materialEditor = materialEditor;
			this.properties = properties;

			Material targetMat = materialEditor.target as Material;

			if (m_FirstTimeApply)
			{
				OnOpenGUI(targetMat, materialEditor, properties);
				m_FirstTimeApply = false;
			}

			// need to set these via reflection...
			// m_MaterialEditor = materialEditor;
			// m_Properties = properties;
			if (m_MaterialEditor == null) m_MaterialEditor = typeof(BuiltInBaseShaderGUI).GetField(nameof(m_MaterialEditor), BindingFlags.Instance | BindingFlags.NonPublic);
			if (m_Properties == null) m_Properties = typeof(BuiltInBaseShaderGUI).GetField(nameof(m_Properties), BindingFlags.Instance | BindingFlags.NonPublic);
			m_MaterialEditor.SetValue(this, materialEditor);
			m_Properties.SetValue(this, properties);
			m_MaterialScopeList.DrawHeaders(materialEditor, targetMat);
		}

		private FieldInfo m_MaterialEditor;
		private FieldInfo m_Properties;

		public override void OnOpenGUI(Material material, MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceOptions, (uint)Expandable.SurfaceOptions, DrawSurfaceOptions);
			m_MaterialScopeList.RegisterHeaderScope(MeshProperties, (uint)ExpandableMeshProperties, DrawGameObjectInfo);
			m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceInputs, (uint)Expandable.SurfaceInputs, DrawSurfaceInputs);
			m_MaterialScopeList.RegisterHeaderScope(Styles.AdvancedLabel, (uint)Expandable.Advanced, DrawAdvancedOptions);
		}

		private struct MaterialInfo
		{
			public bool hasColor;
			public bool hasUV0;
			public bool hasUV1;
			public int occlusionTextureTexCoord;
			public int baseColorTextureTexCoord;
		}

		private void DrawGameObjectInfo(Material targetMaterial)
		{
			// Strict Mode
			// - texture transforms only for baseColorTexture, is used for all others as well
			// - occlusion MUST use UV2 if present
			var renderer = ShaderGraphHelpers.GetRendererForMaterialEditor(materialEditor);
			currentMaterialInfo.hasColor = true;
			currentMaterialInfo.hasUV0 = true;
			currentMaterialInfo.hasUV1 = true;
			currentMaterialInfo.occlusionTextureTexCoord = Mathf.RoundToInt(targetMaterial.GetFloat("occlusionTextureTexCoord"));
			currentMaterialInfo.baseColorTextureTexCoord = Mathf.RoundToInt(targetMaterial.GetFloat("baseColorTextureTexCoord"));

			if (renderer)
			{
				var meshFilter = renderer.GetComponent<MeshFilter>();
				var mesh = meshFilter.sharedMesh;

				currentMaterialInfo.hasColor = mesh.HasVertexAttribute(VertexAttribute.Color);
				currentMaterialInfo.hasUV0 = mesh.HasVertexAttribute(VertexAttribute.TexCoord0);
				currentMaterialInfo.hasUV1 = mesh.HasVertexAttribute(VertexAttribute.TexCoord1);

				EditorGUILayout.ObjectField("Target Mesh", mesh, typeof(Mesh), true);
				EditorGUILayout.Toggle("Has Vertex Colors", currentMaterialInfo.hasColor);

				if (currentMaterialInfo.hasColor != targetMaterial.IsKeywordEnabled("_VERTEX_COLORS_ON"))
				{
					EditorGUI.indentLevel++;
					if (currentMaterialInfo.hasColor)
					{
						EditorGUILayout.HelpBox("Mesh has vertex colors but Enable Vertex Colors is disabled.", MessageType.Info);
					}
					else
					{
						EditorGUILayout.HelpBox("Mesh has no vertex colors but Enable Vertex Colors is enabled.", MessageType.Info);
					}
					EditorGUI.indentLevel--;
				}

				EditorGUILayout.Toggle("Has UV0", currentMaterialInfo.hasUV0);
				EditorGUILayout.Toggle("Has UV1", currentMaterialInfo.hasUV1);
				if (currentMaterialInfo.hasUV1 && targetMaterial.GetTexture("occlusionTexture"))
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

				if (!currentMaterialInfo.hasUV1)
				{
					if (currentMaterialInfo.baseColorTextureTexCoord > 0)
					{
						EditorGUILayout.HelpBox("This mesh does not have UV1 vertex data but Base Texture is set to use UV1. This will lead to unexpected results.", MessageType.Warning);
						if (GUILayout.Button("Fix", EditorStyles.miniButtonRight, GUILayout.Width(50)))
							targetMaterial.SetFloat("baseColorTextureTexCoord", 0);
					}
					if (currentMaterialInfo.occlusionTextureTexCoord > 0)
					{
						EditorGUILayout.HelpBox("This mesh does not have UV1 vertex data but Occlusion Texture is set to use UV1. This will lead to unexpected results.", MessageType.Warning);
						if (GUILayout.Button("Fix", EditorStyles.miniButtonRight, GUILayout.Width(50)))
							targetMaterial.SetFloat("occlusionTextureTexCoord", 0);
					}
				}
			}
		}

		protected override void DrawSurfaceInputs(Material mat)
		{
			var targetMaterial = materialEditor.target as Material;
			if (!targetMaterial) return;


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
			if (!currentMaterialInfo.hasUV0 && !currentMaterialInfo.hasUV1)
			{
				// hide all texture properties?
				propertyList.RemoveAll(x => x.name.Contains("texture", StringComparison.OrdinalIgnoreCase));
			}
			if (!currentMaterialInfo.hasUV1 && currentMaterialInfo.occlusionTextureTexCoord == 0 && currentMaterialInfo.baseColorTextureTexCoord == 0)
			{
				propertyList.RemoveAll(x => x.name.EndsWith("TextureTexCoord", StringComparison.Ordinal));
			}

			// TODO we probably want full manual control, all this internal access is horrible...
			// E.g. impossible to render inline texture properties...
			ShaderGraphHelpers.DrawShaderGraphGUI(materialEditor, propertyList);

			if (EditorGUI.EndChangeCheck())
			{
				foreach (var t in materialEditor.targets)
				{
					if (t is Material material)
						ShaderGraphHelpers.ValidateMaterialKeywords(material);
				}
			}
		}

		public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
		{
			GLTFMaterialHelper.ConvertMaterialToGLTF(material, oldShader, newShader);
		}
	}
}
