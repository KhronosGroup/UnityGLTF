#if !NO_INTERNALS_ACCESS
#if UNITY_2021_3_OR_NEWER
#define HAVE_BUILTIN_SHADERGRAPH
#define HAVE_CATEGORIES
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
#if HAVE_BUILTIN_SHADERGRAPH
using UnityEditor.Rendering;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityGLTF
{
	public class PBRGraphGUI :
#if HAVE_BUILTIN_SHADERGRAPH
		BuiltInBaseShaderGUI
#else
		ShaderGUI
#endif
	{
#if UNITY_2021_1_OR_NEWER
		public override void ValidateMaterial(Material material)
		{
			base.ValidateMaterial(material);
			ShaderGraphHelpers.ValidateMaterialKeywords(material);
		}
#endif

		protected MaterialEditor materialEditor;
		protected MaterialProperty[] properties;
		private MaterialInfo currentMaterialInfo;

#if HAVE_CATEGORIES
		private readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue & ~(uint)Expandable.Advanced);
		private const int ExpandableMeshProperties = 1 << 8;
		private static readonly GUIContent MeshProperties = new GUIContent("Mesh Properties");

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
#else
		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			this.materialEditor = materialEditor;
			this.properties = properties;

			Material targetMat = materialEditor.target as Material;

			DrawGameObjectInfo(targetMat);
#if !UNITY_2021_1_OR_NEWER
			DrawSurfaceOptions(targetMat);
#endif
			_DrawSurfaceInputs(targetMat);
		}
