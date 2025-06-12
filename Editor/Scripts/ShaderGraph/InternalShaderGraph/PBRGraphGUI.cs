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
			GLTFMaterialHelper.ValidateMaterialKeywords(material);
		}
#endif

		protected MaterialEditor materialEditor;
		protected MaterialProperty[] properties;
		private MaterialInfo currentMaterialInfo;

#if HAVE_CATEGORIES
		private readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue & ~(uint)Expandable.Advanced);
		private const int ExpandableMeshProperties = 1 << 8;
		private static readonly GUIContent MeshProperties = new GUIContent("Mesh Properties");

		private bool immutableMaterialCanBeModified = false;

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			this.materialEditor = materialEditor;
			this.properties = properties;

			Material targetMat = materialEditor.target as Material;
			if (!targetMat || !targetMat.shader) return;

			if (m_FirstTimeApply)
			{
				OnOpenGUI(targetMat, materialEditor, properties);

				if (MaterialModificationTracker.CanEdit(materialEditor.target as Material))
					immutableMaterialCanBeModified = true;

				m_FirstTimeApply = false;
			}

			// Force GUI changes to be allowed, despite being in an immutable context.
			// We can store the changes back to .glTF when saving.
			if (immutableMaterialCanBeModified)
			{
				GUI.enabled = true;
				EditorGUILayout.HelpBox("This material is part of a glTF asset. If you enable editing (experimental), changes will be stored back to the .glTF file when saving.", MessageType.None);
			
				// looks like GetInstanceID() changes per import; so we use the path instead
				var path = AssetDatabase.GetAssetPath(targetMat) + "_" + targetMat.name;
				var materialEditingKey = nameof(PBRGraphGUI) + ".AllowGltfMaterialEditing." + path;
				var parentAssetIsMaterialLibrary = !(AssetDatabase.LoadMainAssetAtPath(path) is GameObject);
				var isAllowed = SessionState.GetBool(materialEditingKey, parentAssetIsMaterialLibrary);
				var allowMaterialEditing = EditorGUILayout.Toggle("Allow Editing", isAllowed);
				if (allowMaterialEditing != isAllowed)
					SessionState.SetBool(materialEditingKey, allowMaterialEditing);
				
				if (allowMaterialEditing)
					EditorGUILayout.HelpBox("glTF editing is enabled. This is highly experimental. Make sure you have a backup!", MessageType.Warning);
				
				GUI.enabled = allowMaterialEditing;
				EditorGUILayout.Space();
			}

			// Need to set these via reflection...
			// m_MaterialEditor = materialEditor;
			// m_Properties = properties;
			if (m_MaterialEditor == null) m_MaterialEditor = typeof(BuiltInBaseShaderGUI).GetField(nameof(m_MaterialEditor), BindingFlags.Instance | BindingFlags.NonPublic);
			if (m_Properties == null) m_Properties = typeof(BuiltInBaseShaderGUI).GetField(nameof(m_Properties), BindingFlags.Instance | BindingFlags.NonPublic);

			if (m_MaterialEditor == null || m_Properties == null)
			{
				Debug.LogError("Internal fields \"m_MaterialEditor\" and/or \"m_Properties\" not found on type BuiltInBaseShaderGUI. Please report this error to the UnityGLTF developers along with the Unity version you're using, there has probably been a Unity API change.");
				return;
			}

			m_MaterialEditor.SetValue(this, materialEditor);
			m_Properties.SetValue(this, properties);

			var assetPath = AssetDatabase.GetAssetPath(targetMat);
			var fullPath = string.IsNullOrEmpty(assetPath) ? "" : System.IO.Path.GetFullPath(assetPath);
			var isMutable = string.IsNullOrEmpty(assetPath) && !fullPath.Contains("Library/PackageCache") && !fullPath.Contains("Library\\PackageCache");
			if (targetMat && targetMat.shader.name.StartsWith("Hidden/UnityGLTF"))
			{
				if (isMutable)
				{
					DrawFixMeBox("This is a legacy shader. Please click \"Fix\" to upgrade to PBRGraph/UnlitGraph.", MessageType.Warning,  () =>
					{
						var isUnlit = targetMat.shader.name.Contains("Unlit");
						var newShaderGuid = isUnlit ? "59541e6caf586ca4f96ccf48a4813a51" : "478ce3626be7a5f4ea58d6b13f05a2e4";
						var newShader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(newShaderGuid));
						Undo.RegisterCompleteObjectUndo(targetMat, "Convert to UnityGltf shader");
						GLTFMaterialHelper.ConvertMaterialToGLTF(targetMat, targetMat.shader, newShader);
					});
				}
				else
				{
					DrawFixMeBox("This is a legacy shader. Upgrade to PBRGraph/UnlitGraph for more options.", MessageType.Warning, null);
				}
			}

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

		protected override void DrawAdvancedOptions(Material material)
		{
			base.DrawAdvancedOptions(material);
			materialEditor.EnableInstancingField();
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
			var isCutout = targetMaterial.GetFloat("alphaCutoff") >= 0;
			var indexOfDash = shaderName.IndexOf("-", StringComparison.Ordinal);
			var baseShaderName = indexOfDash < 0 ? shaderName : shaderName.Substring(0, indexOfDash);
			// EditorGUILayout.LabelField("Shader Name", baseShaderName);

			void SetShader()
			{
				var newName = baseShaderName + (isTransparent ? "-Transparent" : "") + (isDoubleSided ? "-Double" : "");
				Undo.RegisterCompleteObjectUndo(targetMaterial, "Set Material Options");
				targetMaterial.shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(ShaderNameToGuid[newName]));
				var currentCutoff = targetMaterial.GetFloat("alphaCutoff");
				if (!isCutout && currentCutoff == 0) currentCutoff = -0.0001f; // Hack to ensure we can actually switch states here
				targetMaterial.SetFloat("alphaCutoff", Mathf.Abs(currentCutoff) * (isCutout ? 1 : -1));
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
			var newA = EditorGUILayout.Toggle("Cutout", isCutout);
			if (newA != isCutout)
			{
				isCutout = newA;
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
				else if (renderer is MeshRenderer mr && mr.GetComponent<MeshFilter>())
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

					/*
					 > _VERTEX_COLORS_ON is currently not used in the shader, so we can't really check for it
					 
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
					}*/

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

					/* Now mostly supported, e.g. in three.js
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
					*/

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

					// check if this uses transmission
					// TODO we need to expose some kind of interface so that other scripts (e.g. the rough refraction components)
					// can attach to this inspector here.
					/*
					if (targetMaterial.IsKeywordEnabled("_VOLUME_TRANSMISSION_ON"))
					{
						// requires extra setup for BiRP / URP to render properly
						if (!GraphicsSettings.currentRenderPipeline && !Camera.main.gameObject.GetComponent<RoughRefraction>())
						{
							// warn
						}
						else
						{
							// check if assigned renderer has the rough refraction feature, offer to add it otherwise
							Camera.main.gameObject.GetComponent<UniversalAdditionalCameraData>()
						}
					}
					*/
				}
				haveDrawnSomething = true;
			}

			if (targetMaterial.HasProperty("_Surface") && targetMaterial.GetFloat("_Surface") == 0 &&
			    targetMaterial.renderQueue <= (int) RenderQueue.AlphaTest + 50 &&
			    !Mathf.Approximately(targetMaterial.GetColor("baseColorFactor").a, 1))
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
			if (action != null && GUILayout.Button("Fix")) action();
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
						GLTFMaterialHelper.ValidateMaterialKeywords(material);
				}

				// We only need to do this for non-editable materials, so we can flush the changes back out to disk.
				// These can be sub-assets of a ScriptedImporter or the main asset.
				// We probably only want to allow storing material changes back to .gltf; to be discussed.
				// TODO if textures are assigned, we need to think about how to handle that, as they should be externally referenced and not written to disk
				if (targetMaterial.hideFlags.HasFlag(HideFlags.NotEditable))
					MaterialModificationTracker.MarkDirty(targetMaterial);
			}
		}

		private static bool HasPropertyButNoTex(Material targetMaterial, string name)
		{
			// turns out HasProperty can return true when someone sets the property on a material - but the shader doesn't actually have that property
			var hasProperty = targetMaterial.shader.FindPropertyIndex(name) > -1;
#if UNITY_2021_2_OR_NEWER
			hasProperty &= targetMaterial.HasTexture(name);
#endif
			return hasProperty && !targetMaterial.GetTexture(name);
		}

		private void DrawProperties(Material targetMaterial, MaterialProperty[] properties)
		{
			// filter properties based on keywords
			var propertyList = properties.ToList();
			if (!targetMaterial.IsKeywordEnabled("_TEXTURE_TRANSFORM_ON"))
			{
				propertyList.RemoveAll(x => x.name.EndsWith("_ST", StringComparison.Ordinal) || x.name.EndsWith("TextureRotation", StringComparison.Ordinal));
				
				// Unity draws the tiling & offset properties based on the scale offset flag, so we need to ensure that's off here
				// so that the property fields are not displayed. Otherwise it's confusing that editing them doesn't do anything.
				foreach (var prop in propertyList)
				{
					if (prop.type == MaterialProperty.PropType.Texture)
						ShaderGraphHelpers.SetNoScaleOffset(prop);
				}
			}
			// want to remove the _ST properties since they are drawn inline already on 2021.2+
			#if UNITY_2021_2_OR_NEWER
			{
				propertyList.RemoveAll(x => x.name.EndsWith("_ST", StringComparison.Ordinal));
			}
			#endif
			if (!targetMaterial.IsKeywordEnabled("_VOLUME_TRANSMISSION_ON") && !targetMaterial.IsKeywordEnabled("_VOLUME_TRANSMISSION_ANDDISPERSION"))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("transmission", StringComparison.Ordinal));
			}
			if (!targetMaterial.IsKeywordEnabled("_VOLUME_TRANSMISSION_ANDDISPERSION"))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("dispersion", StringComparison.Ordinal));
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
			if (!targetMaterial.IsKeywordEnabled("_SHEEN_ON"))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("sheen", StringComparison.Ordinal));
			}
			if (!targetMaterial.HasProperty("_ANISOTROPY") || !(targetMaterial.GetFloat("_ANISOTROPY") > 0.5f))
			{
				propertyList.RemoveAll(x => x.name.StartsWith("anisotropy", StringComparison.Ordinal));
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
				// hide all texture properties if no UVs
				propertyList.RemoveAll(x => x.name.Contains("texture"));
			}
			if (!currentMaterialInfo.hasUV1 && currentMaterialInfo.occlusionTextureTexCoord == 0 && currentMaterialInfo.baseColorTextureTexCoord == 0)
			{
				propertyList.RemoveAll(x => x.name.EndsWith("TextureTexCoord", StringComparison.Ordinal));
			}