#endif

		private struct MaterialInfo
		{
			public bool hasColor;
			public bool hasUV0;
			public bool hasUV1;
			public int occlusionTextureTexCoord;
			public int baseColorTextureTexCoord;
		}

		private static bool HasAnyTextureAssigned(Material targetMaterial)
		{
			var s = targetMaterial.shader;
			var propertyCount = ShaderUtil.GetPropertyCount(s);
			for (var i = 0; i < propertyCount; i++)
			{
				if (ShaderUtil.GetPropertyType(s, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
				if (ShaderUtil.IsShaderPropertyHidden(s, i)) continue;
				var tex = targetMaterial.GetTexture(ShaderUtil.GetPropertyName(s, i));
				if (tex) return true;
			}
			return false;
		}

#if !UNITY_2021_1_OR_NEWER
		private static readonly Dictionary<string, string> ShaderNameToGuid = new Dictionary<string, string>()
		{
			{ "UnityGLTF/PBRGraph", "478ce3626be7a5f4ea58d6b13f05a2e4" },
			{ "UnityGLTF/PBRGraph-Transparent", "0a931320a74ca574b91d2d7d4557dcf1" },
			{ "UnityGLTF/PBRGraph-Transparent-Double", "54352a53405971b41a6587615f947085" },
			{ "UnityGLTF/PBRGraph-Double", "8bc739b14fe811644abb82057b363ba8" },

			{ "UnityGLTF/UnlitGraph", "59541e6caf586ca4f96ccf48a4813a51" },
			{ "UnityGLTF/UnlitGraph-Transparent", "83f2caca07949794fb997734c4b0520f" },
			{ "UnityGLTF/UnlitGraph-Transparent-Double", "8a8841b4fb2f63644896f4e2b36bc06d" },
			{ "UnityGLTF/UnlitGraph-Double", "33ee70a7f505ddb4e80d235c3d70766d" },
		};

		private void DrawSurfaceOptions(Material targetMaterial)
		{
			EditorGUILayout.Space();

			var shaderName = targetMaterial.shader.name.Replace("Hidden/", "");
			var isTransparent = shaderName.Contains("-Transparent");
			var isDoubleSided = shaderName.Contains("-Double");
			var indexOfDash = shaderName.IndexOf("-", StringComparison.Ordinal);
			var baseShaderName = indexOfDash < 0 ? shaderName : shaderName.Substring(0, indexOfDash);
			// EditorGUILayout.LabelField("Shader Name", baseShaderName);

			void SetShader()
			{
				var newName = baseShaderName + (isTransparent ? "-Transparent" : "") + (isDoubleSided ? "-Double" : "");
				Undo.RegisterCompleteObjectUndo(targetMaterial, "Set Material Options");
				targetMaterial.shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(ShaderNameToGuid[newName]));
			}

			var newT = EditorGUILayout.Toggle("Transparent", isTransparent);
			if (newT != isTransparent)
			{
				isTransparent = newT;
				SetShader();
			}
			var newD = EditorGUILayout.Toggle("Double Sided", isDoubleSided);
			if (newD != isDoubleSided)
			{
				isDoubleSided = newD;
				SetShader();
			}

			EditorGUILayout.Space();
		}
#endif

		private void DrawGameObjectInfo(Material targetMaterial)
		{
			var singleSelection = Selection.objects != null && Selection.objects.Length < 2;

			// Strict Mode
			// - texture transforms only for baseColorTexture, is used for all others as well
			// - occlusion MUST use UV2 if present
			var renderer = ShaderGraphHelpers.GetRendererForMaterialEditor(materialEditor);
			var haveDrawnSomething = false;

			if (renderer && !singleSelection)
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUI.showMixedValue = true;
				EditorGUILayout.ObjectField("Target Mesh", null, typeof(Mesh), true);
				EditorGUI.showMixedValue = false;
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.HelpBox("Multiple objects selected. Can't show mesh properties.", MessageType.Info);
				haveDrawnSomething = true;
			}

			currentMaterialInfo.hasColor = true;
			currentMaterialInfo.hasUV0 = true;
			currentMaterialInfo.hasUV1 = true;
			currentMaterialInfo.occlusionTextureTexCoord = targetMaterial.HasProperty("occlusionTextureTexCoord") ? Mathf.RoundToInt(targetMaterial.GetFloat("occlusionTextureTexCoord")) : 0;
			currentMaterialInfo.baseColorTextureTexCoord = targetMaterial.HasProperty("baseColorTextureTexCoord") ? Mathf.RoundToInt(targetMaterial.GetFloat("baseColorTextureTexCoord")) : 0;

			if (renderer && singleSelection)
			{
				var mesh = default(Mesh);
				if (renderer is SkinnedMeshRenderer smr)
					mesh = smr.sharedMesh;
				else if (renderer is MeshRenderer mr)
					mesh = mr.GetComponent<MeshFilter>()?.sharedMesh;

				if (mesh)
				{
					currentMaterialInfo.hasColor = mesh.HasVertexAttribute(VertexAttribute.Color);
					currentMaterialInfo.hasUV0 = mesh.HasVertexAttribute(VertexAttribute.TexCoord0);
					currentMaterialInfo.hasUV1 = mesh.HasVertexAttribute(VertexAttribute.TexCoord1);

					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField("Target Mesh", mesh, typeof(Mesh), true);
					EditorGUILayout.Toggle("Has Vertex Colors", currentMaterialInfo.hasColor);
					EditorGUI.EndDisabledGroup();

					if (currentMaterialInfo.hasColor != targetMaterial.IsKeywordEnabled("_VERTEX_COLORS_ON"))
					{
						EditorGUI.indentLevel++;
						var msg = "";
						var msgType = MessageType.Info;
						if (currentMaterialInfo.hasColor)
						{
							msg = "Mesh has vertex colors but \"Enable Vertex Colors\" is off. Exported glTF files will look different since vertex color is stored per-mesh, not per-material.";
							msgType = MessageType.Warning;
						}
						// else
						// {
						//		// doesn't really make sense to encourage turning vertex colors OFF - export will have them anyways.
						//		EditorGUILayout.HelpBox("Mesh has no vertex colors but \"Enable Vertex Colors\" is on.", MessageType.Info);
						// }

						if (!string.IsNullOrEmpty(msg))
						{
							DrawFixMeBox(msg, msgType, () =>
							{
								if (currentMaterialInfo.hasColor)
								{
									targetMaterial.EnableKeyword("_VERTEX_COLORS_ON");
									targetMaterial.SetFloat("_VERTEX_COLORS", 1);
								}
								else
								{
									targetMaterial.DisableKeyword("_VERTEX_COLORS_ON");
									targetMaterial.SetFloat("_VERTEX_COLORS", 0);
								}
							});
						}
						EditorGUI.indentLevel--;
					}

					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.Toggle("Has UV0", currentMaterialInfo.hasUV0);
					EditorGUILayout.Toggle("Has UV1", currentMaterialInfo.hasUV1);
					EditorGUI.EndDisabledGroup();

					if (!currentMaterialInfo.hasUV0 && HasAnyTextureAssigned(targetMaterial))
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.HelpBox("This mesh has no UV coordinates but has textures assigned.", MessageType.Warning);
						EditorGUI.indentLevel--;
					}

					if (currentMaterialInfo.hasUV1 && targetMaterial.HasProperty("occlusionTexture") && targetMaterial.GetTexture("occlusionTexture"))
					{
						EditorGUI.indentLevel++;
						var texCoord =  Mathf.RoundToInt(targetMaterial.GetFloat("occlusionTextureTexCoord"));
						if (texCoord != 1)
						{
							DrawFixMeBox("This mesh has an Occlusion Texture and UV1 vertex data.\nWhen exporting to three.js, UV1 will be used independent of TexCoord setting.", MessageType.Warning, () =>
							{
								Undo.RegisterCompleteObjectUndo(targetMaterial, "Set occlusionTextureTexCoord to 1");
								targetMaterial.SetFloat("occlusionTextureTexCoord", 1);
							});
						}
						EditorGUI.indentLevel--;
					}

					if (!currentMaterialInfo.hasUV1)
					{
						if (currentMaterialInfo.baseColorTextureTexCoord > 0)
						{
							DrawFixMeBox("This mesh does not have UV1 vertex data but Base Texture is set to use UV1. This will lead to unexpected results.", MessageType.Warning, () =>
							{
								Undo.RegisterCompleteObjectUndo(targetMaterial, "Set baseColorTextureTexCoord to 0");
								targetMaterial.SetFloat("baseColorTextureTexCoord", 0);
							});
						}
						if (currentMaterialInfo.occlusionTextureTexCoord > 0)
						{
							DrawFixMeBox("This mesh does not have UV1 vertex data but Occlusion Texture is set to use UV1. This will lead to unexpected results.", MessageType.Warning, () =>
							{
								Undo.RegisterCompleteObjectUndo(targetMaterial, "Set occlusionTextureTexCoord to 0");
								targetMaterial.SetFloat("occlusionTextureTexCoord", 0);
							});
						}
					}
				}
				haveDrawnSomething = true;
			}

			if (targetMaterial.HasProperty("_Surface") && targetMaterial.GetFloat("_Surface") == 0 && targetMaterial.GetColor("baseColorFactor").a != 1)
			{
				DrawFixMeBox("Material is opaque but baseColorFactor has an alpha value != 1. This object might render unexpectedly in some viewers that blend results (e.g. AR, Babylon, Stager).", MessageType.Warning, () =>
				{
					Undo.RegisterCompleteObjectUndo(materialEditor.targets, "Set baseColorFactor.a to 1");
					foreach (var t in materialEditor.targets)
					{
						var mat = (Material)t;
						var color = mat.GetColor("baseColorFactor");
						color.a = 1;
						mat.SetColor("baseColorFactor", color);
						EditorUtility.SetDirty(mat);
					}
				});
				haveDrawnSomething = true;
			}

			if (!renderer && !haveDrawnSomething)
			{
				EditorGUILayout.HelpBox("Select a Renderer to see additional info", MessageType.None);
			}
		}

		private static void DrawFixMeBox(string msg, MessageType msgType, Action action)
		{
#if HAVE_BUILTIN_SHADERGRAPH && false
			CoreEditorUtils.DrawFixMeBox(msg, msgType, action);
#else
			EditorGUILayout.HelpBox(msg, msgType);
			if (GUILayout.Button("Fix")) action();
#endif
		}

#if HAVE_CATEGORIES
		protected override void DrawSurfaceInputs(Material mat) => _DrawSurfaceInputs(mat);
#endif
		protected void _DrawSurfaceInputs(Material mat)
		{
			var targetMaterial = materialEditor.target as Material;
			if (!targetMaterial) return;


			materialEditor.SetDefaultGUIWidths();

			EditorGUI.BeginChangeCheck();

			DrawProperties(targetMaterial, properties);

			if (EditorGUI.EndChangeCheck())
			{
				foreach (var t in materialEditor.targets)
				{
					if (t is Material material)
						ShaderGraphHelpers.ValidateMaterialKeywords(material);
				}
			}
		}

		private static bool HasPropertyButNoTex(Material targetMaterial, string name)
		{
			return targetMaterial.HasProperty(name) &&
			       #if UNITY_2021_2_OR_NEWER
			       targetMaterial.HasTexture(name) &&
			       #endif
			       !targetMaterial.GetTexture(name);
		}

		private void DrawProperties(Material targetMaterial, MaterialProperty[] properties)
		{
			// filter properties based on keywords
			var propertyList = properties.ToList();
			if (!targetMaterial.IsKeywordEnabled("_TEXTURE_TRANSFORM_ON"))
			{
				propertyList.RemoveAll(x => x.name.EndsWith("_ST", StringComparison.Ordinal) || x.name.EndsWith("Rotation", StringComparison.Ordinal));
			}
			// want to remove the _ST properties since they are drawn inline already on 2021.2+
			#if UNITY_2021_2_OR_NEWER
			{
				propertyList.RemoveAll(x => x.name.EndsWith("_ST", StringComparison.Ordinal));
			}
			#endif
			if (!targetMaterial.IsKeywordEnabled("_VOLUME_TRANSMISSION_ON"))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("transmission", StringComparison.Ordinal));
			}
			if (!targetMaterial.HasProperty("_VOLUME_ON") || !(targetMaterial.GetFloat("_VOLUME_ON") > 0.5f))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("thickness", StringComparison.Ordinal) || x.name.StartsWith("attenuation", StringComparison.Ordinal));
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
			if (HasPropertyButNoTex(targetMaterial, "occlusionTexture"))
			{
				propertyList.RemoveAll(x => x.name == "occlusionStrength" || (x.name.StartsWith("occlusionTexture", StringComparison.Ordinal) && x.name != "occlusionTexture"));
			}
			// remove UV-related properties
			if (HasPropertyButNoTex(targetMaterial, "baseColorTexture") && HasPropertyButNoTex(targetMaterial,"metallicRoughnessTexture") && HasPropertyButNoTex(targetMaterial,"normalTexture") && HasPropertyButNoTex(targetMaterial,"emissiveTexture"))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("baseColorTexture", StringComparison.Ordinal) && x.name != "baseColorTexture");
			}
			if (HasPropertyButNoTex(targetMaterial,"normalTexture"))
			{
				propertyList.RemoveAll(x => x.name == "normalScale");
			}
			if (!currentMaterialInfo.hasUV0 && !currentMaterialInfo.hasUV1)
			{
				// hide all texture properties?
				propertyList.RemoveAll(x => x.name.Contains("texture"));
			}
			if (!currentMaterialInfo.hasUV1 && currentMaterialInfo.occlusionTextureTexCoord == 0 && currentMaterialInfo.baseColorTextureTexCoord == 0)
			{
				propertyList.RemoveAll(x => x.name.EndsWith("TextureTexCoord", StringComparison.Ordinal));
			}

			// TODO we probably want full manual control, all this internal access is horrible...
			// E.g. impossible to render inline texture properties...
			ShaderGraphHelpers.DrawShaderGraphGUI(materialEditor, propertyList);
		}

		public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
		{
			GLTFMaterialHelper.ConvertMaterialToGLTF(material, oldShader, newShader);
		}
	}
}

#endif