#if UNITY_2021_1_OR_NEWER
			var isBirp = !GraphicsSettings.currentRenderPipeline;
			if ((isBirp && !targetMaterial.IsKeywordEnabled("_BUILTIN_ALPHATEST_ON")) ||
			    (!isBirp && !targetMaterial.IsKeywordEnabled("_ALPHATEST_ON")))
#else
			if (targetMaterial.GetFloat("alphaCutoff") < 0)
#endif
			{
				propertyList.RemoveAll(x => x.name == "alphaCutoff");
			}
			
			// remove advanced properties that we want to draw a foldout for
			var overrideSurfaceMode = propertyList.FirstOrDefault(x => x.name == "_OverrideSurfaceMode");
			var normalMapFormatXYZ = propertyList.FirstOrDefault(x => x.name == "_NormalMapFormatXYZ");
			if (overrideSurfaceMode != null) propertyList.Remove(overrideSurfaceMode);
			if (normalMapFormatXYZ != null) propertyList.Remove(normalMapFormatXYZ);
			
			// TODO we probably want full manual control, all this internal access is horrible...
			// E.g. impossible to render inline texture properties...
			ShaderGraphHelpers.DrawShaderGraphGUI(materialEditor, propertyList);
			
			// draw a foldout with the advanced properties
			const string key = nameof(PBRGraphGUI) + "_AdvancedFoldout";
			var val = SessionState.GetBool(key, false);
			var newVal = EditorGUILayout.Foldout(val, "Mode Overrides", true);
			if (newVal != val) SessionState.SetBool(key, newVal);
			if (newVal)
			{
				EditorGUI.indentLevel++;
				if (overrideSurfaceMode != null) materialEditor.ShaderProperty(overrideSurfaceMode, 
					new GUIContent(overrideSurfaceMode.displayName, "When disabled, surface mode and queue are set by default based on material options. For example, transmissive objects will be set to \"transparent\". For some cases, explicit control over the surface mode helps with render order, e.g. nested transparency sorting."));
				if (normalMapFormatXYZ != null) materialEditor.ShaderProperty(normalMapFormatXYZ,
					new GUIContent(normalMapFormatXYZ.displayName, "When disabled, normal maps are assumed to be in the specified project format (XYZ or DXT5nm). Normal maps imported at runtime are always in XYZ, so this option is enabled for materials imported at runtime."));
				EditorGUI.indentLevel--;
			}
		}

		public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
		{
			GLTFMaterialHelper.ConvertMaterialToGLTF(material, oldShader, newShader);
		}

		public delegate void OnImmutableMaterialChanged(Material material);
		public static event OnImmutableMaterialChanged ImmutableMaterialChanged;

		internal static void InvokeMaterialChangedEvent(Material material)
		{
			try
			{
				ImmutableMaterialChanged?.Invoke(material);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}

	// TODO check if we can lose in-flight changes on domain reload this way
	internal class MaterialModificationTracker : AssetModificationProcessor
	{
		private static readonly List<UnityEngine.Object> ObjectsToSave = new List<UnityEngine.Object>();
		private static readonly Dictionary<Material, bool> CanEditCache = new Dictionary<Material, bool>();

		internal static void MarkDirty(UnityEngine.Object obj)
		{
			if (!ObjectsToSave.Contains(obj))
				ObjectsToSave.Add(obj);
		}

		private static string[] OnWillSaveAssets(string[] paths)
		{
			// we don't really care about the assets, we just want to flush changes for changed glTF materials
			var copy = new List<UnityEngine.Object>(ObjectsToSave);
			foreach (var obj in copy)
			{
				if (obj is Material material)
				{
					PBRGraphGUI.InvokeMaterialChangedEvent(material);
				}
			}
			copy.Clear();
			ObjectsToSave.Clear();
			return paths;
		}

		internal static bool CanEdit(Material materialEditorTarget)
		{
			if (CanEditCache.TryGetValue(materialEditorTarget, out var canEdit))
				return canEdit;

			// we can only edit this material if it's from a .gltf file right now. We're caching this here to avoid having to re-parse the file
			canEdit = AssetDatabase.GetAssetPath(materialEditorTarget).EndsWith(".gltf", StringComparison.OrdinalIgnoreCase);
			CanEditCache.Add(materialEditorTarget, canEdit);
			return canEdit;
		}
	}
}

#endif
